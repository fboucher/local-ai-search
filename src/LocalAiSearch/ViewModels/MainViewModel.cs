using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using LocalAiSearch.Services;

namespace LocalAiSearch.ViewModels;

/// <summary>
/// Main ViewModel for the single-page application.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _db;
    private CancellationTokenSource? _searchCts;

    private MediaItemViewModel? _selectedItem;
    private string _searchQuery = string.Empty;
    private string _sortMode = "Date";
    private bool _sortAscending = false;
    private string _selectedMediaType = "All";
    private bool _isEmpty;

    public ObservableCollection<MediaItemViewModel> DisplayedItems { get; }

    public string[] MediaTypeOptions { get; } = { "All", "Photo", "Screenshot", "Image" };

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
                _ = LoadImagesAsync(debounce: true);
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

    public string SelectedMediaType
    {
        get => _selectedMediaType;
        set
        {
            if (_selectedMediaType != value)
            {
                _selectedMediaType = value;
                OnPropertyChanged();
                _ = LoadImagesAsync();
            }
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (_isEmpty != value)
            {
                _isEmpty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EmptyStateVisibility));
            }
        }
    }

    /// <summary>Drives empty-state panel visibility without requiring a XAML converter.</summary>
    public Visibility EmptyStateVisibility => _isEmpty ? Visibility.Visible : Visibility.Collapsed;

    public ICommand SortToggleCommand { get; }
    public ICommand LoadMoreCommand { get; }

    public MainViewModel() : this(new DatabaseService())
    {
    }

    public MainViewModel(DatabaseService db)
    {
        _db = db;
        DisplayedItems = new ObservableCollection<MediaItemViewModel>();
        SortToggleCommand = new RelayCommand(ToggleSort);
        LoadMoreCommand = new RelayCommand(() => { });
        _ = LoadImagesAsync();
    }

    private void ToggleSort()
    {
        if (SortMode == "Date")
        {
            SortMode = "Name";
        }
        else
        {
            SortMode = _sortAscending ? "Date" : "Name";
            _sortAscending = !_sortAscending;
        }
        _ = LoadImagesAsync();
    }

    public async Task LoadImagesAsync(bool debounce = false)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            if (debounce)
            {
                await Task.Delay(300, token);
            }

            if (token.IsCancellationRequested) return;

            List<LocalAiSearch.Models.MediaItem> results;

            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                results = await _db.GetAllAsync();
            }
            else
            {
                results = await _db.SearchAsync(_searchQuery);
            }

            if (token.IsCancellationRequested) return;

            // Apply media type filter client-side (case-insensitive)
            if (_selectedMediaType != "All")
            {
                var filter = _selectedMediaType.ToLowerInvariant();
                results = results.FindAll(r => r.MediaType.Equals(filter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply sort
            results = (SortMode, _sortAscending) switch
            {
                ("Date", false) => results.OrderByDescending(r => r.CreatedAt).ToList(),
                ("Date", true)  => results.OrderBy(r => r.CreatedAt).ToList(),
                ("Name", false) => results.OrderByDescending(r => r.FilePath).ToList(),
                _               => results.OrderBy(r => r.FilePath).ToList(),
            };

            DisplayedItems.Clear();
            foreach (var item in results)
            {
                DisplayedItems.Add(new MediaItemViewModel
                {
                    Id = item.Id,
                    FilePath = item.FilePath,
                    Description = item.Description,
                    Tags = item.Tags,
                    MediaType = item.MediaType,
                    CreatedAt = item.CreatedAt
                });
            }

            IsEmpty = DisplayedItems.Count == 0;
        }
        catch (OperationCanceledException)
        {
            // Search superseded by a newer query — ignore
        }
        catch (Exception)
        {
            // DB not yet initialised or no items — show empty state
            DisplayedItems.Clear();
            IsEmpty = true;
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
