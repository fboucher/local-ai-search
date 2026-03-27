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
    private readonly ScanProgressService _scanProgress;
    private readonly ImageImportService _imageImport;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _scanCts;

    private MediaItemViewModel? _selectedItem;
    private string _searchQuery = string.Empty;
    private string _sortMode = "Date";
    private bool _sortAscending = false;
    private string _selectedMediaType = "All";
    private bool _isEmpty;
    private string _statusMessage = string.Empty;
    private bool _isScanning;
    private string _scanProgressText = string.Empty;
    private string _currentScanFile = string.Empty;
    private double _scanProgressValue;

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

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (_isScanning != value)
            {
                _isScanning = value;
                OnPropertyChanged();
                ((RelayCommand)RescanCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelScanCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string ScanProgressText
    {
        get => _scanProgressText;
        private set
        {
            if (_scanProgressText != value)
            {
                _scanProgressText = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentScanFile
    {
        get => _currentScanFile;
        private set
        {
            if (_currentScanFile != value)
            {
                _currentScanFile = value;
                OnPropertyChanged();
            }
        }
    }

    public double ScanProgressValue
    {
        get => _scanProgressValue;
        private set
        {
            if (_scanProgressValue != value)
            {
                _scanProgressValue = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SortToggleCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand RescanCommand { get; }
    public ICommand CancelScanCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusMessageVisibility));
            }
        }
    }

    /// <summary>Drives status bar visibility without requiring a XAML converter.</summary>
    public Visibility StatusMessageVisibility =>
        string.IsNullOrEmpty(_statusMessage) ? Visibility.Collapsed : Visibility.Visible;

    public MainViewModel() : this(new DatabaseService())
    {
    }

    public MainViewModel(DatabaseService db)
    {
        _db = db;
        _imageImport = new ImageImportService(db);
        var scanner = new FolderScannerService(db);
        var tagger = new AiTaggingService(db);
        _scanProgress = new ScanProgressService(scanner, tagger);
        DisplayedItems = new ObservableCollection<MediaItemViewModel>();
        SortToggleCommand = new RelayCommand(ToggleSort);
        LoadMoreCommand = new RelayCommand(() => { });
        RescanCommand = new RelayCommand(() => _ = StartRescanAsync(), () => !_isScanning);
        CancelScanCommand = new RelayCommand(() => _scanCts?.Cancel(), () => _isScanning);
        _ = LoadImagesAsync();
    }

    public MainViewModel(DatabaseService db, ScanProgressService scanProgressService)
    {
        _db = db;
        _imageImport = new ImageImportService(db);
        _scanProgress = scanProgressService;
        DisplayedItems = new ObservableCollection<MediaItemViewModel>();
        SortToggleCommand = new RelayCommand(ToggleSort);
        LoadMoreCommand = new RelayCommand(() => { });
        RescanCommand = new RelayCommand(() => _ = StartRescanAsync(), () => !_isScanning);
        CancelScanCommand = new RelayCommand(() => _scanCts?.Cancel(), () => _isScanning);
        _ = LoadImagesAsync();
    }

    private async Task StartRescanAsync()
    {
        if (_isScanning) return;

        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        ScanProgressText = "Starting scan…";
        ScanProgressValue = 0;
        CurrentScanFile = string.Empty;

        var progress = new Progress<ScanProgress>(p =>
        {
            CurrentScanFile = p.CurrentFile;
            switch (p.Phase)
            {
                case ScanPhase.Scanning:
                    ScanProgressText = "Scanning folder…";
                    ScanProgressValue = 0;
                    break;
                case ScanPhase.Tagging:
                    ScanProgressText = p.Total > 0
                        ? $"Processing image {p.Current} of {p.Total}"
                        : "Tagging images…";
                    ScanProgressValue = p.Total > 0 ? (double)p.Current / p.Total * 100.0 : 0;
                    break;
                case ScanPhase.Complete:
                    ScanProgressText = "Scan complete";
                    ScanProgressValue = 100;
                    CurrentScanFile = string.Empty;
                    break;
            }
        });

        try
        {
            await _scanProgress.RunAsync(null, progress, _scanCts.Token);
        }
        catch (OperationCanceledException)
        {
            ScanProgressText = "Scan cancelled";
        }
        catch (Exception ex)
        {
            ScanProgressText = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
            await LoadImagesAsync();
        }
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

    public async Task ImportImagesAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        var result = await _imageImport.ImportAsync(paths);
        StatusMessage = $"Added {result.Added}, skipped {result.Skipped} duplicate(s).";
        await LoadImagesAsync();
        _ = ClearStatusAfterDelayAsync();
    }

    private async Task ClearStatusAfterDelayAsync()
    {
        await Task.Delay(4000);
        StatusMessage = string.Empty;
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

            await _db.InitializeAsync();

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
