using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LocalAiSearch.Services;
using LocalAiSearch.ViewModels;

namespace LocalAiSearch;

/// <summary>
/// Main page of the application.
/// </summary>
public sealed partial class MainPage : Page
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };

    private readonly HashSet<string> _checkedImagePaths = new();

    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new DatabaseService());
        FolderPathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        AddImagesBtn.Click += (_, _) => ShowAddImagesPanel();
        BrowseFolderBtn.Click += (_, _) => NavigateTo(FolderPathBox.Text?.Trim() ?? "");
        FolderPathBox.TextChanged += (_, _) =>
        {
            var text = FolderPathBox.Text?.Trim() ?? "";
            if (text.Length > 3) RefreshImageList(text);
        };
        CancelAddImagesBtn.Click += (_, _) => HideAddImagesPanel();
        AddSelectedBtn.Click += async (_, _) => await CommitSelectedImages();

        ShortcutPictures.Click += (_, _) => NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        ShortcutDownloads.Click += (_, _) => NavigateTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
        ShortcutDocuments.Click += (_, _) => NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        ShortcutDesktop.Click += (_, _) => NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        ShortcutHome.Click += (_, _) => NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private void ShowAddImagesPanel()
    {
        _checkedImagePaths.Clear();
        AddImagesPanel.Visibility = Visibility.Visible;
        var path = FolderPathBox.Text?.Trim() ?? "";
        RefreshFolderList(path);
        RefreshImageList(path);
    }

    private void HideAddImagesPanel()
    {
        AddImagesPanel.Visibility = Visibility.Collapsed;
        ImageChecklistPanel.Children.Clear();
        _checkedImagePaths.Clear();
        AddSelectedBtn.IsEnabled = false;
    }

    private void NavigateTo(string path)
    {
        FolderPathBox.Text = path;
        RefreshFolderList(path);
        RefreshImageList(path);
    }

    private void RefreshFolderList(string folderPath)
    {
        SubfolderPanel.Children.Clear();
        if (!Directory.Exists(folderPath)) return;

        var parent = Directory.GetParent(folderPath)?.FullName;
        var upBtn = new Button
        {
            Content = "↑ Up",
            IsEnabled = parent != null,
            Margin = new Thickness(0, 0, 0, 4)
        };
        if (parent != null)
            upBtn.Click += (_, _) =>
            {
                FolderPathBox.Text = parent;
                RefreshFolderList(parent);
                RefreshImageList(parent);
            };
        SubfolderPanel.Children.Add(upBtn);

        try
        {
            var dirs = Directory.EnumerateDirectories(folderPath)
                .OrderBy(d => Path.GetFileName(d))
                .ToList();
            foreach (var dir in dirs)
            {
                var name = Path.GetFileName(dir);
                var btn = new Button { Content = $"📁 {name}", Tag = dir };
                btn.Click += (_, _) =>
                {
                    FolderPathBox.Text = dir;
                    RefreshFolderList(dir);
                    RefreshImageList(dir);
                };
                SubfolderPanel.Children.Add(btn);
            }
        }
        catch (UnauthorizedAccessException) { /* skip inaccessible dirs */ }
    }

    private void RefreshImageList(string folderPath)
    {
        ImageChecklistPanel.Children.Clear();
        _checkedImagePaths.Clear();
        AddSelectedBtn.IsEnabled = false;

        if (!Directory.Exists(folderPath))
        {
            FolderStatusLabel.Text = "Folder not found.";
            return;
        }

        var images = Directory.EnumerateFiles(folderPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        if (images.Count == 0)
        {
            FolderStatusLabel.Text = "No supported images found in this folder.";
            return;
        }

        FolderStatusLabel.Text = $"{images.Count} image(s) found. Check the ones to import.";

        foreach (var imagePath in images)
        {
            var cb = new CheckBox { Content = Path.GetFileName(imagePath), Tag = imagePath };
            cb.Checked += (_, _) => { _checkedImagePaths.Add(imagePath); AddSelectedBtn.IsEnabled = true; };
            cb.Unchecked += (_, _) => { _checkedImagePaths.Remove(imagePath); AddSelectedBtn.IsEnabled = _checkedImagePaths.Count > 0; };
            ImageChecklistPanel.Children.Add(cb);
        }
    }

    private async Task CommitSelectedImages()
    {
        var paths = _checkedImagePaths.ToList();
        HideAddImagesPanel();
        await ViewModel.ImportImagesAsync(paths);
    }

    private void OnThumbnailSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ThumbnailGrid.SelectedItem is MediaItemViewModel selectedItem)
        {
            ViewModel.SelectedItem = selectedItem;
        }
    }

    private void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender;
        var verticalOffset = scrollViewer.VerticalOffset;
        var maxVerticalOffset = scrollViewer.ScrollableHeight;

        if (maxVerticalOffset - verticalOffset < 100)
        {
            ViewModel.LoadMoreCommand.Execute(null);
        }
    }
}
