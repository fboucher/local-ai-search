namespace LocalAiSearch.Services;

public enum ScanPhase { Scanning, Tagging, Complete }

public record ScanProgress(int Current, int Total, string CurrentFile, ScanPhase Phase);

/// <summary>
/// Orchestrates FolderScannerService → AiTaggingService with IProgress&lt;ScanProgress&gt; reporting.
/// </summary>
public class ScanProgressService
{
    private readonly FolderScannerService _scanner;
    private readonly AiTaggingService _tagger;

    public ScanProgressService(FolderScannerService scanner, AiTaggingService tagger)
    {
        _scanner = scanner;
        _tagger = tagger;
    }

    /// <summary>
    /// Runs a full scan + tagging cycle, reporting progress at each step.
    /// Uses SCAN_FOLDER env var, falling back to MyPictures.
    /// </summary>
    public async Task RunAsync(
        string? folderPath,
        IProgress<ScanProgress>? progress,
        CancellationToken ct = default)
    {
        var folder = folderPath
            ?? Environment.GetEnvironmentVariable("SCAN_FOLDER")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Phase 1: Scan
        progress?.Report(new ScanProgress(0, 0, folder, ScanPhase.Scanning));

        var scanResult = await _scanner.ScanFolderAsync(folder, ct);

        ct.ThrowIfCancellationRequested();

        // Phase 2: Tag unprocessed items one by one
        var items = scanResult.UnprocessedItems;
        int total = items.Count;

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();

            var item = items[i];
            progress?.Report(new ScanProgress(i + 1, total, item.FilePath, ScanPhase.Tagging));

            try
            {
                await _tagger.TagImageAsync(item, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ScanProgressService] Skipped '{item.FilePath}': {ex.Message}");
            }
        }

        // Phase 3: Complete
        progress?.Report(new ScanProgress(total, total, string.Empty, ScanPhase.Complete));
    }
}
