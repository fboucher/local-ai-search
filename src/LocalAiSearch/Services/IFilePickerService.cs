namespace LocalAiSearch.Services;

/// <summary>
/// Abstracts OS file picker for MVVM cleanliness — ViewModel never touches UI APIs directly.
/// </summary>
public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickImagesAsync();
}
