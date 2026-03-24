# Local AI Search - Project Plan

## Overview
Desktop application to manage and search local images using AI-powered tagging and descriptions.

## Technology Stack

| Component | Choice | Notes |
|-----------|--------|-------|
| **UI Framework** | Uno Platform | Cross-platform C# (Win/Mac/Linux) |
| **Database** | Turso (libsql) | Local SQLite-compatible DB |
| **AI Integration** | Reka API | `https://api.reka.ai/v1` |
| **Image Processing** | SkiaSharp | Cross-platform image handling |

## Features

### 1. Folder Scanning
- User selects a root folder
- Recursively scans for images (jpg, png, webp, gif, bmp)
- Detects new/modified files vs already-processed

### 2. AI Tagging Service
- Calls Reka API for each unprocessed image
- Extracts: description, type (photo/screenshot/other), tags
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
- Text search (description, tags)
- Filter by media type
- Sort by date/name

### 5. Image Viewer
- Thumbnail grid
- Full-size preview on selection
- Metadata display (tags, description)

## Project Structure
```
local-ai-search/
├── src/LocalAiSearch/
│   ├── Services/
│   │   ├── FolderScannerService.cs
│   │   ├── AiTaggingService.cs
│   │   ├── DatabaseService.cs
│   │   └── ThumbnailService.cs
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
4. **Thumbnail caching** - Determine best approach (local cache folder)

## Open Questions

- [ ] Should thumbnails be generated upfront or on-demand?
- [ ] Any specific image types to exclude (RAW formats, SVG)?
- [ ] Single-folder or multiple folder support?
