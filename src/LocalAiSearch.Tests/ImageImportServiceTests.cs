using Microsoft.Data.Sqlite;
using LocalAiSearch.Services;

namespace LocalAiSearch.Tests;

public class ImageImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseService _db;
    private readonly ImageImportService _sut;
    private readonly List<string> _tempFiles = new();

    // Minimal valid JPEG header bytes so the file is readable by FileInfo
    private static readonly byte[] MinimalJpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private int _fileCounter;

    public ImageImportServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new DatabaseService(_connection);
        _db.InitializeAsync().GetAwaiter().GetResult();
        _sut = new ImageImportService(_db);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        _connection.Dispose();
    }

    private string CreateTempImageFile(string extension = ".jpg")
    {
        var path = Path.ChangeExtension(Path.GetTempFileName(), extension);
        // Append a unique counter so each file has a distinct hash
        var content = MinimalJpegHeader.Concat(BitConverter.GetBytes(++_fileCounter)).ToArray();
        File.WriteAllBytes(path, content);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public async Task ImportAsync_WithNewImages_ReturnsCorrectAddedCount()
    {
        var files = new[]
        {
            CreateTempImageFile(".jpg"),
            CreateTempImageFile(".png"),
            CreateTempImageFile(".jpeg"),
        };

        var result = await _sut.ImportAsync(files);

        Assert.Equal(3, result.Added);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Unsupported);
    }

    [Fact]
    public async Task ImportAsync_WithDuplicateHash_SkipsSilently()
    {
        var file = CreateTempImageFile(".jpg");

        var first = await _sut.ImportAsync([file]);
        Assert.Equal(1, first.Added);

        var second = await _sut.ImportAsync([file]);

        Assert.Equal(0, second.Added);
        Assert.Equal(1, second.Skipped);
        Assert.Equal(0, second.Unsupported);
    }

    [Fact]
    public async Task ImportAsync_WithUnsupportedExtension_CountsAsUnsupported()
    {
        var rawFile = CreateTempImageFile(".raw");
        var txtFile = CreateTempImageFile(".txt");

        var result = await _sut.ImportAsync([rawFile, txtFile]);

        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(2, result.Unsupported);
    }

    [Fact]
    public async Task ImportAsync_WithMixedFiles_ReturnsCorrectCounts()
    {
        var newFile1 = CreateTempImageFile(".jpg");
        var newFile2 = CreateTempImageFile(".png");
        var unsupportedFile = CreateTempImageFile(".raw");

        // Pre-import the duplicate
        var duplicate = CreateTempImageFile(".jpg");
        await _sut.ImportAsync([duplicate]);

        var result = await _sut.ImportAsync([newFile1, newFile2, duplicate, unsupportedFile]);

        Assert.Equal(2, result.Added);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, result.Unsupported);
    }

    [Fact]
    public async Task ImportAsync_NewImage_IsStoredInDatabase()
    {
        var file = CreateTempImageFile(".jpg");

        await _sut.ImportAsync([file]);

        var all = await _db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(file, all[0].FilePath);
    }

    [Fact]
    public async Task ImportAsync_NewImage_IsNotTagged()
    {
        var file = CreateTempImageFile(".jpg");

        await _sut.ImportAsync([file]);

        var all = await _db.GetAllAsync();
        Assert.Single(all);
        Assert.False(all[0].IsTagged);
    }

    [Fact]
    public async Task ImportAsync_WithEmptyList_ReturnsZeros()
    {
        var result = await _sut.ImportAsync(Array.Empty<string>());

        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Unsupported);
    }
}
