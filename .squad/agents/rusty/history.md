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

### 2026-03-26 — Slice #1: Project Bootstrap Scaffold

**Branch:** `squad/2-project-bootstrap`
**PR:** https://github.com/fboucher/local-ai-search/pull/10
**Status:** ✅ Completed (Awaiting HITL verification from Frank)

**What I built:**
- Created manual Uno Platform project structure (templates not installed on Frank's machine)
- Solution file: `LocalAiSearch.sln`
- Main project: `LocalAiSearch/LocalAiSearch.csproj` (targeting net8.0-windows10, net8.0-ios, net8.0-android, net8.0-maccatalyst)
- Folder structure: `Models/`, `Services/`, `ViewModels/`, `Views/`
- Placeholder classes with TODO comments matching PRD spec:
  - `Models/MediaItem.cs`
  - `Services/FolderScannerService.cs`
  - `Services/AiTaggingService.cs`
  - `Services/DatabaseService.cs`
  - `Services/ImageDisplayService.cs`
  - `ViewModels/MainViewModel.cs`
- Application entry point: `App.xaml` and `App.xaml.cs`
- Main UI: `MainPage.xaml` and `MainPage.xaml.cs` (displays "Local AI Search" centered text)

**Decisions made:**
- Multi-target framework approach for cross-platform compatibility
- Used Uno.WinUI 5.* and Uno.Extensions.Hosting packages
- All classes have namespaces matching folder structure
- TODO comments reference PRD requirements for each component

**Next steps (HITL):**
- Frank needs to run `dotnet restore` and `dotnet build`
- Verify app launches on macOS (maccatalyst target)
- Once verified, merge to main/dev and proceed to Slice #2 (Database)
