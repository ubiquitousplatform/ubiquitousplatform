# Libra — Media Library Application

> A plugin-based media library built on Ubiquitous, competing with Audiobookshelf, Calibre, Plex, Jellyfin, and ROM managers.

---

## Vision

One self-hosted application to manage **all** your media: books, audiobooks, music, video, comics, and game ROMs. Built entirely as Ubiquitous serverless functions + plugins, proving the platform can handle real-world applications.

---

## Why "Libra"

The name evokes "library" and "balance" (the constellation). A balanced, universal media library.

---

## Competitive Landscape

| App | Media Type | Strengths | Weaknesses |
|-----|-----------|-----------|------------|
| **Plex** | Video, Music | Polished UI, transcoding, wide device support | Proprietary, requires account, moving to ad-supported |
| **Jellyfin** | Video, Music | Open source Plex alternative | Complex setup, transcoding issues, no ebook support |
| **Audiobookshelf** | Audiobooks, Podcasts | Best-in-class audiobook management | Single media type only |
| **Calibre** | Ebooks | Most powerful ebook management | Desktop-only, ugly UI, learning curve |
| **Calibre-Web** | Ebooks | Web UI for Calibre libraries | Limited features, depends on Calibre DB |
| **Kavita** | Comics, Manga, Books | Modern UI, multi-format | Limited audiobook/video support |
| **Komga** | Comics, Manga | Clean UI, good metadata | Comics only |
| **RetroArch** | ROMs | Comprehensive emulator frontend | Not a library manager, complex config |
| **RomM** | ROMs | Web-based ROM manager | Early stage, limited features |
| **Navidrome** | Music | Subsonic-compatible, lightweight | Music only |

### The Gap
No single application manages books, audiobooks, video, music, comics, AND game ROMs. Users currently need 3-5 different apps. Libra unifies them.

---

## Architecture

### Plugin-Based Design

```
┌──────────────────────────────────────────────┐
│                   Libra Core                  │
│                                               │
│  Library Scanner │ Metadata Engine │ User Mgmt│
│  Progress Sync   │ Search Index   │ API       │
└────────┬──────────────────────────┬───────────┘
         │                          │
    ┌────┴────┐              ┌──────┴──────┐
    │ Plugins │              │   Web UI    │
    └────┬────┘              │   (Static)  │
         │                   └─────────────┘
         ├── @libra/books        (EPUB, PDF, MOBI)
         ├── @libra/audiobooks   (MP3, M4B, chapters)
         ├── @libra/comics       (CBZ, CBR, PDF)
         ├── @libra/video        (MKV, MP4, stream)
         ├── @libra/music        (FLAC, MP3, albums)
         └── @libra/roms         (ROMs, BIOSes, emulators)
```

Each media type is a **plugin**. The core provides:
- Library scanning and file watching
- Metadata fetching and caching
- User management and progress tracking
- Search indexing
- Unified API
- Web UI shell

Plugins provide:
- File format detection and parsing
- Media-type-specific metadata sources
- Custom UI components (readers, players)
- Streaming/transcoding logic

---

## Core Functions

### Library Management

```typescript
// functions/library/scan.ts
// Triggered by: cron schedule or manual API call
// Walks configured directories, identifies media files, delegates to plugins
export default async function scan(input: { path: string }) {
  const files = await storage.list(input.path);
  for (const file of files) {
    const mediaType = detectMediaType(file);
    await events.emit('media.discovered', { file, mediaType });
  }
}
```

```typescript
// functions/library/index.ts
// Maintains the search index
export default async function index(event: { file: string, metadata: any }) {
  await kv.set(`index:${event.file}`, JSON.stringify({
    ...event.metadata,
    indexed_at: Date.now()
  }));
}
```

### User & Progress

