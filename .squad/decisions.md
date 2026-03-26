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

**Decision:** Source code relocated to `src/` folder; target framework upgraded to .NET 10.

**Context:**
- Frank's explicit user directive for repository structure
- .NET 10 released Nov 2025, current LTS better suited for new projects than .NET 8

**Approach:**
1. Relocated source folder: `LocalAiSearch/` → `src/LocalAiSearch/`
2. Updated solution file to reference `src\LocalAiSearch\LocalAiSearch.csproj`
3. Upgraded all target frameworks: `net8.0-*` → `net10.0-*`
   - net10.0-windows10.0.19041
   - net10.0-ios
   - net10.0-android
   - net10.0-maccatalyst
4. Preserved git history using `git mv` during relocation

**Rationale:**
- Frank's directive establishes clear repository layout convention
- .NET 10 LTS provides better long-term support and features than .NET 8
- Cross-platform targets maintained across all relocations

**Impact:**
- ✅ Repository follows established src/ convention
- ✅ Project targets current .NET LTS
- ✅ All 4 platform targets updated consistently

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
