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
- net10.0 (Linux/Skia/GTK desktop)

**Removed:**
- net10.0-ios
- net10.0-android

**Branch:** squad/2-project-bootstrap
**Commit:** refactor: desktop-only targets — remove iOS/Android, add Linux

## 2026-03-26: Linux TFM correction — net10.0-linux → net10.0
**Source:** Frank (NETSDK1139 error fix)
**What:** `net10.0-linux` is not a valid .NET TFM. Uno Platform uses `net10.0` (generic) for Linux/Skia/GTK desktop.
**Why:** NETSDK1139 error — `linux` is not a recognized .NET platform identifier. For Uno Skia Linux support, the TFM is simply `net10.0`.
**Branch:** squad/2-project-bootstrap
**Commit:** fix: correct Linux TFM — net10.0-linux → net10.0 (Uno Skia)

## 2026-03-26: maccatalyst dropped — Skia chosen for Mac + Linux desktop
**Source:** Frank (no Xcode, no maccatalyst)
**What:** `net10.0-maccatalyst` removed. `net10.0` (Skia) now covers both macOS and Linux desktop.
**Why:** Frank doesn't have Xcode. `net10.0-maccatalyst` requires the Xcode toolchain. Uno's Skia renderer (`Uno.WinUI.Skia.Desktop`) gives cross-platform desktop on Mac and Linux without it.

**Final TFMs:**
- `net10.0-windows10.0.19041` — Windows (WinUI)
- `net10.0` — Mac + Linux (Uno Skia Desktop)

**Changes made:**
- Removed `net10.0-maccatalyst` from `TargetFrameworks`
- Added `Uno.WinUI.Skia.Desktop 5.*` conditioned on `'$(TargetFramework)' == 'net10.0'`
- Added `Program.cs` with `#if !WINDOWS` guard — Skia desktop host entry point (fixes CS5001)
- XAML codegen (`InitializeComponent`) flows from `Uno.WinUI` unconditionally + Skia package for the `net10.0` TFM (fixes CS0103)

**Key learnings:**
- Mac + Linux Skia desktop = `net10.0` TFM (generic), no Xcode needed
- `net10.0` Skia needs `Uno.WinUI.Skia.Desktop` package + a `Program.cs` `Main` entry point
- WinUI Windows target auto-generates its entry point; Skia target does not — must provide `Program.cs`

## 2026-03-26: Fix — Correct Uno Skia NuGet package for net10.0

**Branch:** `squad/2-project-bootstrap`
**Commit:** `fix: correct Uno Skia package reference for net10.0 desktop`

**Problem:** `Uno.WinUI.Skia.Desktop` does not exist on NuGet — restore was failing.

**What actually exists on NuGet (as of 5.6.99/5.7.0-dev):**
| Package | Notes |
|---|---|
| `Uno.WinUI.Skia.Gtk` | GTK renderer, works on Linux + macOS (GTK3 required) |
| `Uno.WinUI.Skia.Wpf` | WPF renderer, Windows only |
| `Uno.WinUI.Skia.Linux.FrameBuffer` | Framebuffer renderer, Linux embedded |
| `Uno.UI.Skia.MacOS` | Native macOS renderer |
| `Uno.UI.Skia.X11` | X11 renderer |

**There is no `Uno.WinUI.Skia.Desktop`.** The previous assumption was wrong.

**Correct entry point API (Uno.WinUI.Runtime.Skia.Gtk, 5.6.99):**
```csharp
using Uno.UI.Runtime.Skia.Gtk;
new GtkHost(() => new App()).Run();
```
- NOT `SkiaHostBuilder` — that class does not exist in Uno 5.6.99
- NOT `UseDesktop()` — comes from the non-existent package
- `GtkHost` is in `Uno.UI.Runtime.Skia.Gtk` namespace (provided by `Uno.WinUI.Skia.Gtk`)

**XAML source generator pattern (IsUnoHead):**
```xml
<!-- Triggers Uno XAML codegen (InitializeComponent) for non-Windows TFMs -->
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
  <IsUnoHead>true</IsUnoHead>
</PropertyGroup>
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
  <Page Include="**/*.xaml" Exclude="obj/**/*.xaml" />
</ItemGroup>
```
- `IsUnoHead=true` tells Uno MSBuild tasks to run source generators
- `<Page>` items feed XAML files to the generator as AdditionalFiles
- Must be conditioned on non-Windows to avoid type conflicts with Windows SDK

**Build result after fix:**
- `dotnet restore` ✅ passes
- `net10.0` build ✅ succeeds (1 informational warning: App.xaml should call InitializeComponent)
- `net10.0-windows10.0.19041` ❌ 2 errors — pre-existing; Uno 5.6.99 has no `net10.0-windows` support, needs Windows-side fix (WinAppSDK or net9.0 downgrade)


