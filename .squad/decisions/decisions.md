# Squad Decisions — local-ai-search

## Merged 2026-03-27

All decisions from `.squad/decisions/inbox/` consolidated here. Deduplicated where multiple decisions covered same topic.

---

## Image AI Analysis Feature Design — 2026-03-27

**Author:** Bertha  
**Status:** Proposed (awaiting Frank approval)  
**Document:** `.squad/decisions/inbox/bertha-image-analysis-design.md`

### Executive Summary
Frank wants users to analyze images on-demand for descriptions and tags. Existing DB schema already has the required columns. Design proposes `IImageAnalysisService` interface to abstract backend choice.

### Backend Decision
**User directive (Frank):** Use local OpenAI-compatible endpoint at `192.168.2.11:8000/v1`. Do NOT use Ollama or cloud OpenAI. Use OpenAI .NET SDK with custom BaseUrl.

### Database
No changes required. Existing columns (`description`, `tags`, `media_type`, `is_tagged`) are sufficient for v1.

### Service Architecture
- `IImageAnalysisService` interface with strategy pattern
- Implementations: `OpenAiImageAnalysisService` (custom BaseUrl), `StubImageAnalysisService` (testing)
- Service only analyzes; caller decides persistence
- `IsAvailableAsync()` for graceful UI feedback

### UX Recommendation
Per-image "Analyze" button in image details panel. User controls which images get processed. Button shows loading state, result populates inline.

### Implementation Order
1. Add OpenAI SDK NuGet package (custom BaseUrl config)
2. Create `IImageAnalysisService` interface
3. Implement `OpenAiImageAnalysisService`
4. Add "Analyze" button to image details panel
5. Wire button to ViewModel command
6. (Optional v2) Implement batch "Analyze All" capability

### Open Questions for Frank
1. Confirm endpoint details: `192.168.2.11:8000/v1` is correct?
2. Is per-image button the only UX needed for v1?
3. Should UI layout accommodate future batch capability?

---

## Copilot Directive — AI Backend — 2026-03-27

**By:** Frank Boucher (via Copilot)  
**Status:** Active directive

The AI backend for image analysis is a local OpenAI-compatible service at `192.168.2.11:8000/v1`. **Do NOT use OpenAI cloud or Ollama.** Use the standard OpenAI .NET SDK pointed at this base URL. Images are stored on disk only — the DB stores the file path, not the image bytes.

---

## File Picker — GTK GSettings Fatal Crash Fix — 2026-03-27

**Author:** Livingston  
**Status:** ✅ Implemented (commit 1938874 on dev)  
**Issue:** `GLib-GIO-ERROR: No GSettings schemas are installed on the system`

### Root Cause
`FileOpenPicker` with `WinRT.Interop.InitializeWithWindow` triggers GTK's GSettings subsystem on macOS, causing fatal process crash (not catchable).

### Solution
Replaced native file picker with XAML `ContentDialog` containing a `TextBox` for path input. Users type or paste paths directly (one per line, semicolons as separator). Paths validated via `File.Exists` before returning.

### Implementation Details
- `ContentDialog` with `AcceptsReturn = true` TextBox (height 120)
- Multi-path input split on `\n`, `\r`, `;` and trimmed
- `XamlRoot` set via instance path: `(Application.Current as App)?.MainWindow?.Content?.XamlRoot`
- Status message on invalid paths: `"No valid paths entered"`
- Removed all `Windows.Storage.Pickers` and `WinRT.Interop` references

### GTK Lesson
**Never use `FileOpenPicker` + `InitializeWithWindow` on Uno GTK/Skia.** GSettings crash on macOS is inevitable. Use XAML ContentDialog or other cross-platform alternatives.

---

## GTK File Picker Window Handle Wiring — 2026-03-27

**Author:** Livingston  
**Status:** ✅ Implemented (commit b919c8e on dev)  
**Issue:** "Add Images" button silent failure on macOS GTK

### Pattern (No Longer Used — Replaced by ContentDialog)
This decision documents the canonical GTK file picker initialization pattern for reference (now superseded by ContentDialog approach):

```csharp
var window = (Application.Current as App)?.MainWindow
    ?? throw new InvalidOperationException("MainWindow not available");

var picker = new FileOpenPicker { ... };
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);  // <-- REQUIRED on GTK
var files = await picker.PickMultipleFilesAsync();
```

### Current Status
**This pattern is now obsolete.** GTK ContentDialog approach (above) is preferred for new code. Kept here for architectural reference.

---

## Database Init Defensive Pattern — 2026-03-27

**Author:** Rusty  
**Status:** ✅ Implemented  
**Issue:** `SQLite Error 1: 'no such table: media_items'` on "Add Images" before first Rescan

### Solution
Call `DatabaseService.InitializeAsync()` (CREATE TABLE IF NOT EXISTS) before any DB operation that assumes schema exists.

### Entry Points
- `ImageImportService.ImportAsync()` — first line
- `MainViewModel.LoadImagesAsync()` — before any query

### Rationale
Each service that touches the DB is self-sufficient. `InitializeAsync` is idempotent, safe to call multiple times. No performance concern (CREATE TABLE IF NOT EXISTS is near-instant when schema already exists).

