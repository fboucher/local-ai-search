using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LocalAiSearch.ViewModels;

/// <summary>
/// Main ViewModel for the single-page application.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private MediaItemViewModel? _selectedItem;
    private string _searchQuery = string.Empty;
    private string _sortMode = "Date";
    
    public ObservableCollection<MediaItemViewModel> DisplayedItems { get; }
    
    public MediaItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value)
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string SortMode
    {
        get => _sortMode;
        set
        {
            if (_sortMode != value)
            {
                _sortMode = value;
                OnPropertyChanged();
            }
        }
    }
    
    public ICommand SortToggleCommand { get; }
    public ICommand LoadMoreCommand { get; }
    
    public MainViewModel()
    {
        DisplayedItems = new ObservableCollection<MediaItemViewModel>();
        SortToggleCommand = new RelayCommand(ToggleSort);
        LoadMoreCommand = new RelayCommand(LoadMore);
        
        // Seed with mock data
        SeedMockData();
    }
    
    private void ToggleSort()
    {
        SortMode = SortMode == "Date" ? "Name" : "Date";
        // TODO: Re-sort DisplayedItems based on SortMode
    }
    
    private void LoadMore()
    {
        // Add 10 more mock items for infinite scroll
        int currentCount = DisplayedItems.Count;
        for (int i = 0; i < 10; i++)
        {
            int index = currentCount + i + 1;
            DisplayedItems.Add(new MediaItemViewModel
            {
                Id = index,
                FilePath = $"/mock/image_{index}.jpg",
                Description = $"Mock image {index} - A beautiful scene",
                Tags = "nature, outdoor, mock",
                MediaType = "image",
                CreatedAt = DateTime.Now.AddDays(-index)
            });
        }
    }
    
    private void SeedMockData()
    {
        var mockDescriptions = new[]
        {
            "A beautiful sunset over the mountains",
            "A cat sleeping on a couch",
            "City skyline at night",
            "Fresh vegetables at a market",
            "A person hiking through a forest",
            "Abstract art with vibrant colors",
            "Coffee cup on a wooden table",
            "Beach waves crashing on shore",
            "Old bookshelf filled with books",
            "Flowers blooming in a garden",
            "Racing car on a track",
            "Snowy mountain peaks",
            "Modern architecture building",
            "Person working on a laptop",
            "Delicious pasta dish",
            "Puppy playing in grass",
            "Vintage camera on a shelf",
            "Rainy city street",
            "Colorful hot air balloons",
            "Musical instruments on stage"
        };
        
        var mockTags = new[]
        {
            "nature, outdoor, landscape",
            "animal, pet, indoor",
            "urban, architecture, night",
            "food, market, fresh",
            "outdoor, adventure, forest",
            "art, abstract, colorful",
            "indoor, beverage, still life",
            "nature, ocean, waves",
            "indoor, books, vintage",
            "nature, flowers, garden",
            "vehicle, sport, speed",
            "nature, winter, mountains",
            "architecture, modern, building",
            "technology, work, indoor",
            "food, cuisine, dinner",
            "animal, pet, outdoor",
            "vintage, technology, indoor",
            "urban, weather, street",
            "outdoor, colorful, sky",
            "music, instrument, indoor"
        };
        
        for (int i = 0; i < 20; i++)
        {
            DisplayedItems.Add(new MediaItemViewModel
            {
                Id = i + 1,
                FilePath = $"/mock/image_{i + 1}.jpg",
                Description = mockDescriptions[i],
                Tags = mockTags[i],
                MediaType = "image",
                CreatedAt = DateTime.Now.AddDays(-(i * 2))
            });
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Simple RelayCommand implementation for MVVM.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged;
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object? parameter) => _execute();
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
