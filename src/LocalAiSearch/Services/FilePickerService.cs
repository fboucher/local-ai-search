using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace LocalAiSearch.Services;

/// <summary>
/// GTK-safe file picker using a XAML ContentDialog with a folder browser + checkbox list.
/// The native FileOpenPicker with InitializeWithWindow triggers GLib-GIO-ERROR on macOS
/// because GTK's GSettings schemas are not installed. This avoids any native OS picker dependency.
/// </summary>
public class FilePickerService : IFilePickerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };

    public async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Add Images",
            PrimaryButtonText = "Add Selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        var root = new StackPanel { Spacing = 8, MinWidth = 480 };

        // Row 1: Folder path input + Browse button
        var folderRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var folderBox = new TextBox
        {
            PlaceholderText = "Folder path (e.g. /Users/frank/Pictures)",
            Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 360
        };
        var browseBtn = new Button { Content = "Browse Folder" };
        folderRow.Children.Add(folderBox);
        folderRow.Children.Add(browseBtn);
        root.Children.Add(folderRow);

        // Row 2: status label
        var statusLabel = new TextBlock
        {
            Text = "Enter a folder path and click Browse.",
            Foreground = new SolidColorBrush(Colors.Gray),
            FontSize = 12
        };
        root.Children.Add(statusLabel);

        // Row 3: scrollable checklist
        var listPanel = new StackPanel { Spacing = 4 };
        var scroll = new ScrollViewer
        {
            Content = listPanel,
            MaxHeight = 240,
            HorizontalScrollMode = ScrollMode.Disabled
        };
        root.Children.Add(scroll);

        dialog.Content = root;
        dialog.XamlRoot = (Application.Current as App)?.MainWindow?.Content?.XamlRoot;

        var checkedPaths = new HashSet<string>();

        void RefreshList(string folderPath)
        {
            listPanel.Children.Clear();
            checkedPaths.Clear();
            dialog.IsPrimaryButtonEnabled = false;

            if (!Directory.Exists(folderPath))
            {
                statusLabel.Text = "Folder not found.";
                return;
            }

            var images = Directory.EnumerateFiles(folderPath)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            if (images.Count == 0)
            {
                statusLabel.Text = "No supported images found in this folder.";
                return;
            }

            statusLabel.Text = $"{images.Count} image(s) found. Check the ones to import.";

            foreach (var path in images)
            {
                var cb = new CheckBox { Content = Path.GetFileName(path), Tag = path };
                cb.Checked += (_, _) =>
                {
                    checkedPaths.Add(path);
                    dialog.IsPrimaryButtonEnabled = checkedPaths.Count > 0;
                };
                cb.Unchecked += (_, _) =>
                {
                    checkedPaths.Remove(path);
                    dialog.IsPrimaryButtonEnabled = checkedPaths.Count > 0;
                };
                listPanel.Children.Add(cb);
            }
        }

        browseBtn.Click += (_, _) => RefreshList(folderBox.Text?.Trim() ?? string.Empty);
        browseBtn.Tapped += (_, _) => RefreshList(folderBox.Text?.Trim() ?? string.Empty);

        // Auto-refresh as user types a path (length > 3 to avoid rapid fs hits on partial paths)
        folderBox.TextChanged += (_, _) =>
        {
            var text = folderBox.Text?.Trim() ?? string.Empty;
            if (text.Length > 3)
                RefreshList(text);
        };

        // Auto-browse the pre-filled Pictures folder on open
        RefreshList(folderBox.Text?.Trim() ?? string.Empty);

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || checkedPaths.Count == 0)
            return Array.Empty<string>();

        return checkedPaths.ToList();
    }
}
