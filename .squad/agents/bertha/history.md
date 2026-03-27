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
