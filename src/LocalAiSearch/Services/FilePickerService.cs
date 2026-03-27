using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LocalAiSearch.Services;

/// <summary>
/// GTK-safe file picker using a XAML ContentDialog with a TextBox.
/// The native FileOpenPicker with InitializeWithWindow triggers GLib-GIO-ERROR on macOS
/// because GTK's GSettings schemas are not installed. This avoids any native OS picker dependency.
/// </summary>
public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Add Images",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var stackPanel = new StackPanel { Spacing = 8 };
        stackPanel.Children.Add(new TextBlock
        {
            Text = "Enter image file paths (one per line):",
            TextWrapping = TextWrapping.Wrap
        });

        var textBox = new TextBox
        {
            PlaceholderText = "/Users/frank/Pictures/photo.jpg",
            AcceptsReturn = true,
            Height = 120,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(new TextBlock
        {
            Text = "Supported: .jpg .jpeg .png .webp .gif .bmp",
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            FontSize = 12
        });

        dialog.Content = stackPanel;
        dialog.XamlRoot = (Application.Current as App)?.MainWindow?.Content?.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return Array.Empty<string>();

        var raw = textBox.Text ?? string.Empty;
        var paths = raw
            .Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0 && File.Exists(p))
            .ToList();

        return paths;
    }
}
