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

### 2026-03-26 — Slice #1 HITL Planning
- ✅ Completed: Reviewed issue #2 requirements
- ✅ Completed: Designed HITL collaboration plan with 4 phases and handoff points
- ✅ Completed: Documented in `.squad/decisions.md` (merged from inbox)
- Key insight: Platform verification (Uno on macOS) is the only genuine blocker for Frank
- Collaboration flow: Rusty scaffolds → Bertha reviews → Frank verifies → Linus accepts
- Next: Review PR #10 for architecture alignment before Frank's HITL phase

### 2026-03-26 — Slices #2/#3/#4 PR Review & Merge
- ✅ Completed: Reviewed and merged PR #12 (Database Foundation, Issue #3)
  - MediaItem model + DatabaseService with full CRUD operations
  - 9 comprehensive unit tests, all passing
  - TFM: net10.0 only (correct), async/await used properly, proper folder structure
  - Had to retarget from main to dev, then merged via squash
- ✅ Completed: Reviewed and merged PR #13 (Folder Scanner, Issue #5)
  - FolderScannerService with SHA256 deduplication, recursive search, RAW format exclusion
  - 12 unit tests covering all scenarios including duplicate detection
  - Conflicts with build artifacts (bin/obj) resolved by keeping implementation over TODO stubs
  - Merged manually after resolving conflicts
- ✅ Completed: Reviewed and merged PR #11 (Image Viewer UI, Issue #4)
  - MainViewModel + MediaItemViewModel with INotifyPropertyChanged
  - Two-panel XAML UI with mock data for 20 items
  - No ThemeResource issues, clean MVVM pattern
  - Merged manually after resolving conflicts with DB/Scanner code
- ✅ Completed: Closed issues #3, #4, #5 (didn't auto-close from manual merges)
- Dev branch now has: Bootstrap + DB + Scanner + Image Viewer (4 slices complete)
- Key learning: Build artifacts (bin/obj) should be gitignored to prevent merge conflicts
- All PRs had to be retargeted from main/bootstrap-branch to dev before merging

### 2026-03-27 — Slices #7/#8 PR Review & Merge
- ✅ Completed: Reviewed and merged PR #16 (Rescan & Progress, Issue #8)
  - ScanProgressService: clean IProgress<ScanProgress> + CancellationToken pattern
  - ScanPhase enum (Scanning/Tagging/Complete) + ScanProgress record
  - MainViewModel: RescanCommand, CancelScanCommand, IsScanning, ScanProgressText, ScanProgressValue
  - Layer separation respected: service has no XAML, ViewModel has no filesystem calls
  - 39/39 tests passing (33 prior + 6 new from Linus), 0 errors, 0 warnings
- ✅ Completed: Reviewed and merged PR #17 (Dark/light theming, Issue #9)
  - App.xaml: ThemeDictionaries with explicit Light/Dark ResourceDictionaries, 10 AppXxx brush keys each
  - App.xaml.cs: Frame.RequestedTheme set on OnLaunched from Application.RequestedTheme — required for GTK Skia
  - MainPage.xaml: All hardcoded hex colors replaced with {ThemeResource AppXxx} — no WinUI built-in brushes
  - ApplicationPageBackgroundThemeBrush absent — GTK layout bug avoided
  - 33/33 tests passing, 0 errors, 0 warnings
- Dev branch now has all 8 slices: Bootstrap + DB + Scanner + Image Viewer + AI Tagging + Search + Rescan + Theming
- Final dev state: 39/39 tests, 0 errors, 0 warnings

### 2026-06-13 — Add Images PR #18 Review & Merge
- ✅ Completed: Reviewed and merged PR #18 (Add Images — manual file picker with dedup and status)
  - ImageImportService: SHA256 dedup via `GetByHashAsync`, `ImportResult(Added, Skipped, Unsupported)` record, CancellationToken support
  - IFilePickerService: clean interface abstraction — ViewModel holds interface, platform `FilePickerService` instantiated in code-behind only
  - AddImagesCommand: properly awaits `PickImagesAsync()` → `ImportAsync()` → `LoadImagesAsync()` in sequence
  - StatusMessage: auto-clears after 4s via `ClearStatusAfterDelayAsync`, `StatusMessageVisibility` computed without XAML converter
  - 7 new tests by Linus (ImageImportServiceTests): all dedup, extension filtering, DB persistence cases covered
  - Build: 0 errors, 0 warnings; Tests: 46/46 passing on branch and post-merge on dev
  - No dedicated GitHub issue — feature designed in-session, no issue to close
- Dev branch final state: 46/46 tests, 0 errors, 0 warnings

### 2026-06-14 — Image AI Analysis Feature Design
- ✅ Completed: Designed image analysis feature based on Frank's question about how to process images
- Key findings from codebase exploration:
  - Existing `AiTaggingService` is tightly coupled to Reka API with hardcoded OpenAI-compatible format
  - DB schema already has `description`, `tags`, `media_type`, `is_tagged` columns — no changes needed
  - `ParseAiResponse()` method is reusable for any backend that returns structured text
  - Current app imports images by file path with SHA256 hash dedup, but no AI processing occurs
- Design decisions:
  - Recommended Ollama + `llava:7b` over OpenAI for privacy/cost (matches Frank's local-first preference)
  - Proposed `IImageAnalysisService` interface for backend abstraction (strategy pattern)
  - Recommended per-image "Analyze" button UX (Frank wants cherry-pick control)
  - No DB schema changes required for v1 — existing columns sufficient
- Artifacts: `.squad/decisions/inbox/bertha-image-analysis-design.md`
- Awaiting Frank's approval on: Ollama assumption, model choice (llava vs moondream), batch capability scope
