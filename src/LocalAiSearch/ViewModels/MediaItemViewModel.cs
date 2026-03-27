using System;
using System.IO;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

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
    
    private ImageSource? _imageSource;

    /// <summary>
    /// Lazy-loaded thumbnail. Decoded at 160px width to reduce memory footprint.
    /// </summary>
    public ImageSource? ImageSource
    {
        get
        {
            if (_imageSource == null && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                var bitmap = new BitmapImage();
                bitmap.DecodePixelWidth = 160;
                bitmap.UriSource = new Uri(FilePath);
                _imageSource = bitmap;
            }
            return _imageSource;
        }
    }
    
    /// <summary>
    /// Display filename for UI.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath ?? string.Empty);
    
    /// <summary>
    /// Formatted date for display.
    /// </summary>
    public string FormattedDate => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
