namespace LocalAiSearch.Models;

/// <summary>
/// Represents a media item (image/video) with metadata and AI-generated tags.
/// </summary>
public class MediaItem
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsTagged { get; set; }
}
