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
