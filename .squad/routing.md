# Squad Routing

## Routing Rules

| Domain | Owner | Notes |
|--------|-------|-------|
| Architecture, design decisions, code review | Bertha | Lead — final call on structure |
| .NET services (FolderScannerService, AiTaggingService, DatabaseService, ImageDisplayService) | Rusty | Backend Dev |
| Models (MediaItem), data contracts | Rusty | Backend Dev |
| Turso/libsql database setup, migrations, SQL | Rusty | Backend Dev |
| Reka AI API integration | Rusty | Backend Dev |
| XAML views, MainPage.xaml | Livingston | Frontend Dev |
| ViewModels (MainViewModel), UI state, search/filter | Livingston | Frontend Dev |
| SkiaSharp image rendering, image display | Livingston | Frontend Dev |
| Dark/light theming, system theme detection | Livingston | Frontend Dev |
| Unit tests (services) | Linus | Tester |
| Integration tests (database) | Linus | Tester |
| Acceptance criteria verification | Linus | Tester |
| Memory, decisions, session logs | Scribe | Silent — Coordinator spawns after each batch |
| Work queue, GitHub issues backlog | Ralph | Monitor — activated on request |

## Cascade Rules

- Cross-cutting concerns (e.g., error handling strategy) → Bertha first
- UI needs a service contract → Livingston asks Rusty
- Service needs architectural sign-off → Rusty flags to Bertha
- Test gaps identified → Linus files in decisions inbox

## Escalation

If a reviewer rejects work, the Coordinator selects a *different* agent to revise. The original author is locked out for that revision cycle.