```typescript
// functions/users/progress.ts
// Sync reading/playback progress across devices
export async function POST(input: { mediaId: string, position: number, userId: string }) {
  const key = `progress:${input.userId}:${input.mediaId}`;
  await kv.set(key, JSON.stringify({
    position: input.position,
    updated_at: Date.now()
  }));
  return { synced: true };
}

export async function GET(input: { mediaId: string, userId: string }) {
  const key = `progress:${input.userId}:${input.mediaId}`;
  const data = await kv.get(key);
  return data ? JSON.parse(data) : { position: 0 };
}
```

---

## Plugin Specifications

### @libra/books

**Supported formats**: EPUB, PDF, MOBI, AZW3, FB2, TXT, RTF, DJVU

**Metadata sources**: Open Library, Google Books, ISBN APIs

**Features**:
- EPUB/PDF parsing for cover extraction
- ISBN detection and auto-metadata
- Series detection and ordering
- Reading progress (page/percentage)
- Highlights and bookmarks (stored in KV)
- OPDS feed for e-reader compatibility

**Functions**:
| Function | Description |
|----------|-------------|
| `books/detect` | Identify book files, extract ISBN |
| `books/metadata` | Fetch metadata from Open Library / Google Books |
| `books/cover` | Extract or fetch cover image |
| `books/read` | Serve book content for web reader |
| `books/opds` | OPDS catalog feed |

---

### @libra/audiobooks

**Supported formats**: MP3, M4A, M4B, FLAC, OGG (folder-based or single file)

**Metadata sources**: Audible (scraping), Google Books, Audnexus

**Features**:
- Folder-based audiobook detection (author/title/chapter*.mp3)
- M4B chapter marker parsing
- Playback progress with position-in-chapter
- Sleep timer support (client-side)
- Playback speed adjustment (client-side)
- Series tracking

**Functions**:
| Function | Description |
|----------|-------------|
| `audiobooks/detect` | Identify audiobook folders/files |
| `audiobooks/metadata` | Fetch metadata, chapter info |
| `audiobooks/stream` | HTTP range streaming for audio files |
| `audiobooks/chapters` | Return chapter list with timecodes |

---

### @libra/comics

**Supported formats**: CBZ, CBR, CB7, PDF, EPUB (comics)

**Metadata sources**: ComicVine, MangaDex, AniList

**Features**:
- Archive extraction (zip, rar, 7z)
- Page ordering and metadata
- Reading progress (page-based)
- Series and volume tracking
- Manga reading direction support (RTL)

---

### @libra/video

**Supported formats**: MKV, MP4, AVI, WebM

**Metadata sources**: TMDB, TVDB, OMDb

**Features**:
- Movie and TV show detection (Plex naming conventions)
- Metadata and poster art fetching
- Subtitle detection and serving (SRT, ASS, VTT)
- HTTP range streaming
- Watch progress tracking
- Basic transcoding (via WASM or native plugin)

**Note**: Video transcoding is the most challenging feature. Options:
1. Direct play only (client must support the codec)
2. WASM-based transcoding (slow but sandboxed)
3. Native transcoding plugin (FFmpeg wrapper, breaks sandbox)

For MVP, **direct play + subtitle conversion** is sufficient.

---

### @libra/music

**Supported formats**: FLAC, MP3, M4A, OGG, WAV, OPUS

**Metadata sources**: MusicBrainz, Last.fm, Discogs

**Features**:
- Album/artist/track organization
- Tag reading (ID3, Vorbis comments)
- Cover art extraction
- Subsonic/Navidrome-compatible API (for existing music apps)
- Scrobbling to Last.fm / ListenBrainz
- Playlist management

---

### @libra/roms

**Supported formats**: Any ROM file (NES, SNES, GBA, N64, PS1, etc.)

**Metadata sources**: No-Intro DATs, IGDB, ScreenScraper, TheGamesDB

**Features**:
- Per-console organization
- BIOS file management with hash verification
- ROM hash verification against No-Intro databases
- Metadata and box art fetching
- Save state management (storage)
- Emulator configuration docs/links
- Categorization: console, handheld, arcade, computer

