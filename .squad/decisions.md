# Squad Decisions

## Slice #1: Project Bootstrap

### 1. Slice #1 HITL Collaboration Plan

**Date:** 2026-03-26  
**Author:** Bertha  
**Status:** Active  

**Decision:** Implement Slice #1 bootstrap via coordinated HITL workflow with clear phase boundaries and handoff points.

**Context:**
- Issue #2 requires Uno Platform project scaffold
- Frank's machine is the only environment to verify cross-platform runtime
- Multiple agents can work autonomously if phases are clearly defined

**Phases:**
1. **Phase 1 (Rusty):** Autonomous project scaffold and PR creation
2. **Phase 2 (Bertha):** Autonomous architecture review
3. **Phase 3 (Frank):** HITL verification on macOS
4. **Phase 4 (Linus):** Autonomous acceptance criteria confirmation

**Handoff Table:**
| Step | Who | Unblocks |
|------|-----|----------|
| Rusty opens PR | Rusty | Bertha |
| Bertha approves | Bertha | Frank |
| Frank runs verification | **Frank** | Linus + Merge |
| Linus confirms acceptance | Linus | Slice #2+ |

**Rationale:**
- Separates autonomous work from HITL work
- Minimizes Frank's time investment (~15 minutes)
- Enables parallel preparation (Bertha reviews while Rusty builds)
- Clear dependencies prevent wasted effort

**Impact:**
- ✅ Slice #1 can proceed with high efficiency
- ✅ Sets pattern for future HITL work
- ✅ Frank's involvement focused on verification, not setup

---

### 2. Manual Uno Platform Scaffold (Slice #1)

**Date:** 2026-03-26  
**Author:** Rusty  
**Status:** Implemented (Pending HITL)

**Decision:** Create manual Uno Platform project scaffold instead of installing templates.

**Context:**
- Uno Platform templates not installed on Frank's machine (`dotnet new list` showed no Uno templates)
- Need clean project structure for Slice #1 bootstrap

**Approach:**
1. Multi-target framework: `net8.0-windows10.0.19041;net8.0-ios;net8.0-android;net8.0-maccatalyst`
2. Package references: `Uno.WinUI` (5.*) and `Uno.Extensions.Hosting` (*)
3. Folder structure: Models/, Services/, ViewModels/, Views/
4. Placeholder classes with TODO comments referencing PRD

**Alternatives Considered:**
- **Install templates first:** Adds setup complexity and potential version mismatches
- **Single-target framework:** Defeats cross-platform requirement
- **Older Uno version:** Latest (5.*) chosen for best feature set

**Rationale:**
- Manual scaffold gives full control over structure
- Multi-target aligns with PRD's cross-platform requirement
- Wildcards (5.*, *) ensure latest stable versions
- TODOs clarify implementation scope for future slices

**Risk Mitigation:**
- Frank's HITL verification will catch any launch issues
- Structure validated against official Uno conventions during review

