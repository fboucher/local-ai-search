using LocalAiSearch.Models;
using LocalAiSearch.Services;
using Microsoft.Data.Sqlite;

namespace LocalAiSearch.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly DatabaseService _service;
    private readonly SqliteConnection _connection;

    public DatabaseServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _service = new DatabaseService(_connection);
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTable()
    {
        await _service.InitializeAsync();
        var items = await _service.GetAllAsync();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task InsertAsync_AddsNewItem_ReturnsId()
    {
        await _service.InitializeAsync();

        var item = new MediaItem
        {
            FilePath = "/test/image.jpg",
            FileHash = "abc123",
            Description = "Test image",
            Tags = "test,image",
            MediaType = "image",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        var id = await _service.InsertAsync(item);
        
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectItem()
    {
        await _service.InitializeAsync();

        var item = new MediaItem
        {
            FilePath = "/test/image.jpg",
            FileHash = "abc123",
            Description = "Test image",
            Tags = "test,image",
            MediaType = "image",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        var id = await _service.InsertAsync(item);
        var retrieved = await _service.GetByIdAsync(id);

        Assert.NotNull(retrieved);
        Assert.Equal(id, retrieved.Id);
        Assert.Equal(item.FilePath, retrieved.FilePath);
        Assert.Equal(item.FileHash, retrieved.FileHash);
        Assert.Equal(item.Description, retrieved.Description);
        Assert.Equal(item.Tags, retrieved.Tags);
        Assert.Equal(item.IsTagged, retrieved.IsTagged);
    }

    [Fact]
    public async Task GetByHashAsync_ReturnsCorrectItem()
    {
        await _service.InitializeAsync();

        var item = new MediaItem
        {
            FilePath = "/test/image.jpg",
            FileHash = "unique_hash_123",
            Description = "Test image",
            Tags = "test,image",
            MediaType = "image",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        await _service.InsertAsync(item);
        var retrieved = await _service.GetByHashAsync("unique_hash_123");

        Assert.NotNull(retrieved);
        Assert.Equal(item.FileHash, retrieved.FileHash);
        Assert.Equal(item.FilePath, retrieved.FilePath);
    }

    [Fact]
    public async Task GetByHashAsync_ReturnsNull_WhenNotFound()
    {
        await _service.InitializeAsync();

        var retrieved = await _service.GetByHashAsync("nonexistent_hash");

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingItem()
    {
        await _service.InitializeAsync();

        var item = new MediaItem
        {
            FilePath = "/test/image.jpg",
            FileHash = "abc123",
            Description = "Original description",
            Tags = "test",
            MediaType = "image",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = false
        };

        var id = await _service.InsertAsync(item);
        item.Id = id;
        item.Description = "Updated description";
        item.Tags = "test,updated";
        item.IsTagged = true;
        item.UpdatedAt = DateTime.UtcNow;

        await _service.UpdateAsync(item);
        var retrieved = await _service.GetByIdAsync(id);

        Assert.NotNull(retrieved);
        Assert.Equal("Updated description", retrieved.Description);
        Assert.Equal("test,updated", retrieved.Tags);
        Assert.True(retrieved.IsTagged);
    }

    [Fact]
    public async Task SearchAsync_FindsByDescription()
    {
        await _service.InitializeAsync();

        var item1 = new MediaItem
        {
            FilePath = "/test/sunset.jpg",
            FileHash = "hash1",
            Description = "Beautiful sunset over mountains",
            Tags = "nature,landscape",
            MediaType = "image",
            FileSizeBytes = 2048,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        var item2 = new MediaItem
        {
            FilePath = "/test/city.jpg",
            FileHash = "hash2",
            Description = "City skyline at night",
            Tags = "urban,architecture",
            MediaType = "image",
            FileSizeBytes = 3072,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        await _service.InsertAsync(item1);
        await _service.InsertAsync(item2);

        var results = await _service.SearchAsync("sunset");

        Assert.Single(results);
        Assert.Equal(item1.FilePath, results[0].FilePath);
    }

    [Fact]
    public async Task SearchAsync_FindsByTags()
    {
        await _service.InitializeAsync();

        var item1 = new MediaItem
        {
            FilePath = "/test/mountain.jpg",
            FileHash = "hash1",
            Description = "Mountain view",
            Tags = "nature,landscape,outdoor",
            MediaType = "image",
            FileSizeBytes = 2048,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        var item2 = new MediaItem
        {
            FilePath = "/test/office.jpg",
            FileHash = "hash2",
            Description = "Office space",
            Tags = "indoor,work",
            MediaType = "image",
            FileSizeBytes = 1536,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        await _service.InsertAsync(item1);
        await _service.InsertAsync(item2);

        var results = await _service.SearchAsync("landscape");

        Assert.Single(results);
        Assert.Equal(item1.FilePath, results[0].FilePath);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        await _service.InitializeAsync();

        var item1 = new MediaItem
        {
            FilePath = "/test/image1.jpg",
            FileHash = "hash1",
            Description = "Image 1",
            Tags = "test",
            MediaType = "image",
            FileSizeBytes = 1024,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        var item2 = new MediaItem
        {
            FilePath = "/test/image2.jpg",
            FileHash = "hash2",
            Description = "Image 2",
            Tags = "test",
            MediaType = "image",
            FileSizeBytes = 2048,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsTagged = true
        };

        await _service.InsertAsync(item1);
        await _service.InsertAsync(item2);

        var results = await _service.GetAllAsync();

        Assert.Equal(2, results.Count);
    }
}
