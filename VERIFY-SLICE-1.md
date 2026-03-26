# Verification Guide: Slice #1 — Project Bootstrap

**Issue:** [#2](https://github.com/fboucher/local-ai-search/issues/2)  
**Target:** Verify the Uno Platform solution builds and runs correctly

---

## Prerequisites
- .NET 8 SDK installed
- Git repository cloned
- Terminal open in project root

---

## Verification Steps

### 1. Fetch and checkout the Slice #1 branch
```bash
git fetch origin
git checkout slice-1-bootstrap
```
**Success:** Branch checked out without errors.  
**Maps to:** Setup for all criteria

---

### 2. Verify solution file exists
```bash
ls -la *.sln
```
**Success:** You see `LocalAiSearch.sln` (or similar `.sln` file) in the listing.  
**Maps to:** ✅ "Solution file (.sln) created with Uno project"

---

### 3. Verify project structure
```bash
find . -type d -name "Services" -o -name "Models" -o -name "Views" -o -name "ViewModels" | head -10
```
**Success:** Output shows directories: `Services/`, `Models/`, `Views/`, `ViewModels/`  
**Maps to:** ✅ "Project structure matches PRD (Services/, Models/, Views/, ViewModels/)"

---

### 4. Verify MainPage.xaml exists and contains content
```bash
find . -name "MainPage.xaml" -exec cat {} \;
```
**Success:** File found and displays XAML with text content (e.g., "Hello World" or app name visible in the markup).  
**Maps to:** ✅ "Basic MainPage.xaml with 'Hello World' or app name displayed"

---

### 5. Build the solution
```bash
dotnet build
```
**Success:** Build completes with `Build succeeded.` message and 0 errors. Warnings are acceptable.  
**Maps to:** ✅ "App compiles without errors"

---

### 6. Run the application
```bash
dotnet run --project <ProjectName>/<ProjectName>.csproj
```
*(Replace `<ProjectName>` with the actual project folder/file discovered in step 2)*

**Success:** Application window launches. You see the UI with the content from MainPage.xaml rendered.  
**Maps to:** ✅ "App launches on at least one platform for verification"

---

### 7. Close the app
Press `Ctrl+C` in terminal or close the application window.

**Success:** App closes cleanly without crashes.

---

## Final Sign-Off

**Date:** _______________  
**Tester:** _______________

- [ ] All 5 acceptance criteria verified and passing
- [ ] No blocking issues found
- [ ] Ready to merge

**Status:** ⬜ PASS  |  ⬜ FAIL

**Notes:**  
_________________________________  
_________________________________  
_________________________________
