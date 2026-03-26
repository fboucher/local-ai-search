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

