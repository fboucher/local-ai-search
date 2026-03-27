using System.Net.Http.Json;
using System.Text.Json;
using LocalAiSearch.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace LocalAiSearch.Services;

public record TaggingResult(bool Success, string? Description, string? Tags, string? MediaType, string? Error);

public record TaggingBatchResult(int Tagged, int Skipped);

/// <summary>
/// Sends images to an OpenAI-compatible vision API and stores the results.
/// Set the AI_ENDPOINT environment variable (or pass endpointUrl) to point at the real local API.
/// If no endpoint is configured, runs in stub mode and returns placeholder data.
/// </summary>
public class AiTaggingService
{
    private readonly DatabaseService _db;
    private readonly HttpClient _http;
    private readonly string _endpointUrl;
    private readonly bool _isStub;

    private const string DefaultModel = "local-model";
    private const string DefaultAnalysisModel = "llava";
    private const string DefaultAnalysisEndpoint = "http://192.168.2.11:8000/v1";

    private const string TaggingPrompt =
        "Describe this image. Provide:\n" +
        "1) A one-sentence description.\n" +
        "2) 5 relevant tags (comma-separated).\n" +
        "3) Media type (photo/screenshot/image).\n\n" +
        "Format your response exactly as:\n" +
        "DESCRIPTION: <description>\n" +
        "TAGS: <tag1,tag2,tag3,tag4,tag5>\n" +
        "TYPE: <photo|screenshot|image>";

    private const string AnalysisPrompt =
        "Describe this image in 1-2 sentences. Then list 5-10 relevant tags separated by commas. " +
        "Format your response as:\n" +
        "DESCRIPTION: [description]\n" +
        "TAGS: [tag1, tag2, ...]";

    public AiTaggingService(DatabaseService db, string? endpointUrl = null, HttpClient? httpClient = null)
    {
        _db = db;
        _http = httpClient ?? new HttpClient();

        var resolvedUrl = endpointUrl ?? Environment.GetEnvironmentVariable("AI_ENDPOINT");
        if (string.IsNullOrWhiteSpace(resolvedUrl))
        {
            _isStub = true;
            _endpointUrl = string.Empty;
        }
        else
        {
            _isStub = false;
            _endpointUrl = resolvedUrl.TrimEnd('/');
        }
    }

    public async Task<TaggingResult> TagImageAsync(MediaItem item, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(item.FilePath))
                return new TaggingResult(false, null, null, null, $"File not found: {item.FilePath}");

            var bytes = await File.ReadAllBytesAsync(item.FilePath, ct);
            var base64 = Convert.ToBase64String(bytes);
            var dataUrl = $"data:{GetMimeType(item.FilePath)};base64,{base64}";

            var result = _isStub
                ? new TaggingResult(true, "Stub description of the image.", "stub,placeholder,image,untagged,local", "photo", null)
                : await CallAiApiAsync(dataUrl, ct);

            if (result.Success)
            {
                item.Description = result.Description ?? string.Empty;
                item.Tags = result.Tags ?? string.Empty;
                item.MediaType = result.MediaType ?? string.Empty;
                item.IsTagged = true;
                item.UpdatedAt = DateTime.UtcNow;
                await _db.UpdateAsync(item);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TaggingResult(false, null, null, null, ex.Message);
        }
    }

    /// <summary>
    /// Analyzes a single image using the local OpenAI-compatible vision endpoint.
    /// Reads the file from disk, sends it as a base64 vision request, parses the response,
    /// and updates the matching row in the database. Returns the extracted description and tags.
    /// </summary>
    public async Task<(string Description, string Tags)> AnalyzeImageAsync(string imagePath, CancellationToken ct = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image not found: {imagePath}", imagePath);

        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var mimeType = GetMimeType(imagePath);

        var analysisEndpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT") ?? DefaultAnalysisEndpoint;
        var model = Environment.GetEnvironmentVariable("AI_MODEL") ?? DefaultAnalysisModel;

        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(analysisEndpoint) };
        var openAiClient = new OpenAIClient(new ApiKeyCredential("sk-local"), clientOptions);
        var chatClient = openAiClient.GetChatClient(model);

        var imagePart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mimeType);
        var textPart = ChatMessageContentPart.CreateTextPart(AnalysisPrompt);

        var messages = new List<ChatMessage>
        {
            new UserChatMessage(imagePart, textPart)
        };

        var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: ct);
        var responseText = completion.Value.Content[0].Text ?? string.Empty;

        var parsed = ParseAiResponse(responseText);
        var description = parsed.Description ?? string.Empty;
        var tags = parsed.Tags ?? string.Empty;

        var item = await _db.GetByFilePathAsync(imagePath);
        if (item != null)
        {
            item.Description = description;
            item.Tags = tags;
            item.IsTagged = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _db.UpdateAsync(item);
        }

        return (description, tags);
    }

    public async Task<TaggingBatchResult> TagAllUnprocessedAsync(CancellationToken ct = default)
    {
        var all = await _db.GetAllAsync();
        var unprocessed = all.Where(i => !i.IsTagged).ToList();

        int tagged = 0;
        int skipped = 0;

        foreach (var item in unprocessed)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await TagImageAsync(item, ct);
                if (result.Success)
                    tagged++;
                else
                {
                    Console.Error.WriteLine($"[AiTaggingService] Skipped '{item.FilePath}': {result.Error}");
                    skipped++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AiTaggingService] Skipped '{item.FilePath}': {ex.Message}");
                skipped++;
            }
        }

        return new TaggingBatchResult(tagged, skipped);
    }

    private async Task<TaggingResult> CallAiApiAsync(string dataUrl, CancellationToken ct)
    {
        var payload = new
        {
            model = DefaultModel,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = dataUrl } },
                        new { type = "text", text = TaggingPrompt }
                    }
                }
            }
        };

        using var response = await _http.PostAsJsonAsync($"{_endpointUrl}/v1/chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return ParseAiResponse(content);
    }

    internal static TaggingResult ParseAiResponse(string content)
    {
        string? description = null;
        string? tags = null;
        string? mediaType = null;

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                description = line["DESCRIPTION:".Length..].Trim();
            else if (line.StartsWith("TAGS:", StringComparison.OrdinalIgnoreCase))
                tags = line["TAGS:".Length..].Trim();
            else if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase))
                mediaType = line["TYPE:".Length..].Trim();
        }

        // Fallback: use raw content as description if structured parsing failed
        if (description == null && tags == null)
        {
            description = content.Length > 200 ? content[..200] : content;
            tags = "untagged";
            mediaType = "image";
        }

        return new TaggingResult(true, description, tags, mediaType ?? "image", null);
    }

    private static string GetMimeType(string filePath) =>
        Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };
}
