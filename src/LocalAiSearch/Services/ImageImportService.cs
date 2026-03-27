using System.Security.Cryptography;
using LocalAiSearch.Models;

namespace LocalAiSearch.Services;

public class ImageImportService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    };

    private readonly DatabaseService _db;

    public ImageImportService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// Imports a list of file paths into the database, skipping duplicates and unsupported formats.
    /// AI tagging is NOT triggered — all items are imported with IsTagged=false.
    /// </summary>
    public async Task<ImportResult> ImportAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default)
    {
        await _db.InitializeAsync();

        int added = 0;
        int skipped = 0;
        int unsupported = 0;

        foreach (var filePath in filePaths)
        {
            ct.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(filePath);
            if (!SupportedExtensions.Contains(ext))
            {
                unsupported++;
                continue;
            }

            try
            {
                var hash = await ComputeFileHashAsync(filePath, ct);

                var existing = await _db.GetByHashAsync(hash);
                if (existing != null)
                {
                    skipped++;
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                var now = DateTime.UtcNow;
                var item = new MediaItem
                {
                    FilePath = filePath,
                    FileHash = hash,
                    FileSizeBytes = fileInfo.Length,
                    MediaType = "image",
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsTagged = false,
                    Description = string.Empty,
                    Tags = string.Empty
                };

                await _db.InsertAsync(item);
                added++;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"[ImageImportService] Error importing {filePath}: {ex.Message}");
                skipped++;
            }
        }

        return new ImportResult(added, skipped, unsupported);
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

public record ImportResult(int Added, int Skipped, int Unsupported);
