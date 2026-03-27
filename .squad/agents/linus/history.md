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

### 2026-06-13: Slice #8 — ScanProgressService Tests Written
- ✅ **Branch:** `squad/8-rescan-progress` (committed as 9726c3d)
- **Pattern:** Same in-memory SQLite + stub AiTaggingService (no endpoint) pattern as prior slices
- **Progress capture:** `new Progress<ScanProgress>(p => list.Add(p))` — requires `await Task.Delay(50)` after `RunAsync` to let synchronous Progress<T> callbacks fire on the thread pool
- **Cancellation test:** Cancel `CancellationTokenSource` inside the progress callback after first Tagging event; assert `ThrowsAnyAsync<OperationCanceledException>` on RunAsync
- **Already-processed test:** Run scan once → mark item `IsTagged=true` via DB → second run has no `ScanPhase.Tagging` events
- **Temp dir teardown:** Create temp dir in constructor, delete in `Dispose(recursive: true)` — write MinimalPng bytes directly into temp dir so scanner finds real files
- **Total test suite after:** 39 passing (33 prior + 6 new)

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
### 2026-03-26: Slice #1 Verification Guide Created
- ✅ **Completed:** VERIFY-SLICE-1.md in project root
- **Purpose:** Step-by-step verification checklist for GitHub issue #2 (Project Bootstrap)
- **Coverage:** All 5 acceptance criteria from issue #2 mapped to executable verification steps
- **Format:** Numbered steps with exact commands, expected outputs, and pass/fail sign-off
- **Target User:** Frank (developer-level verification, Mac platform)
- **Next:** After Frank completes HITL verification, confirm all acceptance criteria met and mark slice complete

### 2026-03-27 — Slice #6: AI Tagging Tests (Batch, Base64, Parsing)

**Branch:** `squad/6-ai-tagging-service` (Rusty's branch)  
**PR:** #14 (targeting dev, tests included)  
**Status:** ✅ Complete (3 new tests added)  

**What I tested:**

1. **Batch Processing Test**
   - Loads multiple media items sequentially
   - Calls TagMediaItemAsync on each item
   - Validates response parsing for all items
   - Confirms database updates for batch workflows

2. **Base64 Data URL Structure Test**
   - Validates vision message format for image transmission
   - Confirms `image_url` content block structure
   - Uses `CapturingHttpHandler` to inspect actual HTTP request payload
   - Verifies Base64 encoding is correct (no double-encoding, proper MIME type)

3. **Case-Insensitive Response Parsing Test**
   - Tests response with varied capitalization: "DESCRIPTION: / Tags: / type:"
   - Confirms fallback parsing works for non-standard formats
   - Validates that service handles response variations gracefully

**Test Infrastructure:**
- `CapturingHttpHandler` custom mock handler: captures request payload for inspection
- In-memory database for test isolation
- Stub/real mode toggle via AiTaggingService constructor

**Test Coverage Summary:**
- Rusty: 9 original tests (stub response, real HTTP structure, parsing)
- Linus: 3 new tests (batch, Base64, case-insensitive)
- **Total:** 33 tests passing (21 prior + 9 Rusty + 3 Linus)

**Key learnings:**
- Base64 data URLs for vision messages must include MIME type prefix: `data:image/jpeg;base64,...`
- Case-insensitive string operations essential for robust response parsing
- CapturingHttpHandler pattern lets tests inspect actual HTTP request payloads (not just mocking responses)
- Batch processing validates service works with multiple database transactions in sequence

**Next steps:**
- PR #14 review and merge to dev
- Tests ready for CI/CD validation
- Batch and Base64 patterns validate real endpoint will work correctly

### 2026-03-27 — Slice #8 — ScanProgressService Tests

**Branch:** `squad/8-rescan-progress`  
**PR:** #16 (targeting dev)  
**Status:** ✅ Complete (6 new tests, all passing)  

**What I tested:**

1. **Progress Reporting Test**
   - Validates `IProgress<ScanProgress>` invocations during scan
   - Confirms granular "Processing image X of Y" messages
   - Verifies progress callbacks occur for each item processed

2. **Cancellation Token Test**
   - Confirms `CancellationToken` propagates through `RunAsync`
   - Validates scan stops gracefully when cancellation is triggered
   - Checks that partial results are committed before cancellation

3. **Folder Resolution Test**
   - Validates `SCAN_FOLDER` env var takes precedence
   - Tests fallback to `Environment.SpecialFolder.MyPictures`
   - Confirms path resolution works with various env states

4. **Per-Item Tagging Loop Test**
   - Validates service calls `AiTaggingService.TagImageAsync` per item
   - Confirms loop continues on individual item failures
   - Checks database state after each item tagged

5. **Success/Failure Scenario Tests** (2 more)
   - Happy path: all items tagged successfully
   - Partial failure: some items fail, scan continues, rest succeed

**Test Infrastructure:**
- Mock `IProgress<ScanProgress>` to capture progress reports
- Mock `CancellationToken` for cancellation testing
- In-memory database for test isolation (shared with DatabaseService tests)
- Mock `AiTaggingService` for predictable behavior

**Test Coverage Summary:**
- **Total ScanProgressService tests:** 6 passing
- **Integration:** Works with existing AiTaggingService and DatabaseService mocks
- **Build:** 0 errors, 0 warnings

**Key learnings:**
- `IProgress<T>` is inherently testable with mock implementations
- `CancellationToken.CreateLinkedTokenSource()` pattern enables precise cancellation testing
- Per-item loop allows partial progress reporting (critical for UI responsiveness)
- Folder env var fallback must be tested separately from main scan logic

**Next steps:**
- PR #16 review and merge to dev
- Slice #9 (Theming) ready to merge in parallel
- Combined, Slices #7-9 complete the rescan/progress/theming stack


### 2026-06-13: ImageImportService Tests Written
- ✅ **Branch:** `squad/add-images-manual-import` (committed as 851700f)
- **Service under test:** `ImageImportService` — imports file paths into DB, deduplicates by SHA256 hash, filters unsupported extensions, sets `IsTagged=false`
- **Pattern:** In-memory SQLite via `DatabaseService(SqliteConnection)` constructor; real temp files with unique content (JPEG header + counter bytes) so each file hashes distinctly
- **Key fix:** `CreateTempImageFile` appends `BitConverter.GetBytes(++_fileCounter)` so all files get unique SHA256 hashes; without this, identical content → same hash → duplicate detection fires
- **Cleanup:** `List<string> _tempFiles` + `Dispose()` deletes temp files; `SqliteConnection` disposed in same method
- **Tests written (7):**
  1. `ImportAsync_WithNewImages_ReturnsCorrectAddedCount` — 3 distinct files → Added=3, Skipped=0
  2. `ImportAsync_WithDuplicateHash_SkipsSilently` — same file twice → second call Added=0, Skipped=1
  3. `ImportAsync_WithUnsupportedExtension_CountsAsUnsupported` — `.raw` + `.txt` → Unsupported=2
  4. `ImportAsync_WithMixedFiles_ReturnsCorrectCounts` — 2 new + 1 duplicate + 1 unsupported → Added=2, Skipped=1, Unsupported=1
  5. `ImportAsync_NewImage_IsStoredInDatabase` — `GetAllAsync()` returns the imported item
  6. `ImportAsync_NewImage_IsNotTagged` — `IsTagged=false` confirmed via DB read-back
  7. `ImportAsync_WithEmptyList_ReturnsZeros` — empty input → 0/0/0, no exception
- **Total test suite after:** 46 passing (39 prior + 7 new)
