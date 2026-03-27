using Windows.Storage.Pickers;

namespace LocalAiSearch.Services;

/// <summary>
/// Uno Platform cross-platform file picker using Windows.Storage.Pickers.FileOpenPicker.
/// Instantiated in code-behind to keep ViewModel free of UI dependencies.
/// </summary>
public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".bmp");

        var files = await picker.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return Array.Empty<string>();

        return files
            .Select(f => f.Path)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }
}