**Impact:**
- ✅ Clean, modular structure matching PRD
- ✅ Cross-platform ready from start
- ⚠️ Manual structure may differ slightly from official templates (mitigation: Frank's HITL test)

---

### 3. Source Folder and .NET 10 Framework

**Date:** 2026-03-26  
**Author:** Frank (directive) + Rusty (implementation)  
**Status:** Implemented

**Decision:** Source code relocated to `src/` folder; target framework upgraded to .NET 10. Solution file moved into `src/` to align with repo-wide convention: code artifacts in `src/`, config-only at root.

**Context:**
- Frank's explicit user directive for repository structure
- .NET 10 released Nov 2025, current LTS better suited for new projects than .NET 8
- Clarification: .sln file should also live in src/ per Frank's update directive

**Approach:**
1. Relocated source folder: `LocalAiSearch/` → `src/LocalAiSearch/`
2. Moved solution file: `LocalAiSearch.sln` → `src/LocalAiSearch.sln`
3. Updated solution file to reference `src\LocalAiSearch\LocalAiSearch.csproj`
4. Upgraded all target frameworks: `net8.0-*` → `net10.0-*`
   - net10.0-windows10.0.19041
   - net10.0-ios
   - net10.0-android
   - net10.0-maccatalyst
5. Preserved git history using `git mv` during relocation

**Rationale:**
- Frank's directive establishes clear repository layout convention
- .NET 10 LTS provides better long-term support and features than .NET 8
- Cross-platform targets maintained across all relocations
- Moving .sln into src/ clarifies artifact organization: repo root becomes purely configuration

**Impact:**
- ✅ Repository follows established src/ convention
- ✅ Project targets current .NET LTS
- ✅ All 4 platform targets updated consistently
- ✅ Clear separation: code in src/, configuration at root

---
## Active Decisions

### 1. Slice #1 HITL Collaboration Plan

**Date:** 2026-03-26  
**Author:** Bertha  
**Status:** Active  

**Decision:** Implement Slice #1 bootstrap via coordinated HITL workflow with clear phase boundaries and handoff points.

**Context:**
- Issue #2 requires Uno Platform project scaffold
- Frank's machine is the only environment to verify cross-platform runtime
- Multiple agents can work autonomously if phases are clearly defined

**Phases:**
1. **Phase 1 (Rusty):** Autonomous project scaffold and PR creation
2. **Phase 2 (Bertha):** Autonomous architecture review
3. **Phase 3 (Frank):** HITL verification on macOS
4. **Phase 4 (Linus):** Autonomous acceptance criteria confirmation

**Handoff Table:**
| Step | Who | Unblocks |
|------|-----|----------|
| Rusty opens PR | Rusty | Bertha |
| Bertha approves | Bertha | Frank |
| Frank runs verification | **Frank** | Linus + Merge |
| Linus confirms acceptance | Linus | Slice #2+ |

**Rationale:**
- Separates autonomous work from HITL work
- Minimizes Frank's time investment (~15 minutes)
- Enables parallel preparation (Bertha reviews while Rusty builds)
- Clear dependencies prevent wasted effort

**Impact:**
- ✅ Slice #1 can proceed with high efficiency
- ✅ Sets pattern for future HITL work
- ✅ Frank's involvement focused on verification, not setup

---

### 2. Manual Uno Platform Scaffold (Slice #1)

**Date:** 2026-03-26  
**Author:** Rusty  
**Status:** Implemented (Pending HITL)

**Decision:** Create manual Uno Platform project scaffold instead of installing templates.

**Context:**
- Uno Platform templates not installed on Frank's machine (`dotnet new list` showed no Uno templates)
- Need clean project structure for Slice #1 bootstrap

**Approach:**
1. Multi-target framework: `net8.0-windows10.0.19041;net8.0-ios;net8.0-android;net8.0-maccatalyst`
2. Package references: `Uno.WinUI` (5.*) and `Uno.Extensions.Hosting` (*)
3. Folder structure: Models/, Services/, ViewModels/, Views/
4. Placeholder classes with TODO comments referencing PRD

**Alternatives Considered:**
- **Install templates first:** Adds setup complexity and potential version mismatches
- **Single-target framework:** Defeats cross-platform requirement
- **Older Uno version:** Latest (5.*) chosen for best feature set

**Rationale:**
- Manual scaffold gives full control over structure
- Multi-target aligns with PRD's cross-platform requirement
- Wildcards (5.*, *) ensure latest stable versions
- TODOs clarify implementation scope for future slices

**Risk Mitigation:**
- Frank's HITL verification will catch any launch issues
- Structure validated against official Uno conventions during review

**Impact:**
- ✅ Clean, modular structure matching PRD
- ✅ Cross-platform ready from start
- ⚠️ Manual structure may differ slightly from official templates (mitigation: Frank's HITL test)

---

### 3. Source Folder and .NET 10 Framework

**Date:** 2026-03-26  
**Author:** Frank (directive) + Rusty (implementation)  
**Status:** Implemented

**Decision:** Source code relocated to `src/` folder; target framework upgraded to .NET 10. Solution file moved into `src/` to align with repo-wide convention: code artifacts in `src/`, config-only at root.

**Context:**
- Frank's explicit user directive for repository structure
- .NET 10 released Nov 2025, current LTS better suited for new projects than .NET 8
- Clarification: .sln file should also live in src/ per Frank's update directive

**Approach:**
1. Relocated source folder: `LocalAiSearch/` → `src/LocalAiSearch/`
2. Moved solution file: `LocalAiSearch.sln` → `src/LocalAiSearch.sln`
3. Updated solution file to reference `src\LocalAiSearch\LocalAiSearch.csproj`
4. Upgraded all target frameworks: `net8.0-*` → `net10.0-*`
   - net10.0-windows10.0.19041
   - net10.0-ios
   - net10.0-android
   - net10.0-maccatalyst
5. Preserved git history using `git mv` during relocation

**Rationale:**
- Frank's directive establishes clear repository layout convention
- .NET 10 LTS provides better long-term support and features than .NET 8
- Cross-platform targets maintained across all relocations
- Moving .sln into src/ clarifies artifact organization: repo root becomes purely configuration

**Impact:**
- ✅ Repository follows established src/ convention
- ✅ Project targets current .NET LTS
- ✅ All 4 platform targets updated consistently
- ✅ Clear separation: code in src/, configuration at root

---

## Slice #6: AI Tagging Service

### AI Tagging Service — Stub Approach & OpenAI Format

**Date:** 2026-03-27  
**Author:** Rusty  
**Status:** Implemented (PR #14)  
**Issue:** #6  

**Decision:** Implement `AiTaggingService` with a **stub mode** controlled by a `_isStub` boolean. The real OpenAI-format HTTP call structure is written in full; the stub short-circuits before any network I/O.

**Context:**
- The real AI API lives on Frank's local network and uses OpenAI-compatible format (`/v1/chat/completions`, vision messages with `image_url` content type).
- The API is not available in CI or other dev environments.
- Frank's directive: stub the actual HTTP call for now, but design the interface as if it makes real calls so wiring up the real endpoint requires zero code changes.

**Approach:**
1. Stub activation: `_isStub = string.IsNullOrWhiteSpace(resolvedUrl)` — no endpoint configured → stub; any endpoint value → real HTTP call path.
2. Real HTTP path (written, not yet activated):
   - POST to `{endpoint}/v1/chat/completions`
   - Model: `"local-model"` (constant, easily changed)
   - Vision-style message: `image_url` content block (Base64 data URL) + text prompt
   - Prompt requests structured `DESCRIPTION:` / `TAGS:` / `TYPE:` format
   - Parses response with fallback for unrecognized formats
3. Stub response: Description/Tags/MediaType with default values

**Configuration (plug in real AI — zero code changes):**
```bash
export AI_ENDPOINT="http://192.168.1.50:1234"
dotnet run
```

**Alternatives Considered:**
- **Interface/mock pattern:** Extra ceremony; not needed until multiple implementations
- **Feature flag in config file:** Env var simpler and already required per spec
- **Always make real HTTP call:** Breaks CI and offline dev; Frank explicitly asked for stub

**Impact:**
- ✅ CI and offline dev work with zero config
- ✅ Real local-network AI plugs in via one env var, no code changes
- ✅ HTTP call structure (headers, payload shape, response parsing) already written and tested via mocked handler
- ✅ 9 passing unit tests + 3 additional tests from Linus (batch, Base64, case-insensitive parsing)

---

## Slice #7: Search & Filter

### Search & Filter UI Patterns

**Date:** 2026-03-27  
**Author:** Livingston  
**Status:** Implemented (PR #15)  
**Issue:** #7  

**Decision:** Wire MainViewModel to real DatabaseService with debounced search and filter UI patterns.

**Pattern 1: Expose Visibility from ViewModel Instead of Using XAML Converter**

WinUI 3 / Uno Platform has no built-in BoolToVisibilityConverter. Rather than registering a converter in XAML resources, expose a `Visibility` computed property (e.g. `EmptyStateVisibility`) directly from the ViewModel. This is pragmatic and avoids XAML boilerplate. The ViewModel already takes a dependency on `Microsoft.UI.Xaml` for `Visibility`.

*Alternatives considered:* Custom converter in XAML; `x:Bind` function binding. Both add more friction for no meaningful architectural benefit in this project.

**Pattern 2: Debounce Pattern for Search**

Use `CancellationTokenSource` + `Task.Delay(300ms)` in the ViewModel to debounce search input. Cancel the previous CTS on each new keystroke. This avoids a separate timer component and works cleanly with `async/await`.

**Pattern 3: MediaTypeOptions as Instance Property**

Bind-able properties accessed via `{x:Bind ViewModel.Prop}` must be instance properties — not `static readonly` fields. The Uno source generator emits code that accesses the binding path through an instance reference, causing CS0176 if the member is static.

**Bug Fix: Microsoft.Data.Sqlite Missing from csproj**

DatabaseService was already implemented using `Microsoft.Data.Sqlite` but the package was never added to `LocalAiSearch.csproj`. Added `<PackageReference Include="Microsoft.Data.Sqlite" Version="*" />` as part of this slice to unblock the build.

**Impact:**
- ✅ Search debounce prevents excessive database queries
- ✅ Filter integrates with UI ComboBox binding
- ✅ Empty state provides user feedback when no results
- ✅ 33 tests passing (all prior tests maintained)
- ✅ Patterns established for future ViewModel-to-View bindings

---

## Merged Decisions (2026-03-27)

### 2026-03-26: Slices #2/#3/#4 merged to dev

**PRs Merged:**
- PR #12: Database Foundation (Issue #3) — MediaItem model + DatabaseService + 9 unit tests
- PR #13: Folder Scanner (Issue #5) — FolderScannerService + SHA256 dedup + 12 unit tests
- PR #11: Image Viewer UI (Issue #4) — MainViewModel + MediaItemViewModel + two-panel XAML

**Issues Closed:** #3, #4, #5

**Dev Branch Status:**
- Bootstrap (Slice #1) ✅
- Database Foundation (Slice #2) ✅
- Folder Scanner (Slice #3) ✅
- Image Viewer UI (Slice #4) ✅

**Next Up:** Slices #5-9 (AI Tagging, Search & Filter, Rescan & Progress, Theming, Polish)

**Technical Notes:**
- All PRs retargeted from main/bootstrap-branch to dev before merge
- Build artifacts (bin/obj) created merge conflicts — should be gitignored
- Manual merges required due to conflicts between TODO stubs and implementations
- All code uses net10.0 TFM, async/await patterns, proper folder structure
- No ThemeResource issues found (avoiding known GTK failure pattern)

### 7. Bertha Final Review — Slices #7 & #8 Merge (Round 2)

**Date:** 2026-03-27  
**Reviewer:** Bertha  
**PRs:** #16 (Rescan & Progress) and #17 (Dark/light theming)  
**Status:** ✅ Both merged to `dev` via squash

**PR #16 — feat: Slice #7 & #8 — Rescan & Progress Service**
- Branch: `squad/8-rescan-progress` → `dev`
- Authors: Rusty (implementation) + Linus (tests)
- ✅ `ScanProgressService.cs` orchestrates scanner → tagger in sequence
- ✅ `IProgress<ScanProgress>` pattern with nullability handled correctly
- ✅ `CancellationToken` threaded through `RunAsync`, loop checks, OperationCanceledException re-thrown properly
- ✅ `MainViewModel`: RescanCommand, CancelScanCommand, IsScanning, progress properties
- ✅ Layer separation: service has no XAML, ViewModel has no filesystem calls
- ✅ 6 new tests by Linus, all 39 tests passing on branch
- ✅ Dual-constructor pattern on MainViewModel is clean DI design
- ✅ PR targets `dev`, issue #8 closed

**PR #17 — feat: Slice #9 — Dark/Light Theming**
- Branch: `squad/9-theming` → `dev`
- Author: Livingston
- ✅ `App.xaml` has `ThemeDictionaries` with `Light` and `Dark` keys (explicit inline)
- ✅ 11 custom `AppXxx` brush keys per theme, NOT relying on WinUI built-ins
- ✅ `App.xaml.cs`: `Frame.RequestedTheme` set on `OnLaunched` from `Application.RequestedTheme`
- ✅ `MainPage.xaml`: all hardcoded colors replaced with `{ThemeResource AppXxx}` references
- ✅ `ApplicationPageBackgroundThemeBrush` absent — GTK layout bug avoided
- ✅ Layout integrity preserved, no new structural elements
- ✅ 0 errors, 0 warnings
- ✅ PR targets `dev`, issue #9 closed

**Dev Branch Final State:**
- `dotnet build` → Success. 0 warnings, 0 errors
- `dotnet test` → Passed! 39 tests, 0 failures
- Slices complete: Bootstrap + DB + Scanner + Image Viewer + AI Tagging + Search + Rescan & Progress + Theming
- All 8 critical slices merged and tested

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
