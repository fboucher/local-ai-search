using System.Net;
using System.Text;
using System.Text.Json;
using LocalAiSearch.Models;
using LocalAiSearch.Services;
using Microsoft.Data.Sqlite;

namespace LocalAiSearch.Tests;

/// <summary>
/// Custom HttpMessageHandler that returns a configured response for testing.
/// </summary>
file sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

/// <summary>
/// HttpMessageHandler that captures request body for assertion in tests.
/// </summary>
file sealed class CapturingHttpHandler(string responseBody) : HttpMessageHandler
{
    public string? CapturedRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
            CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}

public class AiTaggingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseService _db;

    public AiTaggingServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new DatabaseService(_connection);
        _db.InitializeAsync().GetAwaiter().GetResult();
    }

    public void Dispose() => _connection.Dispose();

    // ── Stub mode ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task StubMode_ReturnsPlaceholderResult_WhenNoEndpointConfigured()
    {
        // No endpoint → stub mode
        var svc = new AiTaggingService(_db, endpointUrl: null);

        var item = await SeedItemAsync("fake/path/photo.jpg");
        // TagImageAsync will fail on File.Exists check in stub mode for a non-existent file
        // so we need a real file. Use a temp PNG in the test dir.
        var path = WriteTestImage();
        item.FilePath = path;
        await _db.UpdateAsync(item);

        var result = await svc.TagImageAsync(item);

        Assert.True(result.Success);
        Assert.NotNull(result.Description);
        Assert.Contains("stub", result.Tags, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.MediaType);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task StubMode_UpdatesItemInDatabase()
    {
        var svc = new AiTaggingService(_db, endpointUrl: null);
        var path = WriteTestImage();
        var item = await SeedItemAsync(path);

        await svc.TagImageAsync(item);

        var updated = await _db.GetByIdAsync(item.Id);
        Assert.NotNull(updated);
        Assert.True(updated!.IsTagged);
        Assert.NotEmpty(updated.Description);
        Assert.NotEmpty(updated.Tags);
    }

    // ── File not found ─────────────────────────────────────────────────────────

    [Fact]
    public async Task TagImageAsync_ReturnsFailure_WhenFileDoesNotExist()
    {
        var svc = new AiTaggingService(_db, endpointUrl: null);
        var item = await SeedItemAsync("/does/not/exist.jpg");

        var result = await svc.TagImageAsync(item);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Batch processing ───────────────────────────────────────────────────────

    [Fact]
    public async Task TagAllUnprocessedAsync_OnlyProcessesUntaggedItems()
    {
        var svc = new AiTaggingService(_db, endpointUrl: null);
        var path = WriteTestImage();

        // One untagged item (real file), one already tagged (missing file — won't be processed)
        var untagged = await SeedItemAsync(path, isTagged: false);
        var alreadyTagged = await SeedItemAsync("/irrelevant.jpg", isTagged: true);

        var batch = await svc.TagAllUnprocessedAsync();

        // The untagged item succeeded; the already-tagged item was never touched.
        Assert.Equal(1, batch.Tagged);
        Assert.Equal(0, batch.Skipped);
    }

    [Fact]
    public async Task TagAllUnprocessedAsync_SkipsAndContinues_OnFailure()
    {
        var svc = new AiTaggingService(_db, endpointUrl: null);
        var path = WriteTestImage();

        // First item: missing file → will fail
        await SeedItemAsync("/missing.jpg", isTagged: false);
        // Second item: real file → will succeed
        await SeedItemAsync(path, isTagged: false);

        var batch = await svc.TagAllUnprocessedAsync();

        Assert.Equal(1, batch.Tagged);
        Assert.Equal(1, batch.Skipped);
    }

    // ── HTTP call structure (mocked handler) ───────────────────────────────────

    [Fact]
    public async Task TagImageAsync_CallsCorrectEndpoint_WhenEndpointConfigured()
    {
        const string fakeResponse = @"{
            ""choices"": [{
                ""message"": {
                    ""content"": ""DESCRIPTION: A sunny beach.\nTAGS: beach,sun,ocean,sand,summer\nTYPE: photo""
                }
            }]
        }";

        var handler = new FakeHttpHandler(HttpStatusCode.OK, fakeResponse);
        var http = new HttpClient(handler);
        var svc = new AiTaggingService(_db, endpointUrl: "http://fake-ai-host", httpClient: http);

        var path = WriteTestImage();
        var item = await SeedItemAsync(path);

        var result = await svc.TagImageAsync(item);

        Assert.True(result.Success);
        Assert.Equal("A sunny beach.", result.Description);
        Assert.Equal("beach,sun,ocean,sand,summer", result.Tags);
        Assert.Equal("photo", result.MediaType);
    }

    [Fact]
    public async Task TagImageAsync_ReturnsFailure_WhenApiReturnsError()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "error");
        var http = new HttpClient(handler);
        var svc = new AiTaggingService(_db, endpointUrl: "http://fake-ai-host", httpClient: http);

        var path = WriteTestImage();
        var item = await SeedItemAsync(path);

        var result = await svc.TagImageAsync(item);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    // ── ParseAiResponse unit tests ─────────────────────────────────────────────

    [Fact]
    public void ParseAiResponse_ParsesStructuredFormat()
    {
        var content = "DESCRIPTION: A red barn.\nTAGS: barn,red,farm,rural,sky\nTYPE: photo";

        var result = AiTaggingService.ParseAiResponse(content);

        Assert.True(result.Success);
        Assert.Equal("A red barn.", result.Description);
        Assert.Equal("barn,red,farm,rural,sky", result.Tags);
        Assert.Equal("photo", result.MediaType);
    }

    [Fact]
    public void ParseAiResponse_FallsBack_WhenFormatUnrecognized()
    {
        var result = AiTaggingService.ParseAiResponse("Just some freeform text from the AI.");

        Assert.True(result.Success);
        Assert.NotNull(result.Description);
        Assert.Equal("untagged", result.Tags);
    }

    // ── Linus: additional edge-case coverage ───────────────────────────────────

    [Fact]
    public async Task TagAllUnprocessedAsync_WithMultipleItems_ProcessesAll()
    {
        var svc = new AiTaggingService(_db, endpointUrl: null);
        var path1 = WriteTestImage();
        var path2 = WriteTestImage();
        var path3 = WriteTestImage();

        await SeedItemAsync(path1);
        await SeedItemAsync(path2);
        await SeedItemAsync(path3);

        var batch = await svc.TagAllUnprocessedAsync();

        Assert.Equal(3, batch.Tagged);
        Assert.Equal(0, batch.Skipped);

        var all = await _db.GetAllAsync();
        Assert.All(all, item => Assert.True(item.IsTagged));
    }

    [Fact]
    public async Task TagImageAsync_WithEndpoint_SendsBase64DataUrl()
    {
        const string fakeResponse = @"{""choices"":[{""message"":{""content"":""DESCRIPTION: Test.\nTAGS: a,b,c,d,e\nTYPE: photo""}}]}";
        var handler = new CapturingHttpHandler(fakeResponse);
        var svc = new AiTaggingService(_db, endpointUrl: "http://fake-ai-host", httpClient: new HttpClient(handler));

        var path = WriteTestImage();
        var item = await SeedItemAsync(path);

        await svc.TagImageAsync(item);

        Assert.NotNull(handler.CapturedRequestBody);
        using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
        var imageUrl = doc.RootElement
            .GetProperty("messages")[0]
            .GetProperty("content")[0]
            .GetProperty("image_url")
            .GetProperty("url")
            .GetString();

        Assert.NotNull(imageUrl);
        Assert.StartsWith("data:", imageUrl);
        Assert.Contains(";base64,", imageUrl);
        // The body after the comma must be valid Base64
        var base64Part = imageUrl!.Split(",", 2)[1];
        Assert.True(IsValidBase64(base64Part), "Data URL payload is not valid Base64");
    }

    [Fact]
    public void ParseAiResponse_IsCaseInsensitive_ForFieldLabels()
    {
        var content = "description: A sunny day.\ntags: sun,sky,warm,light,day\ntype: photo";

        var result = AiTaggingService.ParseAiResponse(content);

        Assert.True(result.Success);
        Assert.Equal("A sunny day.", result.Description);
        Assert.Equal("sun,sky,warm,light,day", result.Tags);
        Assert.Equal("photo", result.MediaType);
    }

    private static bool IsValidBase64(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length % 4 != 0) return false;
        try { Convert.FromBase64String(s); return true; }
        catch { return false; }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<MediaItem> SeedItemAsync(string filePath, bool isTagged = false)
    {
        var item = new MediaItem
        {
            FilePath = filePath,
            FileHash = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = isTagged,
            MediaType = "image",
        };
        item.Id = await _db.InsertAsync(item);
        return item;
    }

    /// <summary>Creates a minimal 1x1 white PNG in the test output directory.</summary>
    private static string WriteTestImage()
    {
        // Minimal valid PNG bytes (1×1 white pixel)
        var png = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR length + type
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // width=1, height=1
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // bit depth, color type, crc
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT length + type
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F, // compressed data
            0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59, // more data + crc
            0xE7, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND length + type
            0x44, 0xAE, 0x42, 0x60, 0x82                    // IEND crc
        };
        var path = Path.Combine(AppContext.BaseDirectory, $"test_{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, png);
        return path;
    }
}
