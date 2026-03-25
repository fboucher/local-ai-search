# Local AI Search - Project Plan

## Overview
Desktop application to manage and search local images using AI-powered tagging and descriptions.

## Technology Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| **UI Framework** | Uno Platform | Cross-platform C# (Win/Mac/Linux), VS Code dev environment |
| **Database** | Turso (libsql) | Local file only (`libsql://file:./local.db`), future sync option |
| **AI Integration** | Reka API | `https://api.reka.ai/v1`, running on separate LAN machine |
| **Image Processing** | SkiaSharp | Cross-platform image handling |

## Features

### 1. Folder Scanning
- User selects a single root folder
- Recursively scans for images (jpg, png, webp, gif, bmp)
- Skips RAW formats (CR2, NEF, ARW, etc.)
- Detects new/modified files via file hash comparison
- Manual rescan button for incremental updates
- Detects duplicate files by hash - only stores first occurrence
- Missing/deleted files remain in DB (not auto-removed)

### 2. AI Tagging Service
- Sequential processing (one image at a time)
- Images sent via Base64 encoding as data URL
- Sends full resolution images (no pre-resizing)
- Extracts: description, type (photo/screenshot/other), tags
- On failure: skip and continue to next image
- Stores results in Turso database

### 3. Local Database Schema
```sql
CREATE TABLE media_items (
  id TEXT PRIMARY KEY,
  file_path TEXT NOT NULL,
  description TEXT,
  media_type TEXT,  -- photo, screenshot, image, etc.
  tags TEXT,        -- JSON array
  thumbnail_path TEXT,
  file_hash TEXT,
  created_at INTEGER,
  updated_at INTEGER
);
```

### 4. Search & Filter UI
- Text search using LIKE/substring matching (description, tags)
- Filter by media type
- Sort by date/name
- Infinite scroll with fixed-size grid tiles
- No thumbnail generation - images resized to fit in UI

### 5. Image Viewer
- Thumbnail grid (fixed tiles, infinite scroll)
- Full-size preview on selection
- Metadata display (tags, description)
- Image loaded at display resolution (no pre-caching)

### 6. Theming
- Auto-follow system dark/light mode preference

## Project Structure
```
local-ai-search/
├── src/LocalAiSearch/
│   ├── Services/
│   │   ├── FolderScannerService.cs
│   │   ├── AiTaggingService.cs
│   │   ├── DatabaseService.cs
│   │   └── ImageDisplayService.cs
│   ├── Models/
│   │   └── MediaItem.cs
│   ├── Views/
│   │   └── MainPage.xaml
│   └── ViewModels/
│       └── MainViewModel.cs
└── LocalAiSearch.sln
```

## Research Items Before Implementation

1. **Uno Platform setup** - Verify Uno 5.x supports .NET 8+ and has SkiaSharp integration
2. **Turso .NET client** - Check libsql-client-ts vs managed C# driver availability
3. **Reka API** - Review API docs for image analysis endpoint and rate limits
4. **File watching** - Not needed for v1 (manual rescan only)

## API Call Example

Reka API accepts images via Base64 data URLs:

```
curl http://<lan-ip>:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "reka-edge-2603",
    "messages": [{
      "role": "user",
      "content": [
        {"type": "image_url", "image_url": {"url": "data:image/jpeg;base64,..."}},
        {"type": "text", "text": "Describe this image in detail."}
      ]
    }]
  }'
```

## Design Decisions Summary

| Decision | Choice |
|----------|--------|
| UI Framework | Uno Platform |
| Database | Turso/libsql (local file only) |
| AI API | Reka via LAN, sequential, skip on failure |
| Image delivery | Base64 encoding, full resolution |
| Thumbnails | None - resize original in UI |
| Folder | Single root folder |
| Formats | JPG, PNG, WEBP, GIF, BMP only |
| Search | Simple LIKE/substring |
| Grid | Fixed tiles, infinite scroll |
| Updates | Manual rescan button |
| Duplicates | Skip by hash |
| Missing files | Keep in DB |
| Theme | Auto-follow system |

## Known Risks

1. **Base64 + full-res** - Large images create large payloads. Consider adding max size warning.
2. **Sequential AI** - 10,000 images at 2s each = ~5.5 hours. Design progress UI.
3. **Uno on Linux** - Less battle-tested than Avalonia.
4. **Turso for local-only** - Complexity for unused feature. Document trade-off.

(End of file - total 136 lines)
