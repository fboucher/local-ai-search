using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LocalAiSearch.ViewModels;

namespace LocalAiSearch;

/// <summary>
/// Main page of the application.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    
    public MainPage()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
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
        
        // Trigger load more when scrolled near bottom (within 100 pixels)
        if (maxVerticalOffset - verticalOffset < 100)
        {
            ViewModel.LoadMoreCommand.Execute(null);
        }
    }
}