## 2026-03-26: Single net10.0 TFM — Skia/GTK on all platforms

**Branch:** `squad/2-project-bootstrap`
**Commit:** `fix: single net10.0 TFM — Skia renderer on all platforms`

**Frank's directive:** One TFM only — `net10.0`. Skia/GTK renderer on Windows, Mac, and Linux.

**What changed:**
- `<TargetFrameworks>` → `<TargetFramework>net10.0</TargetFramework>` (singular, no semicolons)
- Removed `<EnableWindowsTargeting>true</EnableWindowsTargeting>`
- Removed all `Condition` blocks based on `$(TargetFramework.Contains('-windows'))` or platform identifier
- `IsUnoHead`, `<Page>` items, and `Uno.WinUI.Skia.Gtk` are now unconditional
- `Program.cs` — removed `#if !WINDOWS` / `#else` / `#endif` guards; just plain `GtkHost(() => new App()).Run()`

**Build result:**
- `dotnet restore` ✅ clean
- `dotnet build` ✅ succeeded with 1 warning (pre-existing Uno0006: App.xaml.cs should call InitializeComponent)

**Key learnings:**
- Final TFM decision: `net10.0` only (single TFM for all platforms via Skia/GTK)
- Uno 5.6.99 does not support net10.0-windows WinUI; Skia/GTK works on Windows via net10.0

### 2026-03-26 — Slice #2: Database Foundation

**Branch:** `squad/3-database-foundation`
**PR:** https://github.com/fboucher/local-ai-search/pull/12
**Status:** ✅ Completed (Ready for review)

**What I built:**
- `MediaItem.cs` model in `src/LocalAiSearch/Models/`:
  - All properties: Id, FilePath, FileHash, Description, Tags, MediaType, FileSizeBytes, CreatedAt, UpdatedAt, IsTagged
  - Tags stored as comma-separated string (no JSON complexity)
- `DatabaseService.cs` in `src/LocalAiSearch/Services/`:
  - Constructor accepts dbPath (default: `./local.db`)
  - Internal constructor accepts SqliteConnection for test isolation
  - `InitializeAsync()` creates `media_items` table with indexes on file_hash and tags
  - Full CRUD: `GetAllAsync()`, `GetByIdAsync()`, `GetByHashAsync()`, `InsertAsync()`, `UpdateAsync()`, `SearchAsync()`
  - LIKE search on description and tags fields
- `LocalAiSearch.Tests` project in `src/LocalAiSearch.Tests/`:
  - xUnit test project targeting `net10.0` only
  - `DatabaseServiceTests.cs` with 9 passing tests
  - In-memory SQLite for test isolation (each test gets its own connection)
  - Tests cover: init, insert, getById, getByHash, update, search by description, search by tags, getAll

**Technology choices:**
- **Microsoft.Data.Sqlite** (not Turso/libsql) — local-only SQLite, no cloud needed
- **Plain ADO.NET** (no ORM) — keeps it simple for this app size
- **Tags as comma-separated string** — simpler than JSON, works fine with LIKE search
- **Internal constructor** for test isolation — allows passing in-memory connection while keeping public API clean

**Key learnings:**
- SQLite in-memory databases (`:memory:`) require the connection to stay open for the entire test lifecycle — otherwise the db disappears
- Used `InternalsVisibleTo` in csproj to expose internal constructor to test project
- `RETURNING id` clause in INSERT works on SQLite 3.35+, which is what .NET 10 ships with
- DateTime stored as ISO 8601 strings (`ToString("O")`) for simplicity

**File paths:**
- `src/LocalAiSearch/Models/MediaItem.cs`
- `src/LocalAiSearch/Services/DatabaseService.cs`
- `src/LocalAiSearch.Tests/DatabaseServiceTests.cs`
- `src/LocalAiSearch/LocalAiSearch.csproj` (added Microsoft.Data.Sqlite + InternalsVisibleTo)
- `src/LocalAiSearch.Tests/LocalAiSearch.Tests.csproj` (xUnit test project)

**Next steps:**
- Slice #4: Image Viewer (Livingston's domain)
- Slice #5: Folder Scanner (uses DatabaseService to check for duplicates via GetByHashAsync)
- Slice #6: AI Tagging (updates MediaItem.Description and Tags via UpdateAsync)

### 2026-03-27 — Slices #7/#8 — Rescan & Progress Service (Round 2)

**Branch:** `squad/8-rescan-progress`  
**PR:** #16 (targeting dev)  
**Status:** ✅ Completed, ready for merge  

**What I built:**

