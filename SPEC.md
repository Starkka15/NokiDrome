# NokiDrome — Specification

Navidrome / Subsonic client for Windows 10 Mobile (Lumia 1520).
Native C# UWP, ARM32. Dark music-player UI in the spirit of Groove Music.

---

## Target Device

- **Primary**: Nokia Lumia 1520 (ARM, 6" 1080×1920, W10M)
- **Secondary**: Any ARM or x64 device running Windows 10 Mobile / Windows 10
- **Min SDK**: 10.0.16299.0 (Fall Creators Update — same as PocketTavern)
- **Build**: ARM Release + x64 Debug (for local testing in VM)

---

## Backend: Subsonic API

Navidrome exposes a Subsonic-compatible REST API. NokiDrome targets the
Subsonic 1.16.1 / OpenSubsonic spec with JSON responses.

### Authentication

Token-based (preferred over basic auth):

```
u=<username>
t=md5(<password> + <salt>)
s=<random salt>
v=1.16.1
c=nokidrome
f=json
```

All requests append these query parameters.

### Endpoints Used

| Endpoint | Purpose |
|---|---|
| `ping` | Test connection / verify credentials |
| `getArtists` | Full artist index (alphabetical buckets) |
| `getArtist` | Artist detail + album list |
| `getAlbum` | Album detail + track list |
| `getSong` | Single track metadata |
| `getAlbumList2` | Sorted album lists (alphabetical, byYear, recent, random) |
| `getGenres` | Genre list with counts |
| `getSongsByGenre` | Tracks filtered by genre |
| `search3` | Search artists, albums, songs |
| `getPlaylists` | All playlists |
| `getPlaylist` | Playlist detail + tracks |
| `createPlaylist` | Create / overwrite playlist |
| `updatePlaylist` | Add/remove tracks, rename |
| `deletePlaylist` | Delete playlist |
| `stream` | Audio stream URL (appended auth params) |
| `getCoverArt` | Album/artist art (appended auth params) |
| `scrobble` | Report playback to Navidrome (submission + now playing) |
| `getStarred2` | Starred/favourited tracks and albums |
| `star` / `unstar` | Star/unstar a track or album |

---

## Solution Structure

```
NokiDrome.sln
NokiDrome.UWP/
  Assets/
  Models/
  Services/
  ViewModels/
  Views/
  Controls/
  Converters/
  App.xaml / App.xaml.cs
  Package.appxmanifest
```

---

## Models

```csharp
SubsonicServer    { Url, Username, Password }
Artist            { Id, Name, AlbumCount, CoverArtId }
Album             { Id, Name, ArtistId, ArtistName, Year, Genre, SongCount, Duration, CoverArtId }
Song              { Id, Title, ArtistId, ArtistName, AlbumId, AlbumName, TrackNumber, DiscNumber,
                    Year, Genre, Duration, BitRate, CoverArtId, StreamUrl }
Genre             { Name, SongCount, AlbumCount }
Playlist          { Id, Name, SongCount, Duration, CoverArtId, Entries: List<Song> }
PlayQueue         { Items: List<Song>, CurrentIndex, ShuffleOrder, RepeatMode }
```

---

## Services

### `SubsonicClient`
- Builds authenticated URLs
- `GetAsync<T>(endpoint, params)` — fetch + deserialize JSON
- `GetStreamUrl(songId)` — returns authenticated stream URL (no download, passed to MediaPlayer)
- `GetCoverArtUrl(id, size)` — returns authenticated art URL

### `PlayerService`
- Singleton wrapping `Windows.Media.Playback.MediaPlayer`
- Manages `PlayQueue` state
- Exposes: `Play`, `Pause`, `Stop`, `Next`, `Prev`, `Seek`, `SetQueue`, `ShuffleAll`
- Registers `SystemMediaTransportControls` (lock screen / Bluetooth controls)
- Fires `PlaybackStateChanged`, `TrackChanged`, `PositionChanged` events
- Survives page navigation (singleton on `App`)

### `CoverArtCache`
- In-memory LRU cache (≤50 entries) for decoded `BitmapImage` objects
- Falls back to placeholder on miss / error

### `SettingsService`
- Stores `SubsonicServer` config in `ApplicationData.Current.LocalSettings`
- Password stored in `PasswordVault`

### `AccentColorService`
- Extracts dominant color from album art bitmap for dynamic theming
- Used by Now Playing page for background tint

---

## Pages / Views

### `NowPlayingPage`
- Full-screen dark layout
- **Blurred album art** as background (low-opacity, RenderTransform blur)
- Large centered album art (300×300 or fill available width)
- Track title (large), Artist / Album (subtitle)
- Scrubber (`Slider`) with elapsed / remaining time
- Controls row: ⏮ Prev · ⏪ Rewind · ⏯ Play/Pause · ⏩ FastFwd · ⏭ Next
- Second row: 🔀 Shuffle · 🔁 Repeat (off / all / one) · ★ Star · ⋮ Add to playlist
- Swipe up or chevron button → `QueueFlyout` (track list, reorderable)
- Dynamic accent tint from album art dominant color

### `LibraryPage` (Pivot)
Pivot tabs: **Artists · Albums · Songs · Genres · Playlists**

Top of page: **Shuffle All** button (full-width accent button, queues entire library)

**Artists tab**
- Alphabetical grouped list (jump-list letters)
- Tap → `ArtistDetailPage`

**Albums tab**
- Sort picker: A–Z · Year (new→old) · Year (old→new) · Recently Added · Random
- Grid of album art tiles (2-column on Lumia 1520 logical width)
- Tap → `AlbumDetailPage`

**Songs tab**
- Sort picker: A–Z · Artist · Album · Duration
- Flat list of tracks with artist/album subtitle
- Tap → enqueues and plays; long-press → context menu (Play Next, Add to Queue, Add to Playlist, Star)

**Genres tab**
- List of genres with song/album count badges
- Tap → `GenreDetailPage` (album grid filtered to genre)

**Playlists tab**
- List of playlists with track count
- Tap → `PlaylistDetailPage`
- Long-press → Delete playlist

### `ArtistDetailPage`
- Header: artist name, album count
- Album grid (same 2-column tile layout)
- Tap album → `AlbumDetailPage`

### `AlbumDetailPage`
- Header: large album art + title / artist / year / genre / duration
- Track list (number · title · duration)
- "Play Album" and "Shuffle Album" buttons in header
- Long-press track → Play Next / Add to Queue / Add to Playlist / Star

### `GenreDetailPage`
- Header: genre name + counts
- Album grid for that genre
- "Shuffle Genre" button

### `PlaylistDetailPage`
- Header: playlist name + track count / duration
- Track list (drag-reorder handle, swipe-to-remove)
- "Play" and "Shuffle" buttons
- Rename via tap on title

### `SearchPage`
- Single search box
- Results sections: Artists / Albums / Songs (collapsed if empty)
- Tap → navigate to detail or play

### `SettingsPage`
- Server URL field
- Username / Password fields
- Test Connection button + result label
- Clear Cache button
- App version

---

## Mini Player (Persistent)

Shown at the bottom of every page except `NowPlayingPage`.
- Album art thumbnail (40×40)
- Track title + artist (truncated)
- Play/Pause button
- Next button
- Tap anywhere (not a button) → navigate to `NowPlayingPage`

Implemented as a `UserControl` in the root `Frame` shell, shown/hidden based on `PlayerService.HasTrack`.

---

## Navigation

Bottom app bar with 4 icons:
- 🎵 Library
- ▶ Now Playing
- 🔍 Search
- ⚙ Settings

`App.Navigation` singleton (same `NavigationService` pattern as PocketTavern).
Back button (W10M hardware back) pops the frame stack.

---

## Playback Architecture

`MediaPlayer` (foreground, not background task) with `SystemMediaTransportControls`:
- Display metadata + art on lock screen
- Respond to hardware media keys and Bluetooth controls
- `AudioCategory = BackgroundCapableMedia` in manifest
- Foreground approach is simpler and sufficient for W10M

Stream URL passed directly to `MediaPlayer.Source` as authenticated URI — no local
buffering needed; Navidrome handles transcoding/streaming.

---

## Shuffle Implementation

**Shuffle All**: fetches all songs via `getAlbumList2(type=alphabeticalByName, size=500)`
repeated until exhausted, builds flat list, Fisher-Yates shuffle, sets as queue.

**Shuffle Queue**: Fisher-Yates shuffles the current queue in-place from current track onward.

**Shuffle Album / Genre / Playlist**: same — build list, shuffle, set queue.

---

## Offline / Caching

**Implemented (v1.2).** `OfflineService` caches original (untranscoded) tracks via the
Subsonic `download` endpoint.

- **Storage location** chosen in Settings: Internal (`ApplicationData.LocalFolder\NokiDrome\`)
  or SD card (`KnownFolders.RemovableDevices` → first device → `NokiDrome\`). SD requires the
  `removableStorage` capability + audio `FileTypeAssociation` (declared in the manifest).
- **Manual pin**: "Download for offline" button on `AlbumDetailPage` (`DownloadManyAsync`).
- **Auto-cache** (Settings toggle): tracks are saved as they play (`PlayerService` →
  `OfflineService.DownloadAsync` on `PlayAt`).
- **Index** of cached songs is persisted as JSON in the internal app folder
  (`offline_index.json`) so the Offline list survives an absent SD card. Loaded at startup.
- **Playback** prefers a local file when present (`MediaSource.CreateFromStorageFile`),
  else streams — so pinned music plays with no network.
- **Offline page** (bottom-nav tab): lists downloaded tracks, play / shuffle / remove.
  Settings has a "Clear all downloads" action.

Filenames: `{songId}.{suffix}` (suffix from the Subsonic `song.suffix` field).

---

## Starred / Favourites

- Star button on Now Playing and track long-press menus
- `getStarred2` populates a **Starred** section in Library (or separate tab, TBD)
- State tracked in memory; re-fetched on app launch

---

## Theming

- Base: dark background (`#FF1A1A1A`), white text
- Accent: dynamic per album (extracted by `AccentColorService`) on Now Playing
- Elsewhere: fixed accent color from settings (default: `#FF1DB954` Spotify-green or
  `#FF0078D7` Windows blue — TBD)
- No light theme

---

## Package Identity

```
Package name:    NokiDrome
Publisher:       CN=Starkka15
Package family:  NokiDrome_<hash>
Min version:     10.0.16299.0
Capabilities:    internetClient, musicLibrary (for background audio)
```

---

## Build Targets

| Config | Platform | Purpose |
|---|---|---|
| Debug | x64 | Local VM testing |
| Release | ARM | Lumia 1520 deploy |

Sideload via Device Portal (same workflow as PocketTavern).

---

## Task List

### T1 — Solution scaffold
Create `.sln`, `.csproj` (min SDK 16299), `Package.appxmanifest`, base `App.xaml`,
`NavigationService`, `ViewModelBase`, resource dictionaries (colors, styles, brushes).

### T2 — SubsonicClient service
Auth URL builder, `GetAsync<T>`, `GetStreamUrl`, `GetCoverArtUrl`.
Unit-testable with mock server.

### T3 — SettingsPage + SettingsService
Server config UI, `PasswordVault` storage, Test Connection.

### T4 — PlayerService
`MediaPlayer` wrapper, `PlayQueue`, `SystemMediaTransportControls`,
`ShuffleAll`, Repeat modes, `PositionChanged` timer.

### T5 — NowPlayingPage
Full layout, scrubber, controls, blurred art background, `AccentColorService`.

### T6 — Mini player control
Persistent bottom bar, wired to `PlayerService`.

### T7 — AlbumDetailPage + ArtistDetailPage
Album art header, track list, play/shuffle buttons.

### T8 — LibraryPage
Pivot with all tabs, `Shuffle All` button, sort pickers, album grid.

### T9 — GenreDetailPage + PlaylistDetailPage
Genre filter, playlist reorder/remove, rename.

### T10 — SearchPage
Search box, results sections, navigation.

### T11 — Starred / favourites
Star button wiring, `getStarred2` fetch, display in library.

### T12 — Scrobble
Report `now playing` on track start, `submission` at 50% or 4 min played.

### T13 — Polish + ARM deploy
Performance pass, Lumia 1520 layout verification, sideload and test.

### T14 — Offline caching (v1.2, DONE)
`OfflineService` (download/remove/index/storage-resolution), Settings storage picker +
auto-cache toggle + clear, Offline nav page, album pin button, offline-aware `PlayAt`.
Manifest: `removableStorage` + audio file-type associations.

### T15 — Playback robustness (v1.2, DONE)
- `MediaFailed` → `Next()` cascade guard (stop after 3 consecutive failures).
- SMTC `Thumbnail` (album art on lock screen / Bluetooth).
- `MediaPlayer.CommandManager.IsEnabled = false` so SMTC/Bluetooth **Next & Previous**
  route to our handler (previously only Play/Pause worked — the command manager swallowed
  next/prev with a single `MediaSource`).
