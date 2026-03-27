using LocalAiSearch.Services;
using Microsoft.Data.Sqlite;

namespace LocalAiSearch.Tests;

public class ScanProgressServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseService _db;
    private readonly FolderScannerService _scanner;
    private readonly AiTaggingService _tagger;
    private readonly ScanProgressService _sut;
    private readonly string _tempDir;

    // Minimal valid PNG bytes (1×1 white pixel)
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F,
        0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59,
        0xE7, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    public ScanProgressServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new DatabaseService(_connection);
        _db.InitializeAsync().GetAwaiter().GetResult();

        _scanner = new FolderScannerService(_db);
        _tagger = new AiTaggingService(_db, endpointUrl: null); // stub mode
        _sut = new ScanProgressService(_scanner, _tagger);

        _tempDir = Path.Combine(Path.GetTempPath(), $"scan_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    /// <summary>Creates a minimal PNG at a given path and returns the path.</summary>
    private string WriteImageAt(string path)
    {
        File.WriteAllBytes(path, MinimalPng);
        return path;
    }

    private List<ScanProgress> CaptureProgress(out IProgress<ScanProgress> progress)
    {
        var events = new List<ScanProgress>();
        progress = new Progress<ScanProgress>(p => events.Add(p));
        return events;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_WithEmptyFolder_ReportsCompletePhase()
    {
        var events = CaptureProgress(out var progress);

        await _sut.RunAsync(_tempDir, progress);

        // Give the synchronous Progress<T> callbacks time to fire
        await Task.Delay(50);

        Assert.Contains(events, e => e.Phase == ScanPhase.Complete);
    }

    [Fact]
    public async Task RunAsync_ReportsProgressUpdates()
    {
        WriteImageAt(Path.Combine(_tempDir, "a.png"));
        WriteImageAt(Path.Combine(_tempDir, "b.png"));

        var events = CaptureProgress(out var progress);

        await _sut.RunAsync(_tempDir, progress);
        await Task.Delay(50);

        // Should have at minimum: Scanning start + two Tagging events + Complete
        Assert.Contains(events, e => e.Phase == ScanPhase.Scanning);
        var taggingEvents = events.Where(e => e.Phase == ScanPhase.Tagging).ToList();
        Assert.Equal(2, taggingEvents.Count);
        Assert.Equal(1, taggingEvents[0].Current);
        Assert.Equal(2, taggingEvents[0].Total);
        Assert.Equal(2, taggingEvents[1].Current);
        Assert.Equal(2, taggingEvents[1].Total);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_StopsProcessing()
    {
        // Create enough images that cancellation can fire mid-loop
        for (int i = 0; i < 5; i++)
            WriteImageAt(Path.Combine(_tempDir, $"img{i}.png"));

        var cts = new CancellationTokenSource();
        var taggingCount = 0;

        var progress = new Progress<ScanProgress>(p =>
        {
            if (p.Phase == ScanPhase.Tagging)
            {
                taggingCount++;
                if (taggingCount == 1)
                    cts.Cancel(); // cancel after first tagging event
            }
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.RunAsync(_tempDir, progress, cts.Token));

        // Fewer than 5 items should have been tagged
        Assert.True(taggingCount < 5);
    }

    [Fact]
    public async Task RunAsync_NewImages_GetTaggedAfterScan()
    {
        WriteImageAt(Path.Combine(_tempDir, "photo.png"));

        var events = CaptureProgress(out var progress);

        await _sut.RunAsync(_tempDir, progress);
        await Task.Delay(50);

        // Tagging phase must have been reported for the discovered image
        Assert.Contains(events, e => e.Phase == ScanPhase.Tagging);

        // The item must now exist in the database (was inserted during scan)
        var all = await _db.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task RunAsync_AlreadyProcessedImages_AreSkipped()
    {
        WriteImageAt(Path.Combine(_tempDir, "tagged.png"));

        // First run: scan + tag the image
        await _sut.RunAsync(_tempDir, progress: null);

        // Mark the item as tagged so scanner sees it as AlreadyProcessed
        var items = await _db.GetAllAsync();
        Assert.Single(items);
        items[0].IsTagged = true;
        items[0].Description = "pre-tagged";
        await _db.UpdateAsync(items[0]);

        // Second run: item is already processed — tagging phase should not fire
        var events = CaptureProgress(out var progress);
        await _sut.RunAsync(_tempDir, progress);
        await Task.Delay(50);

        Assert.DoesNotContain(events, e => e.Phase == ScanPhase.Tagging);
        Assert.Contains(events, e => e.Phase == ScanPhase.Complete);
    }

    [Fact]
    public async Task RunAsync_CompletionPhase_ReportedLast()
    {
        WriteImageAt(Path.Combine(_tempDir, "x.png"));

        var events = CaptureProgress(out var progress);

        await _sut.RunAsync(_tempDir, progress);
        await Task.Delay(50);

        Assert.NotEmpty(events);
        Assert.Equal(ScanPhase.Complete, events.Last().Phase);
    }
}