---

## Add Images UX — Manual File Picker — 2026-03-27

**Author:** Livingston  
**Status:** ✅ Implemented (PR #18)  
**Issue:** #9 (Manual image import feature)

### UX Spec (Frank approved)
- Native OS file picker, multi-select (jpg/jpeg/png/webp/gif/bmp)
- "Add Images" button in top toolbar, next to Rescan
- Files added to DB via ImageImportService, grid refreshes
- **AI tagging NOT triggered on import** — deferred by design
- Status message: "X image(s) added" (auto-clears after ~4s)
- Duplicates skipped silently (SHA-256 hash-based dedup)

### Design Choices
1. **`IFilePickerService` interface** — MVVM abstraction, platform impl in code-behind
2. **`FileOpenPicker`** — Correct Uno cross-platform API (not StorageProvider)
3. **Inline status TextBlock** — No toast/dialog overhead, simple and clean
4. **Deferred AI tagging** — Import is fast (atomic DB write); tagging is slow (seconds per image)
5. **Hash-based dedup** — SHA-256 of file bytes; silent skip on match

### Note
`FileOpenPicker` + GTK window init (previous decision) was later replaced with ContentDialog approach to fix GSettings crash on macOS.

---

## Image Thumbnail Strategy — 2026-03-27

**Author:** Livingston  
**Status:** ✅ Implemented (commit f2ba0ae on dev)  
**Issue:** Real image previews in grid instead of emoji placeholders

### Implementation
- `MediaItemViewModel.ImageSource` — lazy computed property
- `BitmapImage` with `DecodePixelWidth = 160` (critical for memory efficiency)
- Bind via `{x:Bind ImageSource}` with `x:DataType` in DataTemplate
- `Image` element with `Stretch="UniformToFill"` in fixed 160×160 Grid

### Design Rationale
- Lazy loading: null until first access (creates BitmapImage from FilePath)
- DecodePixelWidth: tells WIC to decode at thumbnail size only, not full resolution
- File URI: `BitmapImage.UriSource = new Uri(FilePath)` works on macOS/GTK with local paths
- DataTemplate: `x:DataType="vm:MediaItemViewModel"` enables `{x:Bind ImageSource}` compilation

### Alternatives Rejected
- **Async loading with INotifyPropertyChanged:** Adds complexity; deferred to future polish
- **Explicit null fallback icon:** Image with null Source shows transparent; Grid background acts as visual fallback

---

## ImageImportService — Manual Image Import — 2026-03-27

**Author:** Rusty  
**Status:** ✅ Implemented (PR #18)  

### ImportResult Record
```csharp
public record ImportResult(int Added, int Skipped, int Unsupported);
```
- **Added** — files successfully persisted to database
- **Skipped** — duplicate (same SHA-256) or exception during processing
- **Unsupported** — extension not in supported list

### Supported Extensions (Case-Insensitive)
- `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.bmp`
- RAW formats implicitly unsupported

### Deferred AI Tagging
**Decision:** AI tagging NOT triggered during import (confirmed by Frank).
- Import is atomic DB write (fast)
- Tagging is AI call per image (slow, seconds each)
- User should control tagging separately
- All imported items land with `IsTagged=false`

### Error Handling
Per-file errors caught, logged to stderr, counted as **Skipped**. `ImportAsync` never throws (except `CancellationToken.ThrowIfCancellationRequested`).

---

## Bertha PR #18 Review — Add Images Feature — 2026-06-13

**Reviewer:** Bertha  
**Status:** ✅ Merged to dev (squash)  
**Authors:** Rusty (service + VM) + Linus (7 tests) + Livingston (XAML/UX)

### Findings
- ✅ SHA-256 dedup via `GetByHashAsync` — clean
- ✅ `IFilePickerService` abstraction — proper layer separation
- ✅ `AddImagesCommand` await chain — correct sequence
- ✅ Status auto-clear 4s — elegant implementation
- ✅ Build: 0 errors, 0 warnings
- ✅ Tests: 46/46 passing

### Issues Found
None. Code is clean, layer separation correct, all acceptance criteria met.

---

## Architecture Notes

### Pattern: Defensive `InitializeAsync` Calls
Each service entry point that touches the DB calls `InitializeAsync()` first. This pattern avoids hidden ordering dependencies and makes services self-sufficient.

### Pattern: `IImageAnalysisService` Strategy
New image analysis feature uses strategy pattern with interface abstraction. Enables backend swapping (Ollama, OpenAI, local endpoint) without code changes. Service only analyzes; caller handles persistence.

### Rule: Uno GTK on macOS
- **ContentDialog best practice:** XAML ContentDialog for interactive pickers (native FileOpenPicker + GTK GSettings = fatal crash)
- **`x:Bind` code generation:** Requires instance properties, not static fields
- **ThemeResource safety:** Define ALL resources explicitly in App.xaml ThemeDictionaries; WinUI built-in ThemeResources fail silently on GTK
- **Window handles:** `MainWindow` must be `public` for service access; `XamlRoot` from `Page.Loaded` only, not `App.MainWindow`

