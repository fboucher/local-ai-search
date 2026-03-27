# Skill: Uno GTK File Picker — Avoid GSettings Crash

## Problem

`FileOpenPicker` with `WinRT.Interop.InitializeWithWindow` causes a **fatal process crash** on macOS GTK/Skia:

```
GLib-GIO-ERROR **: 18:42:11.123: No GSettings schemas are installed on the system.
```

This is NOT catchable with try/catch — it kills the process at the GLib level. The crash occurs because initializing the native GTK file picker activates GTK's GSettings subsystem, which requires `glib-compile-schemas` output not present on macOS without Homebrew extras.

## Rule

> **Never use `FileOpenPicker` + `InitializeWithWindow` on Uno GTK/Skia.**

This includes all patterns like:
```csharp
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);  // ← fatal on macOS GTK
```

## GTK-Safe Replacement

Use a XAML `ContentDialog` with a `TextBox` for path input. No native OS picker, no GSettings.

```csharp
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
    // IMPORTANT: XamlRoot must be set — use instance access, not static App.MainWindow
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
```

## Gotchas

| Gotcha | Detail |
|--------|--------|
| `XamlRoot` access | `App.MainWindow` is an **instance** property, not static. Use `(Application.Current as App)?.MainWindow?.Content?.XamlRoot` |
| `AcceptsReturn = true` | Required for multi-line TextBox to accept Enter key for new lines |
| Path validation | Always validate with `File.Exists(p)` — user input is untrusted |
| Status feedback | When `paths.Count == 0`, set a `StatusMessage` (e.g. "No valid paths entered") — don't silently return |
| Remove usings | Remove `using Windows.Storage.Pickers` and `WinRT.Interop` — they trigger compile references to the failing subsystem |

## Why Not Platform-Conditional Code?

`#if __SKIA__` / `#if __MACOS__` could theoretically gate the picker. But:
1. The same GTK subsystem crash applies to all Skia targets (Linux included)
2. The XAML dialog is simpler, maintains one code path, and works everywhere

## Discovered

2026-03-27 — Livingston, fixing fatal crash on macOS GTK Skia  
Commit: 1938874 on dev
