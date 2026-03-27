# Skill: Uno MVVM Debounced Search with Filter

**Category:** Uno Platform / MVVM
**Author:** Livingston
**Slice:** #7 (Search & Filter)

## Pattern: Debounced Search with CancellationTokenSource

Use this pattern in a ViewModel to debounce live-search-on-keypress against any async data source.

```csharp
private CancellationTokenSource? _searchCts;

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

public async Task LoadImagesAsync(bool debounce = false)
{
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

    try
    {
        if (debounce)
            await Task.Delay(300, token);

        if (token.IsCancellationRequested) return;

        var results = string.IsNullOrWhiteSpace(_searchQuery)
            ? await _db.GetAllAsync()
            : await _db.SearchAsync(_searchQuery);

        if (token.IsCancellationRequested) return;

        DisplayedItems.Clear();
        foreach (var item in results)
            DisplayedItems.Add(ToViewModel(item));

        IsEmpty = DisplayedItems.Count == 0;
    }
    catch (OperationCanceledException) { /* superseded */ }
    catch (Exception) { IsEmpty = true; }
}
```

## Pattern: EmptyState Without Converter

Expose `Visibility EmptyStateVisibility` from the ViewModel to avoid registering a BoolToVisibilityConverter in XAML (WinUI 3 has none built-in):

```csharp
// ViewModel
using Microsoft.UI.Xaml;

public bool IsEmpty { get => _isEmpty; private set { _isEmpty = value; OnPropertyChanged(); OnPropertyChanged(nameof(EmptyStateVisibility)); } }
public Visibility EmptyStateVisibility => _isEmpty ? Visibility.Visible : Visibility.Collapsed;
```

```xml
<!-- XAML -->
<StackPanel Visibility="{x:Bind ViewModel.EmptyStateVisibility, Mode=OneWay}">
  <TextBlock Text="No results found." />
</StackPanel>
```

## Pattern: ComboBox Filter Bound to String Property

Expose an instance `string[]` property for filter options and bind both `ItemsSource` and `SelectedItem`:

```csharp
// ViewModel — MUST be instance property, not static (x:Bind requires instance path)
public string[] MediaTypeOptions { get; } = { "All", "Photo", "Screenshot", "Image" };
public string SelectedMediaType { get => ...; set { ... OnPropertyChanged(); _ = LoadImagesAsync(); } }
```

```xml
<ComboBox ItemsSource="{x:Bind ViewModel.MediaTypeOptions}"
          SelectedItem="{x:Bind ViewModel.SelectedMediaType, Mode=TwoWay}"
          PlaceholderText="Media type"/>
```

## Gotcha: static Members Can't Be Bound via {x:Bind}

`static readonly` fields accessed via `{x:Bind ViewModel.Field}` cause **CS0176** at build time. The Uno/WinUI source generator emits instance-qualified access. Always use an instance property.

## Gotcha: UpdateSourceTrigger for Live Search

```xml
<TextBox Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

Without `UpdateSourceTrigger=PropertyChanged`, the TextBox only pushes value on focus-lost — live search won't work.
