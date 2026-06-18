using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Services
{
    // Manages downloaded tracks for offline playback.
    //
    // Audio files live in the configured storage location (Internal app folder or
    // SD card). The index (song metadata for the Offline view + filename lookup)
    // is always kept in the internal app folder as JSON, so the offline list is
    // available even when the SD card is absent.
    public class OfflineService
    {
        private const string IndexFileName  = "offline_index.json";
        private const string AudioFolderName = "NokiDrome";   // subfolder on the chosen store

        private readonly SettingsService _settings;
        private readonly SubsonicClient  _client;

        // songId -> song metadata. Presence here means "downloaded".
        private readonly Dictionary<string, Song> _index = new Dictionary<string, Song>();
        private bool _loaded;

        public OfflineService(SettingsService settings, SubsonicClient client)
        {
            _settings = settings;
            _client   = client;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public bool IsOffline(string songId)
            => songId != null && _index.ContainsKey(songId);

        public IReadOnlyList<Song> GetOfflineSongs()
            => _index.Values.OrderBy(s => s.ArtistName)
                            .ThenBy(s => s.AlbumName)
                            .ThenBy(s => s.TrackNumber)
                            .ToList();

        public int Count => _index.Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public async Task InitAsync()
        {
            if (_loaded) return;
            try
            {
                var local = ApplicationData.Current.LocalFolder;
                var item  = await local.TryGetItemAsync(IndexFileName);
                if (item is StorageFile file)
                {
                    var json = await FileIO.ReadTextAsync(file);
                    var list = JsonConvert.DeserializeObject<List<Song>>(json) ?? new List<Song>();
                    foreach (var s in list)
                        if (!string.IsNullOrEmpty(s.Id)) _index[s.Id] = s;
                }
            }
            catch { /* corrupt/missing index → start empty */ }
            _loaded = true;
        }

        // ── Download / remove ──────────────────────────────────────────────────

        public async Task<bool> DownloadAsync(Song song)
        {
            if (song == null || string.IsNullOrEmpty(song.Id)) return false;
            if (_index.ContainsKey(song.Id)) return true;   // already cached

            try
            {
                var bytes  = await _client.DownloadBytesAsync(song.Id);
                if (bytes == null || bytes.Length == 0) return false;

                var folder = await GetAudioFolderAsync();
                if (folder == null) return false;

                var file = await folder.CreateFileAsync(FileNameFor(song),
                    CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(file, bytes);

                _index[song.Id] = song;
                await SaveIndexAsync();
                return true;
            }
            catch { return false; }
        }

        // Pin a set of tracks (album / playlist) for offline. Returns count downloaded.
        public async Task<int> DownloadManyAsync(IEnumerable<Song> songs)
        {
            int ok = 0;
            foreach (var s in songs)
                if (await DownloadAsync(s)) ok++;
            return ok;
        }

        public async Task RemoveAsync(string songId)
        {
            if (!_index.TryGetValue(songId, out var song)) return;
            try
            {
                var folder = await GetAudioFolderAsync();
                var item   = folder == null ? null : await folder.TryGetItemAsync(FileNameFor(song));
                if (item != null) await item.DeleteAsync();
            }
            catch { /* file already gone */ }
            _index.Remove(songId);
            await SaveIndexAsync();
        }

        public async Task ClearAllAsync()
        {
            foreach (var id in _index.Keys.ToList())
                await RemoveAsync(id);
        }

        // ── Playback source ─────────────────────────────────────────────────────

        // Returns the local file for a cached song, or null if not available.
        public async Task<StorageFile> GetFileAsync(string songId)
        {
            if (!_index.TryGetValue(songId, out var song)) return null;
            try
            {
                var folder = await GetAudioFolderAsync();
                if (folder == null) return null;
                return await folder.TryGetItemAsync(FileNameFor(song)) as StorageFile;
            }
            catch { return null; }
        }

        // ── Storage resolution ──────────────────────────────────────────────────

        private async Task<StorageFolder> GetAudioFolderAsync()
        {
            try
            {
                StorageFolder root;
                if (_settings.OfflineStorageLocation == OfflineStorage.SdCard)
                {
                    // First removable device is the SD card. Requires the
                    // removableStorage capability + audio file-type associations.
                    var devices = await KnownFolders.RemovableDevices.GetFoldersAsync();
                    root = devices.FirstOrDefault();
                    if (root == null) return null;   // no card inserted
                }
                else
                {
                    root = ApplicationData.Current.LocalFolder;
                }
                return await root.CreateFolderAsync(AudioFolderName,
                    CreationCollisionOption.OpenIfExists);
            }
            catch { return null; }
        }

        private static string FileNameFor(Song s)
        {
            var ext = string.IsNullOrEmpty(s.Suffix) ? "mp3" : s.Suffix;
            return s.Id + "." + ext;
        }

        private async Task SaveIndexAsync()
        {
            try
            {
                var local = ApplicationData.Current.LocalFolder;
                var file  = await local.CreateFileAsync(IndexFileName,
                    CreationCollisionOption.ReplaceExisting);
                var json  = JsonConvert.SerializeObject(_index.Values.ToList());
                await FileIO.WriteTextAsync(file, json);
            }
            catch { /* best-effort persistence */ }
        }
    }
}
