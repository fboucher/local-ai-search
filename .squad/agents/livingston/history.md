## Project Context

**Project:** local-ai-search — Local AI Search Image Management Application
**User:** Frank Boucher
**Stack:** Uno Platform 5.x + .NET 8 (C#), Turso/libsql, Reka AI API (LAN), SkiaSharp
**Architecture:**
- Models/: MediaItem (id, filePath, description, mediaType, tags, fileHash, timestamps)
- Services/: FolderScannerService, AiTaggingService, DatabaseService, ImageDisplayService
- ViewModels/: MainViewModel
- Views/: MainPage.xaml (single-page app)
**Database:** Turso local file (libsql://file:./local.db), SQLite schema
**AI API:** Reka Edge (reka-edge-2603), POST /v1/chat/completions, Base64 image encoding
**Issues:** 8 slices in GitHub — #2 [HITL] Bootstrap, #3 Database, #4 Image Viewer, #5 Folder Scanner, #6 AI Tagging, #7 Search & Filter, #8 Rescan & Progress, #9 Theming

## Learnings

### Search & Filter UI (Slice #7) — 2026-03-27

**Built:** Wired search bar, media type filter, sort toggle, and empty state to real DatabaseService.

**Changes Made:**
1. **MainViewModel.cs**: Added DatabaseService injection (default `./local.db`), SearchQuery 300ms debounce via CancellationTokenSource, SelectedMediaType property, IsEmpty + EmptyStateVisibility (Visibility type — avoids XAML converter), LoadImagesAsync that calls GetAllAsync/SearchAsync + client-side MediaType filter + sort.
2. **MainPage.xaml**: Added ComboBox for media type (ItemsSource bound to ViewModel.MediaTypeOptions), wired TextBox with UpdateSourceTrigger=PropertyChanged, added empty state StackPanel visible when EmptyStateVisibility is set.
3. **LocalAiSearch.csproj**: Added Microsoft.Data.Sqlite package (was missing — pre-existing issue blocking build).

**MVVM Patterns Used:**
- Debounce with CancellationTokenSource + Task.Delay(300ms) — cancel previous on new input
- Expose `Visibility EmptyStateVisibility` property from ViewModel instead of XAML converter (pragmatic for Uno/WinUI)
- `MediaTypeOptions` must be an instance property, not `static readonly` — XAML source generator accesses it via instance path through {x:Bind ViewModel.X}

**Uno Platform Gotchas:**
- `static readonly` fields on ViewModel bound via `{x:Bind ViewModel.Field}` cause CS0176 ("cannot be accessed with an instance reference") — always use instance property for anything bound via {x:Bind}
- `UpdateSourceTrigger=PropertyChanged` on TextBox is required for live-on-keypress search

**Build Result:** ✅ 0 errors, 0 warnings

**PR:** #15 targeting dev

### Basic Image Viewer UI (Slice #4) — 2025-03-26

**Built:** Complete two-panel image viewer UI with MVVM pattern and mock data.

**Components Created:**
1. **MediaItemViewModel.cs**: Display wrapper for media items with ImageSource (null for now), FileName, FormattedDate computed properties
2. **MainViewModel.cs**: Full MVVM implementation with ObservableCollection<MediaItemViewModel>, INotifyPropertyChanged, RelayCommand helper, sort toggle (Date/Name), LoadMoreCommand for infinite scroll. Seeds 20 mock items with diverse descriptions/tags.
3. **MainPage.xaml**: Two-panel layout (2:1 ratio):
   - Left: Search bar + Sort toggle button + GridView with 150x150 fixed tiles (placeholder emoji + filename)
   - Right: Full preview area + metadata panel (Description, Tags, MediaType, Date)
4. **MainPage.xaml.cs**: ViewModel instantiation, SelectionChanged handler, ScrollViewer.ViewChanged for infinite scroll trigger

**XAML Patterns Used:**
- `x:Bind` with Mode=OneWay/TwoWay for bindings (Uno Platform WinUI3 syntax)
- Explicit `Background="White"` (no ThemeResource — GTK lesson from Slice #1)
- Fixed-size tiles in GridView.ItemTemplate with Border + TextBlock placeholder
- Grid.ColumnDefinitions for two-panel layout (2* and *)
- ScrollViewer.ViewChanged event for near-bottom detection (100px threshold)

**Uno Platform Gotchas Discovered:**
- GridView works fine for fixed-size tiles on GTK (no need for ItemsRepeater)
- `x:Bind` requires ViewModel as public property on Page, not just DataContext
- Spacing attribute on StackPanel works on Uno 5.x
- Explicit HorizontalAlignment/VerticalAlignment on Grid still required (GTK lesson carries forward)

**Mock Data Approach:**
- 20 items seeded in constructor with realistic descriptions ("A beautiful sunset...", "A cat sleeping...")
- Placeholder emoji (📷) in Border for thumbnails (no image files yet)
- LoadMoreCommand appends 10 more items on scroll (infinite scroll simulation)
- All ImageSource properties null (real image loading comes in later slice)

**Build Result:** ✅ 0 errors, 0 warnings on `dotnet build`

**PR:** #11 opened against squad/2-project-bootstrap branch

### 2026-03-27 — Slice #7: Search & Filter Integration

**Branch:** `squad/7-search-filter`  
**PR:** #15 (targeting dev)  
**Status:** ✅ Complete (Ready for review)  

**What I updated:**
- **MainViewModel.cs** — Wired to real DatabaseService:
  - SearchQuery property (string) binding from TextBox
  - SelectedMediaType property (string) binding from ComboBox
  - EmptyStateVisibility computed property for no-results display
  - Debounced search using `CancellationTokenSource` + `Task.Delay(300ms)`
  - Filter integration: combines search text + media type in database query
  - LoadMoreCommand preserved for infinite scroll

- **Debounce pattern:**
  - On each SearchQuery/SelectedMediaType change: cancel previous CTS
  - Delay 300ms, then call `LoadItemsAsync()` with combined filters
  - Prevents excessive database queries during rapid typing

- **UI Patterns:**
  - `EmptyStateVisibility` exposed from ViewModel instead of XAML converter
  - MediaTypeOptions as instance property (required for `x:Bind` code generation)
  - No custom BoolToVisibilityConverter (avoids XAML boilerplate)

- **Bug Fix:**
  - Added missing `<PackageReference Include="Microsoft.Data.Sqlite" Version="*" />` to csproj
  - DatabaseService was failing to build without this package

**Integration Points:**
- Reads from DatabaseService.SearchAsync() with description + tags LIKE search
- MediaTypeOptions seeded from database (or hardcoded default: Photo/Video/Document)
- Empty state shows when items.Count == 0 after filter applied
- Debounce prevents UI lag during search interactions

**Key learnings:**
- `x:Bind` code generation requires instance properties, not static fields
- Uno Platform needs explicit Visibility property from ViewModel (no built-in BoolToVisibilityConverter)
- Debounce via CTS + Task.Delay is cleaner than timer components
- Ensure all NuGet dependencies explicitly listed in csproj (Microsoft.Data.Sqlite hidden dependency bug)

**Next steps:**
- PR #15 review and merge to dev
- Slice #8 (Rescan & Progress) uses this search infrastructure
- AI Tagging service (Slice #6) integrates with search filters



### GTK GSettings Fatal Crash — XAML ContentDialog Picker — 2026-03-27

**Bug fixed:** `GLib-GIO-ERROR: No GSettings schemas are installed on the system` — fatal crash on macOS when clicking "Add Images".

**Root cause:** `FileOpenPicker` with `WinRT.Interop.InitializeWithWindow` triggers GTK's GSettings subsystem on macOS. GSettings schemas are not installed without extra Homebrew packages, causing a fatal GLib-GIO-ERROR that kills the process.

**Fix:** Replaced `FilePickerService` entirely — no GTK native picker at all. New implementation uses a XAML `ContentDialog` with a `TextBox` so users type or paste file paths directly. Zero dependency on OS file picker APIs, GSettings, or WinRT.Interop.

**Key implementation details:**
- `ContentDialog` with `AcceptsReturn = true` TextBox (height 120) for multi-path input
- Paths split on `\n`, `\r`, `;` — trimmed and validated via `File.Exists`
- `XamlRoot` set via `(Application.Current as App)?.MainWindow?.Content?.XamlRoot` (instance, not static)
- `MainViewModel.AddImagesAsync`: `paths.Count == 0` now sets `StatusMessage = "No valid paths entered"` instead of silently returning
- Removed `using Windows.Storage.Pickers` and `WinRT.Interop` references

**GTK Lesson:** `FileOpenPicker` + `InitializeWithWindow` = GSettings crash on macOS. Never use it on Uno GTK/Skia. Use XAML ContentDialog instead.

**Build:** ✅ 0 errors, 0 warnings  
**Commit:** 1938874 on dev

---

### GTK File Picker Wiring — 2026-03-27

**Bug fixed:** "Add Images" button did nothing on macOS GTK Skia.

**Root cause:** `FileOpenPicker` on Uno Skia/GTK silently fails unless initialized with the native window handle via `WinRT.Interop` before calling `PickMultipleFilesAsync`.

**Canonical GTK file picker pattern:**
```csharp
var window = (Application.Current as App)?.MainWindow
    ?? throw new InvalidOperationException("MainWindow not available");

var picker = new FileOpenPicker { ... };
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);  // <-- REQUIRED on GTK
var files = await picker.PickMultipleFilesAsync();
```

**Other changes:**
- `App.MainWindow` changed from `protected` to `public` so services can access it.
- Silent `catch { return; }` in `AddImagesAsync` replaced with `Console.Error` log + `StatusMessage = "Error opening file picker"` so failures surface in UI.

**Build:** ✅ 0 errors, 0 warnings  
**Commit:** b919c8e on dev



**Built:** System-aware dark/light theming using ResourceDictionary.ThemeDictionaries.

**Changes Made:**
1. **App.xaml**: Replaced empty MergedDictionaries TODO with inline `ResourceDictionary.ThemeDictionaries` with `Light` and `Dark` keys. 11 custom named brushes per theme (AppBackground, AppSurfaceBackground, AppCardBackground, AppPreviewBackground, AppTextPrimary, AppTextSecondary, AppTextTertiary, AppBorderColor, AppBorderStrong, AppAccentColor).
2. **App.xaml.cs**: On `OnLaunched`, read `Application.RequestedTheme` and set `rootFrame.RequestedTheme = ElementTheme.Dark/Light` explicitly — required for GTK Skia to resolve ThemeDictionary resources.
3. **MainPage.xaml**: Replaced all hardcoded hex colors with `{ThemeResource AppXxx}` references. Layout unchanged.

**GTK Theming Lessons:**
- `{ThemeResource ApplicationPageBackgroundThemeBrush}` fails silently on GTK Skia — never use WinUI built-in ThemeResources without Uno GTK verification.
- Safe pattern: define ALL resources explicitly inline in App.xaml ThemeDictionaries with custom key names (e.g., AppBackground not ApplicationPageBackgroundThemeBrush).
- `Application.RequestedTheme` reads system preference correctly.
- `RequestedThemeChanged` event does NOT compile (CS0246: RequestedThemeChangedEventArgs not found). Use startup-only detection for now.
- Setting `Frame.RequestedTheme` explicitly is required — just defining ThemeDictionaries is not sufficient on GTK Skia.

**Defined Brushes (Light/Dark pairs):**
- AppBackground: White / #FF1E1E1E
- AppSurfaceBackground: #FFF5F5F5 / #FF2D2D2D
- AppCardBackground: #FFE0E0E0 / #FF3E3E3E
- AppPreviewBackground: White / #FF252525
- AppTextPrimary: #FF1E1E1E / #FFEFEFEF
- AppTextSecondary: #FF666666 / #FFAAAAAA
- AppTextTertiary: #FF999999 / #FF777777
- AppBorderColor: #FFCCCCCC / #FF555555
- AppBorderStrong: #FF888888 / #FF888888
- AppAccentColor: #FF0078D4 / #FF60CDFF

**Build Result:** ✅ 0 errors, 0 warnings

**PR:** #17 targeting dev

**Status:** ✅ Completed (ready for merge after Bertha review)

### Folder-Browse ContentDialog Pattern — 2026-06-XX

**Feature:** Upgraded "Add Images" picker from manual path typing to a two-step folder browse + checkbox select.

**UX Flow:**
1. ContentDialog opens pre-filled with `~/Pictures`
2. User edits folder path and clicks "Browse Folder" → checklist populates
3. User checks desired images → "Add Selected" enables → dialog closes → import runs

**Implementation Pattern (code-only, no XAML page):**
- Build entire dialog content programmatically in `FilePickerService.cs`
- `StackPanel` root with `Orientation=Horizontal` folder row (TextBox + Button)
- `ScrollViewer { MaxHeight=240, HorizontalScrollMode=Disabled }` wrapping a `StackPanel` of `CheckBox` items
- `IsPrimaryButtonEnabled` toggled by tracking a `HashSet<string> checkedPaths` in Checked/Unchecked handlers
- Auto-browse pre-filled path on dialog open (call `RefreshList` before `ShowAsync`)
- `Directory.EnumerateFiles` + extension filter via `HashSet<string>` (OrdinalIgnoreCase)

**Key Rules:**
- `Colors.Gray` (not `Microsoft.UI.Colors.Gray`) — use the `using Microsoft.UI;` import
- `dialog.XamlRoot = (Application.Current as App)?.MainWindow?.Content?.XamlRoot` — required for ContentDialog on GTK
- `IsPrimaryButtonEnabled = false` initially — only enable when checkedPaths.Count > 0
- `HorizontalScrollMode = ScrollMode.Disabled` on ScrollViewer to prevent horizontal overflow

**Build Result:** ✅ 0 errors, 0 warnings
