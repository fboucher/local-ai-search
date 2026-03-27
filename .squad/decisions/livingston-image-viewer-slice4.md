# Image Viewer MVVM Structure and Placeholder Strategy

**Date:** 2025-03-26  
**Author:** Livingston  
**Status:** Implemented (Slice #4)

## Decision: Use Placeholder Emoji Thumbnails + Full MVVM Separation

**Context:**
- Issue #4 requires basic image viewer with thumbnail grid, preview panel, metadata display
- No real image files available yet (folder scanning comes in Slice #5)
- Need functional UI for HITL verification without image loading complexity

**Approach:**

### 1. MVVM Structure
- **MediaItemViewModel**: Display-focused wrapper with ImageSource (nullable), FileName/FormattedDate computed properties
- **MainViewModel**: Full INotifyPropertyChanged implementation, ObservableCollection<MediaItemViewModel>, commands (SortToggle, LoadMore)
- **RelayCommand**: Simple ICommand helper class in same file (avoids external MVVM toolkit dependency for now)
- **MainPage.xaml.cs**: Thin code-behind, only event handlers (SelectionChanged, ScrollViewer.ViewChanged)

**Rationale:**
- Separates UI state (ViewModel) from UI structure (XAML) cleanly
- MediaItemViewModel wraps display concerns, keeping MainViewModel focused on collection/command logic
- Nullable ImageSource allows graceful placeholder → real image transition in future slices
- RelayCommand avoids NuGet package for a 15-line helper

### 2. Placeholder Thumbnail Strategy
- **Emoji (📷) in Border**: Visual placeholder for thumbnails (150x150 fixed)
- **Colored background**: #FFE0E0E0 gray with #FF888888 border
- **No pre-generated images**: Avoids file I/O complexity in UI slice
- **ImageSource stays null**: Future slice will populate with real BitmapImage

**Alternatives Considered:**
- **Colored rectangles with ID numbers**: Less visually engaging, harder to tell apart
- **SkiaSharp generated placeholders**: Overkill for mock data phase
- **Loading real images from Resources/**: No images exist in repo yet, would be fake work

**Rationale:**
- Emoji approach is instantly recognizable as "image placeholder"
- Zero external dependencies (no image files, no drawing code)
- Easy to replace: just set ImageSource property when real images load
- Matches PRD's "placeholder colored rectangles" intent while being more user-friendly

### 3. ThemeResource Avoidance Pattern
- **Explicit colors only**: `Background="White"`, `#FFE0E0E0`, etc.
- **No `{ThemeResource ...}`**: Carries forward GTK lesson from Slice #1
- **Placeholder dark/light mode**: Noted in acceptance criteria, but not implemented (out of scope for Slice #4, covered by Slice #9)

**Rationale:**
- ThemeResource failed silently on GTK in Slice #1 (documented in Livingston history)
- Explicit colors guarantee consistent cross-platform rendering
- Theme system should be slice-specific work (Issue #9), not mixed into viewer basics

## Impact
- ✅ Clean MVVM separation enables easy data source swap (mock → DB)
- ✅ Placeholder strategy requires zero image assets
- ✅ Build succeeds with 0 warnings on GTK target
- ✅ UI functional for HITL verification without backend dependencies
- ⚠️ Real image loading deferred to later slice (acceptable per PRD phasing)
