using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NativeFileDialogExtendedSharp;
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

        AddImagesBtn.Click += (_, _) => ShowAddImagesPanel();
        BrowseFolderBtn.Click += async (_, _) =>
        {
            var result = Nfd.PickFolder();
            if (result.Status == NfdStatus.Ok && result.Path is string path)
            {
                FolderPathBox.Text = path;
                RefreshImageList(path);
            }
        };
        CancelAddImagesBtn.Click += (_, _) => HideAddImagesPanel();
        AddSelectedBtn.Click += async (_, _) => await CommitSelectedImages();
    }

    private void ShowAddImagesPanel()
    {
        _checkedImagePaths.Clear();
        ImageChecklistPanel.Children.Clear();
        AddSelectedBtn.IsEnabled = false;
        FolderStatusLabel.Text = "Click Browse to pick a folder.";
        AddImagesPanel.Visibility = Visibility.Visible;
    }

    private void HideAddImagesPanel()
    {
        AddImagesPanel.Visibility = Visibility.Collapsed;
        ImageChecklistPanel.Children.Clear();
        _checkedImagePaths.Clear();
        AddSelectedBtn.IsEnabled = false;
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