1. **ScanProgressService.cs** — Manages folder scan with progress reporting and cancellation
   - `IProgress<ScanProgress>` pattern (thread-safe, standard .NET)
   - `RunAsync(string folderPath, IProgress<ScanProgress> progress, CancellationToken cancellation)` orchestrates the scan
   - Per-item tagging loop: calls `AiTaggingService.TagImageAsync()` per item vs. batch `TagAllUnprocessedAsync()`
   - Folder resolution: `SCAN_FOLDER` env var with fallback to `Environment.SpecialFolder.MyPictures`
   - Progress reporting: "Processing image X of Y" granular messages
   - Handles cancellation gracefully (CancellationToken propagated throughout)

2. **MainViewModel.cs enhancements**
   - Secondary constructor: `MainViewModel(DatabaseService, ScanProgressService)` for DI/testing
   - Existing parameterless constructor still works (creates services internally, no breaking changes)
   - `RescanCommand` — triggers folder scan with progress UI updates
   - `CancelScanCommand` — cancels in-progress scan
   - `IsScanning` property (bool) — tracks whether scan is active
   - Progress properties for UI binding (scanned count, total, current file, etc.)

**Architecture decisions:**

1. **IProgress<ScanProgress> pattern** — Standard .NET, auto-marshals to UI thread, testable with mock
2. **Per-item tagging** — Enables granular progress vs. batch operation
3. **Env var folder path** — Developer-friendly, no native folder picker dialog yet
4. **Dual constructors** — Supports both production and test scenarios without breaking changes

**Testing:**
- Linus added 6 comprehensive tests to this branch (all passing)
- Covers progress reporting, cancellation, folder resolution, per-item loop

**Next steps:**
- Merge PR #16 to dev
- Livingston's Slice #9 (Theming) ready for merge in parallel


### 2026-03-27 — Slice #6: AI Tagging Service (Stub + Real HTTP)

**Branch:** `squad/6-ai-tagging-service`  
**PR:** #14 (targeting dev)  
**Status:** ✅ Complete (Ready for review)  

**What I built:**
- `AiTaggingService.cs` in `src/LocalAiSearch/Services/`:
  - Constructor accepts optional `endpointUrl` parameter + DatabaseService
  - Auto-detects stub mode: if `AI_ENDPOINT` env var unset → stub; any value → real HTTP
  - Stub response: hardcoded description/tags/mediatype for testing
  - Real HTTP path (fully written, not yet activated):
    - POST to `{endpoint}/v1/chat/completions`
    - Vision-style message with Base64 data URL image
    - Prompt structure: "Provide DESCRIPTION: / TAGS: / TYPE:" format
    - Response parsing with fallback for varied formats (case-insensitive)
  - `TagMediaItemAsync()` method: loads image, calls API, parses response, updates database

**HTTP Design:**
- Model: `"local-model"` (constant, easily changed for different endpoints)
- Content types: `image/base64` vision block + `text` prompt
- Response parsing: extracts DESCRIPTION, TAGS, TYPE with fallback handling

**Configuration for real AI (zero code changes):**
```bash
export AI_ENDPOINT="http://192.168.1.50:1234"
dotnet run
```

**Tests (9 written):**
- Basic stub response
- Real HTTP POST structure (via mock handler)
- Response parsing with various formats
- Error handling and fallbacks
- Batch processing (added by Linus)

**Key learnings:**
- OpenAI-compatible format is standard for local LLM endpoints
- Base64 data URLs work well for vision-style messages
- Fallback parsing essential for robust response handling
- Stub mode pattern: check for config → auto-switch to real mode

**Next steps:**
- Linus adds additional tests (batch, Base64, case-insensitive)
- PR #14 review and merge to dev
- Live endpoint integration when AI service becomes available


### 2026-06-10 — Bug Fix: `no such table: media_items`

**Branch:** `dev`
**Commit:** `23f71c6`
**Status:** ✅ Completed

**Bug:** `SQLite Error 1: 'no such table: media_items'` when user clicks "Add Images" without first running Rescan.

**Root cause:** `DatabaseService.InitializeAsync()` was only called via the Rescan flow (`ScanProgressService.RunAsync()`). If the user added images before ever running a Rescan, the `media_items` table didn't exist yet.

**Fix:**
- Added `await _db.InitializeAsync()` as the first line of `ImageImportService.ImportAsync()` — guarantees schema exists before any import DB operations.
- Added `await _db.InitializeAsync()` inside `MainViewModel.LoadImagesAsync()` before any query — guarantees schema exists on first grid load.
- `InitializeAsync` uses `CREATE TABLE IF NOT EXISTS`, so calling it multiple times is safe (idempotent).

**Files changed:**
- `src/LocalAiSearch/Services/ImageImportService.cs`
- `src/LocalAiSearch/ViewModels/MainViewModel.cs`

**Tests:** 46/46 passed. Zero build warnings/errors.
