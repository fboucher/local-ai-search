using System;
using Microsoft.UI.Xaml.Media;

namespace LocalAiSearch.ViewModels;

/// <summary>
/// ViewModel wrapper for a media item, used for display in the UI.
/// </summary>
public class MediaItemViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Thumbnail image source. Null for now (placeholder mode).
    /// </summary>
    public ImageSource? Thumbnail { get; set; }
    
    /// <summary>
    /// Display filename for UI.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);
    
    /// <summary>
    /// Formatted date for display.
    /// </summary>
    public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
