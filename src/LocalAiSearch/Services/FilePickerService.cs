using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace LocalAiSearch.Services;

/// <summary>
/// Uno Platform cross-platform file picker using Windows.Storage.Pickers.FileOpenPicker.
/// On macOS GTK (Skia renderer), the picker must be initialized with the native window handle
/// via WinRT.Interop — without this step, PickMultipleFilesAsync silently fails on GTK.
/// </summary>
public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var window = (Application.Current as App)?.MainWindow
            ?? throw new InvalidOperationException("MainWindow is not available. Cannot open file picker.");

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        // Required on all non-Windows Uno Skia targets (GTK/macOS, WPF, Linux).
        // Without this the picker throws or returns empty silently.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

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
