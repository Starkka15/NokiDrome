# NokiDrome

A Subsonic/Navidrome music client for **Windows 10 Mobile** (Lumia 1520 and other W10M devices).

Built with UWP, targeting the Creators Update (build 15063 / 1703) minimum.

## Features

- Browse library by Albums, Artists, Songs, Genres, and Playlists
- Starred songs tab — star/unstar tracks from the Now Playing screen
- Search songs, albums, and artists
- Full-screen Now Playing with album art, scrubber, shuffle, repeat, and star
- Mini player visible on every screen while music is playing
- Background audio — continues playing when the screen locks or app is backgrounded
- Lock screen / Bluetooth controls via System Media Transport Controls
- Shuffle and repeat modes (Off / All / One)
- Last.fm-compatible scrobbling via the Subsonic API

## Supported Servers

| Server | Status |
|--------|--------|
| [Navidrome](https://www.navidrome.org/) | ✅ Tested |
| Any Subsonic-compatible server | ⚠️ Untested — should work |

Authentication uses the Subsonic token method (MD5 + salt). Plain-text auth is not used.

## Building

Requires **Visual Studio 2017+** with the UWP workload and the Windows 10 SDK (16299).

1. Open `NokiDrome.sln`
2. Restore NuGet packages
3. Build `Release | ARM`
4. Run `PackAndInstallARM.ps1` (as Administrator in the VM) to pack, sign, and produce `NokiDrome_ARM.appx`
5. Side-load the APPX onto the device via the Windows Device Portal or `WinAppDeployCmd`

## Settings

On first launch the app redirects to Settings. Enter:

- **Server URL** — e.g. `http://192.168.1.x:4533` or `https://music.example.com`
- **Username** and **Password**

Use **Test Connection** to verify before saving.

## License

MIT
