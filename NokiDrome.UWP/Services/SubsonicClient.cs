using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Services
{
    // Simple result wrappers — avoids System.ValueTuple which isn't in MNTK 6.1.9
    public class ArtistDetailResult
    {
        public Artist       Artist { get; set; }
        public List<Album>  Albums { get; set; } = new List<Album>();
    }

    public class AlbumDetailResult
    {
        public Album       Album { get; set; }
        public List<Song>  Songs { get; set; } = new List<Song>();
    }

    public class SubsonicClient
    {
        private const string ApiVersion = "1.16.1";
        private const string ClientName = "nokidrome";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly SettingsService _settings;

        public SubsonicClient(SettingsService settings)
        {
            _settings = settings;
        }

        // Called after credentials are saved so the next request picks up the new server config.
        // Nothing to do here since BuildUrl reads _settings on every call.
        public void Refresh() { }

        // ── URL builders ──────────────────────────────────────────────────────

        // extra: alternating key, value strings — e.g. BuildUrl("ping", "id", "123", "size", "10")
        public string BuildUrl(string endpoint, params string[] extra)
        {
            var server = _settings.GetServer();
            var saltBuf = Windows.Security.Cryptography.CryptographicBuffer.GenerateRandom(16);
            var salt    = Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(saltBuf);
            var token  = Md5Hex(server.Password + salt);

            var sb = new StringBuilder();
            sb.Append(server.Url.TrimEnd('/'));
            sb.Append("/rest/").Append(endpoint).Append(".view");
            sb.Append("?u=").Append(Uri.EscapeDataString(server.Username));
            sb.Append("&t=").Append(token);
            sb.Append("&s=").Append(salt);
            sb.Append("&v=").Append(ApiVersion);
            sb.Append("&c=").Append(ClientName);
            sb.Append("&f=json");

            for (int i = 0; i + 1 < extra.Length; i += 2)
                sb.Append("&").Append(Uri.EscapeDataString(extra[i])).Append("=").Append(Uri.EscapeDataString(extra[i + 1]));

            return sb.ToString();
        }

        public string GetStreamUrl(string songId)
            => BuildUrl("stream", "id", songId);

        public string GetCoverArtUrl(string coverArtId, int size = 300)
            => BuildUrl("getCoverArt", "id", coverArtId, "size", size.ToString());

        // Original, untranscoded file — used for offline downloads.
        public string GetDownloadUrl(string songId)
            => BuildUrl("download", "id", songId);

        public async Task<byte[]> DownloadBytesAsync(string songId)
            => await _http.GetByteArrayAsync(GetDownloadUrl(songId));

        // ── API calls ─────────────────────────────────────────────────────────

        public async Task<bool> PingAsync()
        {
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("ping"));
                var root = JObject.Parse(body);
                return root["subsonic-response"]?.Value<string>("status") == "ok";
            }
            catch { return false; }
        }

        public async Task<List<Artist>> GetArtistsAsync()
        {
            var artists = new List<Artist>();
            try
            {
                var body  = await _http.GetStringAsync(BuildUrl("getArtists"));
                var root  = JObject.Parse(body);
                var index = root["subsonic-response"]?["artists"]?["index"] as JArray;
                if (index == null) return artists;
                foreach (var bucket in index)
                    foreach (var a in bucket["artist"] as JArray ?? new JArray())
                        artists.Add(ParseArtist(a as JObject));
            }
            catch { }
            return artists;
        }

        public async Task<ArtistDetailResult> GetArtistAsync(string id)
        {
            var result = new ArtistDetailResult();
            try
            {
                var body  = await _http.GetStringAsync(BuildUrl("getArtist", "id", id));
                var root  = JObject.Parse(body);
                var aNode = root["subsonic-response"]?["artist"] as JObject;
                if (aNode == null) return result;
                result.Artist = ParseArtist(aNode);
                foreach (var al in aNode["album"] as JArray ?? new JArray())
                    result.Albums.Add(ParseAlbum(al as JObject));
            }
            catch { }
            return result;
        }

        public async Task<AlbumDetailResult> GetAlbumAsync(string id)
        {
            var result = new AlbumDetailResult();
            try
            {
                var body   = await _http.GetStringAsync(BuildUrl("getAlbum", "id", id));
                var root   = JObject.Parse(body);
                var alNode = root["subsonic-response"]?["album"] as JObject;
                if (alNode == null) return result;
                result.Album = ParseAlbum(alNode);
                foreach (var s in alNode["song"] as JArray ?? new JArray())
                    result.Songs.Add(ParseSong(s as JObject));
            }
            catch { }
            return result;
        }

        public async Task<List<Album>> GetAlbumListAsync(string type, int size = 50, int offset = 0,
            int fromYear = 0, int toYear = 0)
        {
            var albums = new List<Album>();
            try
            {
                string url;
                if (type == "byYear")
                    url = BuildUrl("getAlbumList2",
                        "type",     type,
                        "size",     size.ToString(),
                        "offset",   offset.ToString(),
                        "fromYear", fromYear.ToString(),
                        "toYear",   toYear.ToString());
                else
                    url = BuildUrl("getAlbumList2",
                        "type",   type,
                        "size",   size.ToString(),
                        "offset", offset.ToString());

                var body = await _http.GetStringAsync(url);
                var root = JObject.Parse(body);
                foreach (var al in root["subsonic-response"]?["albumList2"]?["album"] as JArray ?? new JArray())
                    albums.Add(ParseAlbum(al as JObject));
            }
            catch { }
            return albums;
        }

        public async Task<List<Genre>> GetGenresAsync()
        {
            var genres = new List<Genre>();
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("getGenres"));
                var root = JObject.Parse(body);
                foreach (var g in root["subsonic-response"]?["genres"]?["genre"] as JArray ?? new JArray())
                    genres.Add(new Genre
                    {
                        Name       = g.Value<string>("value") ?? "",
                        SongCount  = g.Value<int>("songCount"),
                        AlbumCount = g.Value<int>("albumCount")
                    });
            }
            catch { }
            return genres;
        }

        public async Task<List<Song>> GetSongsByGenreAsync(string genre, int size = 50, int offset = 0)
        {
            var songs = new List<Song>();
            try
            {
                var body = await _http.GetStringAsync(
                    BuildUrl("getSongsByGenre", "genre", genre, "count", size.ToString(), "offset", offset.ToString()));
                var root = JObject.Parse(body);
                foreach (var s in root["subsonic-response"]?["songsByGenre"]?["song"] as JArray ?? new JArray())
                    songs.Add(ParseSong(s as JObject));
            }
            catch { }
            return songs;
        }

        public async Task<List<Playlist>> GetPlaylistsAsync()
        {
            var lists = new List<Playlist>();
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("getPlaylists"));
                var root = JObject.Parse(body);
                foreach (var p in root["subsonic-response"]?["playlists"]?["playlist"] as JArray ?? new JArray())
                    lists.Add(ParsePlaylist(p as JObject));
            }
            catch { }
            return lists;
        }

        public async Task<Playlist> GetPlaylistAsync(string id)
        {
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("getPlaylist", "id", id));
                var root = JObject.Parse(body);
                var node = root["subsonic-response"]?["playlist"] as JObject;
                if (node == null) return null;
                var pl = ParsePlaylist(node);
                foreach (var s in node["entry"] as JArray ?? new JArray())
                    pl.Entries.Add(ParseSong(s as JObject));
                return pl;
            }
            catch { return null; }
        }

        public async Task<List<Song>> SearchSongsAsync(string query, int count = 20)
        {
            var songs = new List<Song>();
            try
            {
                var body = await _http.GetStringAsync(
                    BuildUrl("search3", "query", query, "songCount", count.ToString(),
                             "artistCount", "0", "albumCount", "0"));
                var root = JObject.Parse(body);
                foreach (var s in root["subsonic-response"]?["searchResult3"]?["song"] as JArray ?? new JArray())
                    songs.Add(ParseSong(s as JObject));
            }
            catch { }
            return songs;
        }

        public async Task<List<Artist>> SearchArtistsAsync(string query, int count = 10)
        {
            var artists = new List<Artist>();
            try
            {
                var body = await _http.GetStringAsync(
                    BuildUrl("search3", "query", query, "artistCount", count.ToString(),
                             "songCount", "0", "albumCount", "0"));
                var root = JObject.Parse(body);
                foreach (var a in root["subsonic-response"]?["searchResult3"]?["artist"] as JArray ?? new JArray())
                    artists.Add(ParseArtist(a as JObject));
            }
            catch { }
            return artists;
        }

        public async Task<List<Album>> SearchAlbumsAsync(string query, int count = 10)
        {
            var albums = new List<Album>();
            try
            {
                var body = await _http.GetStringAsync(
                    BuildUrl("search3", "query", query, "albumCount", count.ToString(),
                             "songCount", "0", "artistCount", "0"));
                var root = JObject.Parse(body);
                foreach (var al in root["subsonic-response"]?["searchResult3"]?["album"] as JArray ?? new JArray())
                    albums.Add(ParseAlbum(al as JObject));
            }
            catch { }
            return albums;
        }

        public async Task<List<Song>> GetStarredSongsAsync()
        {
            var songs = new List<Song>();
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("getStarred2"));
                var root = JObject.Parse(body);
                foreach (var s in root["subsonic-response"]?["starred2"]?["song"] as JArray ?? new JArray())
                    songs.Add(ParseSong(s as JObject));
            }
            catch { }
            return songs;
        }

        public async Task<List<Song>> GetRandomSongsAsync(int count = 50)
        {
            var songs = new List<Song>();
            try
            {
                var body = await _http.GetStringAsync(BuildUrl("getRandomSongs", "size", count.ToString()));
                var root = JObject.Parse(body);
                foreach (var s in root["subsonic-response"]?["randomSongs"]?["song"] as JArray ?? new JArray())
                    songs.Add(ParseSong(s as JObject));
            }
            catch { }
            return songs;
        }

        public async Task ScrobbleAsync(string songId, bool submission = true)
        {
            try
            {
                var url = submission
                    ? BuildUrl("scrobble", "id", songId)
                    : BuildUrl("scrobble", "id", songId, "submission", "false");
                await _http.GetStringAsync(url);
            }
            catch { }
        }

        public async Task StarAsync(string songId)
        {
            try { await _http.GetStringAsync(BuildUrl("star", "id", songId)); }
            catch { }
        }

        public async Task UnstarAsync(string songId)
        {
            try { await _http.GetStringAsync(BuildUrl("unstar", "id", songId)); }
            catch { }
        }

        // ── Parsers ───────────────────────────────────────────────────────────

        private static Artist ParseArtist(JObject o) => o == null ? null : new Artist
        {
            Id         = o.Value<string>("id")       ?? "",
            Name       = o.Value<string>("name")     ?? "",
            AlbumCount = o.Value<int>("albumCount"),
            CoverArtId = o.Value<string>("coverArt") ?? ""
        };

        private static Album ParseAlbum(JObject o) => o == null ? null : new Album
        {
            Id         = o.Value<string>("id")       ?? "",
            Name       = o.Value<string>("name")     ?? "",
            ArtistId   = o.Value<string>("artistId") ?? "",
            ArtistName = o.Value<string>("artist")   ?? "",
            Year       = o.Value<int>("year"),
            Genre      = o.Value<string>("genre")    ?? "",
            SongCount  = o.Value<int>("songCount"),
            Duration   = o.Value<int>("duration"),
            CoverArtId = o.Value<string>("coverArt") ?? ""
        };

        private static Song ParseSong(JObject o) => o == null ? null : new Song
        {
            Id          = o.Value<string>("id")       ?? "",
            Title       = o.Value<string>("title")    ?? "",
            ArtistId    = o.Value<string>("artistId") ?? "",
            ArtistName  = o.Value<string>("artist")   ?? "",
            AlbumId     = o.Value<string>("albumId")  ?? "",
            AlbumName   = o.Value<string>("album")    ?? "",
            TrackNumber = o.Value<int>("track"),
            DiscNumber  = o.Value<int>("discNumber"),
            Year        = o.Value<int>("year"),
            Genre       = o.Value<string>("genre")    ?? "",
            Duration    = o.Value<int>("duration"),
            BitRate     = o.Value<int>("bitRate"),
            CoverArtId  = o.Value<string>("coverArt") ?? "",
            IsStarred   = o["starred"] != null,
            Suffix      = o.Value<string>("suffix")   ?? ""
        };

        private static Playlist ParsePlaylist(JObject o) => o == null ? null : new Playlist
        {
            Id         = o.Value<string>("id")       ?? "",
            Name       = o.Value<string>("name")     ?? "",
            SongCount  = o.Value<int>("songCount"),
            Duration   = o.Value<int>("duration"),
            CoverArtId = o.Value<string>("coverArt") ?? ""
        };

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Md5Hex(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(32);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