**Functions**:
| Function | Description |
|----------|-------------|
| `roms/detect` | Identify ROM files, detect console by extension/header |
| `roms/verify` | Hash ROM and verify against No-Intro database |
| `roms/metadata` | Fetch game metadata and art from IGDB/TheGamesDB |
| `roms/bios` | BIOS library management, hash verification |
| `roms/organize` | Auto-organize ROMs by console |

---

## Web UI

### Technology
- Static SPA (React, Svelte, or vanilla JS + htmx)
- Served directly by Ubiquitous runtime (no separate web server)
- Responsive (mobile, tablet, desktop)
- PWA support for mobile home screen install

### Screens

#### Dashboard
- Recently added media
- Continue reading/watching/playing
- Library statistics

#### Library Browser
- Grid view with cover art (like Plex)
- Filter by: media type, genre, author/artist, year, rating
- Search across all media types
- Sort by: title, date added, last accessed, progress

#### Book Reader
- EPUB reader (epub.js or similar)
- PDF viewer
- Progress tracking
- Bookmark/highlight support
- Font/theme customization

#### Audio Player
- Persistent player bar (like Spotify)
- Chapter navigation
- Speed control
- Sleep timer
- Queue management

#### Video Player
- HTML5 video player
- Subtitle overlay
- Progress bar with chapter markers
- Quality selection (if multiple files)

#### ROM Browser
- Console-organized grid
- Box art display
- Links to emulator setup guides
- BIOS status indicators (have/missing)

#### Admin Panel
- Library path configuration
- Media scan triggers
- User management
- Plugin management (enable/disable, configure)
- System health and storage usage

---

## Data Model

### Stored in KV Store

```
media:{id}              → { type, title, metadata, paths, ... }
progress:{userId}:{id}  → { position, percentage, updatedAt }
user:{id}               → { name, email, preferences }
collection:{id}         → { name, mediaIds, userId }
series:{id}             → { name, mediaIds, order }
index:{term}            → { mediaIds }
plugin:{name}:config    → { ...config }
bios:{console}:{hash}   → { name, verified, path }
```

### Stored in File Storage

```
media/{id}/original     → Original media file (or symlink)
media/{id}/cover        → Cover art (extracted or fetched)
media/{id}/thumbnail    → Thumbnail for grid view
media/{id}/subtitles/*  → Subtitle files
```

---

## Implementation Phases

### Phase 1: Core + Books (4 weeks)
- Library scanner (walk directories)
- Book detection (EPUB, PDF by extension)
- Open Library metadata fetching
- KV-based library index
- Basic web UI (grid + book reader)
- Reading progress sync

### Phase 2: Audiobooks (3 weeks)
- Folder-based audiobook detection
- Audio file streaming (HTTP range)
- Chapter detection
- Audio player UI
- Playback progress sync

### Phase 3: Comics + ROMs (3 weeks)
- Archive extraction (CBZ)
- Comic reader UI
- ROM detection and organization
- BIOS hash verification
- ROM browser UI

### Phase 4: Video + Music (4 weeks)
- Video file detection and TMDB metadata
- Video streaming (direct play)
- Subtitle support
- Music tag reading and MusicBrainz metadata
- Music player UI with Subsonic API

### Phase 5: Polish (2 weeks)
- Search across all media types
- Collections and playlists
- UI polish and responsive design
- Documentation and setup guide

---

## Why This Showcases Ubiquitous

Libra exercises every Ubiquitous feature:

| Ubiquitous Feature | Libra Usage |
|--------------------|-------------|
| Functions | Every API endpoint is a function |
| KV Store | Library index, progress, user data |
| File Storage | Media files, cover art, thumbnails |
| Events | Media discovery → metadata fetch → index |
| Cron | Scheduled library scans |
| HTTP Client | Metadata API calls (TMDB, Open Library) |
| Plugins | Each media type is a plugin |
| Middleware | Auth, CORS |
| Static file serving | Web UI |
| Hot reload | Develop UI + functions together |
| Test harness | Test each function in isolation |

If you can build a competitive media library on Ubiquitous, you can build anything.
