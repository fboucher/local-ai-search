using LocalAiSearch.Models;
using LocalAiSearch.Services;
using Microsoft.Data.Sqlite;

namespace LocalAiSearch.Tests;

public class FolderScannerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseService _db;
    private readonly FolderScannerService _scanner;

    public FolderScannerServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new DatabaseService(_connection);
        _db.InitializeAsync().Wait();
        _scanner = new FolderScannerService(_db);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    [Fact]
    public void IsSupportedImage_SupportedFormats_ReturnsTrue()
    {
        Assert.True(FolderScannerService.IsSupportedImage("test.jpg"));
        Assert.True(FolderScannerService.IsSupportedImage("test.JPG"));
        Assert.True(FolderScannerService.IsSupportedImage("test.jpeg"));
        Assert.True(FolderScannerService.IsSupportedImage("test.png"));
        Assert.True(FolderScannerService.IsSupportedImage("test.webp"));
        Assert.True(FolderScannerService.IsSupportedImage("test.gif"));
        Assert.True(FolderScannerService.IsSupportedImage("test.bmp"));
    }

    [Fact]
    public void IsSupportedImage_UnsupportedFormats_ReturnsFalse()
    {
        Assert.False(FolderScannerService.IsSupportedImage("test.cr2"));
        Assert.False(FolderScannerService.IsSupportedImage("test.nef"));
        Assert.False(FolderScannerService.IsSupportedImage("test.txt"));
        Assert.False(FolderScannerService.IsSupportedImage("test.mp4"));
    }

    [Fact]
    public void IsRawFormat_RawFormats_ReturnsTrue()
    {
        Assert.True(FolderScannerService.IsRawFormat("test.cr2"));
        Assert.True(FolderScannerService.IsRawFormat("test.CR2"));
        Assert.True(FolderScannerService.IsRawFormat("test.nef"));
        Assert.True(FolderScannerService.IsRawFormat("test.arw"));
        Assert.True(FolderScannerService.IsRawFormat("test.dng"));
        Assert.True(FolderScannerService.IsRawFormat("test.orf"));
        Assert.True(FolderScannerService.IsRawFormat("test.rw2"));
        Assert.True(FolderScannerService.IsRawFormat("test.raf"));
        Assert.True(FolderScannerService.IsRawFormat("test.pef"));
        Assert.True(FolderScannerService.IsRawFormat("test.srw"));
    }

    [Fact]
    public void IsRawFormat_NonRawFormats_ReturnsFalse()
    {
        Assert.False(FolderScannerService.IsRawFormat("test.jpg"));
        Assert.False(FolderScannerService.IsRawFormat("test.png"));
        Assert.False(FolderScannerService.IsRawFormat("test.txt"));
    }

    [Fact]
    public async Task ComputeFileHashAsync_SameFile_ReturnsSameHash()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "test content");

            var hash1 = await FolderScannerService.ComputeFileHashAsync(tempFile);
            var hash2 = await FolderScannerService.ComputeFileHashAsync(tempFile);

            Assert.Equal(hash1, hash2);
            Assert.NotEmpty(hash1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeFileHashAsync_DifferentContent_ReturnsDifferentHash()
    {
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile1, "content 1");
            await File.WriteAllTextAsync(tempFile2, "content 2");

            var hash1 = await FolderScannerService.ComputeFileHashAsync(tempFile1);
            var hash2 = await FolderScannerService.ComputeFileHashAsync(tempFile2);

            Assert.NotEqual(hash1, hash2);
        }
        finally
        {
            File.Delete(tempFile1);
            File.Delete(tempFile2);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_NewImages_CreatesNewItems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test images
            var img1 = Path.Combine(tempDir, "image1.jpg");
            var img2 = Path.Combine(tempDir, "image2.png");
            await File.WriteAllTextAsync(img1, "image1 content");
            await File.WriteAllTextAsync(img2, "image2 content");

            var result = await _scanner.ScanFolderAsync(tempDir);

            Assert.Equal(2, result.TotalDiscovered);
            Assert.Equal(2, result.NewItems);
            Assert.Equal(0, result.Duplicates);
            Assert.Equal(0, result.AlreadyProcessed);
            Assert.Equal(2, result.UnprocessedItems.Count);
            Assert.Empty(result.Errors);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_SkipsRawFormats()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            var img1 = Path.Combine(tempDir, "image1.jpg");
            var raw1 = Path.Combine(tempDir, "image2.cr2");
            var raw2 = Path.Combine(tempDir, "image3.nef");
            await File.WriteAllTextAsync(img1, "image1 content");
            await File.WriteAllTextAsync(raw1, "raw1 content");
            await File.WriteAllTextAsync(raw2, "raw2 content");

            var result = await _scanner.ScanFolderAsync(tempDir);

            Assert.Equal(1, result.TotalDiscovered); // Only jpg counted
            Assert.Equal(1, result.NewItems);
            Assert.Equal(1, result.UnprocessedItems.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_SecondScan_DetectsDuplicates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test image
            var img1 = Path.Combine(tempDir, "image1.jpg");
            await File.WriteAllTextAsync(img1, "image1 content");

            // First scan
            var result1 = await _scanner.ScanFolderAsync(tempDir);
            Assert.Equal(1, result1.NewItems);

            // Second scan
            var result2 = await _scanner.ScanFolderAsync(tempDir);
            Assert.Equal(1, result2.TotalDiscovered);
            Assert.Equal(0, result2.NewItems);
            Assert.Equal(1, result2.Duplicates);
            Assert.Equal(0, result2.AlreadyProcessed);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_TaggedItems_MarkedAsAlreadyProcessed()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test image
            var img1 = Path.Combine(tempDir, "image1.jpg");
            await File.WriteAllTextAsync(img1, "image1 content");

            // First scan
            var result1 = await _scanner.ScanFolderAsync(tempDir);
            Assert.Equal(1, result1.NewItems);

            // Tag the item
            var item = result1.UnprocessedItems[0];
            item.IsTagged = true;
            item.Description = "Test description";
            await _db.UpdateAsync(item);

            // Second scan
            var result2 = await _scanner.ScanFolderAsync(tempDir);
            Assert.Equal(1, result2.TotalDiscovered);
            Assert.Equal(0, result2.NewItems);
            Assert.Equal(0, result2.Duplicates);
            Assert.Equal(1, result2.AlreadyProcessed);
            Assert.Empty(result2.UnprocessedItems); // Already processed items not included
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_RecursiveSearch_FindsNestedImages()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            // Create test images in different levels
            var img1 = Path.Combine(tempDir, "image1.jpg");
            var img2 = Path.Combine(subDir, "image2.png");
            await File.WriteAllTextAsync(img1, "image1 content");
            await File.WriteAllTextAsync(img2, "image2 content");

            var result = await _scanner.ScanFolderAsync(tempDir);

            Assert.Equal(2, result.TotalDiscovered);
            Assert.Equal(2, result.NewItems);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanFolderAsync_NonExistentDirectory_ReturnsError()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var result = await _scanner.ScanFolderAsync(nonExistentDir);

        Assert.Equal(0, result.TotalDiscovered);
        Assert.Single(result.Errors);
        Assert.Contains("does not exist", result.Errors[0]);
    }
}
