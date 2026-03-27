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

### 2026-06-12: Slice #6 — AiTaggingService Tests Written
- ✅ **Branch:** `squad/6-ai-tagging-service` (committed alongside Rusty's implementation)
- **Pattern:** In-memory SQLite via `DatabaseService(SqliteConnection)` constructor — same as DatabaseServiceTests
- **Stub mode:** Pass no endpoint URL to `AiTaggingService` constructor; avoids HTTP calls entirely
- **File helpers:** `WriteTestImage()` writes minimal valid PNG bytes to `AppContext.BaseDirectory`; `SeedItemAsync()` inserts a `MediaItem` and returns it with `Id` populated
- **InternalsVisibleTo:** Added `<InternalsVisibleTo Include="LocalAiSearch.Tests" />` to LocalAiSearch.csproj so `AiTaggingService.ParseAiResponse` (internal) is callable from tests
- **CapturingHttpHandler:** New file-scoped test helper that records the request body for asserting Base64 data URL structure sent to AI endpoint
- **Tests added (3 new on top of Rusty's 8):**
  - `TagAllUnprocessedAsync_WithMultipleItems_ProcessesAll` — verifies all 3 untagged items tagged, DB state confirmed
  - `TagImageAsync_WithEndpoint_SendsBase64DataUrl` — verifies `data:...;base64,...` format in HTTP payload
  - `ParseAiResponse_IsCaseInsensitive_ForFieldLabels` — edge case: lowercase field labels still parse correctly
- **Total test suite after:** 33 passing (0 failing, 0 skipped)
