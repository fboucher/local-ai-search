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

### 2026-03-26 — Project Refactoring: src/ folder & .NET 10 upgrade

**Branch:** `squad/2-project-bootstrap` (additional commits)
**What changed:**
- **Frank's directive:** Source code now lives under `src/` folder at repo root
- **Layout:** `src/LocalAiSearch/` contains the Uno Platform project, `LocalAiSearch.sln` remains at root
- **Framework bump:** All target frameworks upgraded from `net8.0-*` to `net10.0-*`
  - net10.0-windows10.0.19041
  - net10.0-ios
  - net10.0-android
  - net10.0-maccatalyst
- **Rationale:** .NET 10 is current LTS (released Nov 2025), better choice than .NET 8 for new projects
- **PRD note:** PRD was written assuming .NET 8, but team upgraded to .NET 10

**Git operations:**
- Used `git mv` to preserve history when relocating files
- Updated solution file to reference `src\LocalAiSearch\LocalAiSearch.csproj`

## 2026-03-26: src/ directive clarification
**Source:** Frank (user directive)
**Context:** Frank wants the solution file inside src/ too - all code and solution artifacts must be under src/. The repo root should only contain config files (like README, .gitignore, etc.). This is the complete layout policy.

**Layout implemented:**
```
src/
  LocalAiSearch.sln     ← solution file moved here
  LocalAiSearch/
    LocalAiSearch.csproj
```

- Mac cross-platform fix: `<EnableWindowsTargeting>true</EnableWindowsTargeting>` required in .csproj when targeting net*-windows from macOS

## 2026-03-26: Desktop-only platform scope
**Source:** Frank (user directive)
**What:** App targets Linux, Mac, Windows desktop only. Removed iOS and Android from TargetFrameworks.
**Why:** Frank explicitly does not need mobile support.

**Target frameworks:**
- net10.0-windows10.0.19041 (Windows desktop)
- net10.0-maccatalyst (macOS desktop)
- net10.0-linux (Linux desktop)

**Removed:**
- net10.0-ios
- net10.0-android

**Branch:** squad/2-project-bootstrap
**Commit:** refactor: desktop-only targets — remove iOS/Android, add Linux
