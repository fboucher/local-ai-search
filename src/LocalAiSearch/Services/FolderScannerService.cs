using System.Security.Cryptography;
using LocalAiSearch.Models;

namespace LocalAiSearch.Services;

public class FolderScannerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    };

    private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cr2", ".nef", ".arw", ".dng", ".orf", ".rw2", ".raf", ".pef", ".srw"
    };

    private readonly DatabaseService _db;

    public FolderScannerService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// Recursively scans a folder for supported images and returns unprocessed items.
    /// </summary>
    public async Task<ScanResult> ScanFolderAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult();

        if (!Directory.Exists(rootPath))
        {
            result.Errors.Add($"Directory does not exist: {rootPath}");
            return result;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to enumerate directory: {ex.Message}");
            return result;
        }

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Skip RAW formats
            if (IsRawFormat(filePath))
            {
                continue;
            }

            // Only process supported images
            if (!IsSupportedImage(filePath))
            {
                continue;
            }

            result.TotalDiscovered++;

            try
            {
                // Get file info
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    result.Errors.Add($"File no longer exists: {filePath}");
                    continue;
                }

                // Compute hash
                var hash = await ComputeFileHashAsync(filePath);

                // Check if file already exists in database
                var existingItem = await _db.GetByHashAsync(hash);

                if (existingItem != null)
                {
                    if (existingItem.IsTagged)
                    {
                        // Already processed and tagged
                        result.AlreadyProcessed++;
                    }
                    else
                    {
                        // Duplicate found but not yet tagged
                        result.Duplicates++;
                        result.UnprocessedItems.Add(existingItem);
                    }
                }
                else
                {
                    // New item - create and insert
                    var now = DateTime.UtcNow;
                    var newItem = new MediaItem
                    {
                        FilePath = filePath,
                        FileHash = hash,
                        MediaType = "image",
                        FileSizeBytes = fileInfo.Length,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsTagged = false
                    };

                    var id = await _db.InsertAsync(newItem);
                    newItem.Id = id;

                    result.NewItems++;
                    result.UnprocessedItems.Add(newItem);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing {filePath}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Computes SHA256 hash of a file.
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a file is a supported image format.
    /// </summary>
    public static bool IsSupportedImage(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// Checks if a file is a RAW format (to be skipped).
    /// </summary>
    public static bool IsRawFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return RawExtensions.Contains(extension);
    }

    /// <summary>
    /// Returns selected folder path or null if cancelled.
    /// Actual platform picker integration will be wired in the UI layer.
    /// </summary>
    public static Task<string?> PickFolderAsync()
    {
        // TODO: Wire to platform folder picker in UI slice
        // For now returns null (picker will be implemented when UI is connected)
        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Result of a folder scan operation.
/// </summary>
public class ScanResult
{
    public int TotalDiscovered { get; set; }
    public int NewItems { get; set; }
    public int Duplicates { get; set; }
    public int AlreadyProcessed { get; set; }
    public List<MediaItem> UnprocessedItems { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
