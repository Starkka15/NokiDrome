using System;
using Windows.UI.Core;
using NokiDrome.UWP.Models;
using NokiDrome.UWP.Services;

namespace NokiDrome.UWP.ViewModels
{
    public class NowPlayingViewModel : ViewModelBase
    {
        private CoreDispatcher _dispatcher;

        private string _title      = "Nothing playing";
        private string _artistName = "";
        private string _albumName  = "";
        private string _coverArtUrl = "";
        private bool   _isPlaying   = false;
        private bool   _isShuffled  = false;
        private bool   _isStarred   = false;
        private RepeatMode _repeatMode = RepeatMode.Off;
        private double _positionSeconds = 0;
        private double _durationSeconds = 1;
        private bool   _isSeeking = false;

        public string Title         { get => _title;       set => Set(ref _title, value); }
        public string ArtistName    { get => _artistName;  set => Set(ref _artistName, value); }
        public string AlbumName     { get => _albumName;   set => Set(ref _albumName, value); }
        public string CoverArtUrl   { get => _coverArtUrl; set => Set(ref _coverArtUrl, value); }
        public bool   IsPlaying     { get => _isPlaying;   set => Set(ref _isPlaying, value); }
        public bool   IsShuffled    { get => _isShuffled;  set => Set(ref _isShuffled, value); }
        public bool   IsStarred     { get => _isStarred;   set => Set(ref _isStarred, value); }
        public RepeatMode RepeatMode { get => _repeatMode; set => Set(ref _repeatMode, value); }

        public double PositionSeconds
        {
            get => _positionSeconds;
            set => Set(ref _positionSeconds, value);
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set => Set(ref _durationSeconds, value);
        }

        public string PositionText => FormatTime(TimeSpan.FromSeconds(_positionSeconds));
        public string DurationText => FormatTime(TimeSpan.FromSeconds(_durationSeconds));

        public string RepeatGlyph
        {
            get
            {
                switch (_repeatMode)
                {
                    case RepeatMode.One: return ""; // repeat one
                    default:            return ""; // repeat all / off
                }
            }
        }

        public bool RepeatActive => _repeatMode != RepeatMode.Off;

        public bool IsSeeking
        {
            get => _isSeeking;
            set => _isSeeking = value;
        }

        // ── Init / cleanup ────────────────────────────────────────────────────

        public void Initialize(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            App.Player.TrackChanged += OnTrackChanged;
            App.Player.StateChanged += OnStateChanged;
            RefreshFromPlayer();
        }

        public void Cleanup()
        {
            App.Player.TrackChanged -= OnTrackChanged;
            App.Player.StateChanged -= OnStateChanged;
        }

        // ── Poll (called by DispatcherTimer every 500ms) ──────────────────────

        public void PollPosition()
        {
            if (_isSeeking) return;
            var pos = App.Player.Position.TotalSeconds;
            var dur = App.Player.Duration.TotalSeconds;
            if (dur < 1) dur = 1;
            _positionSeconds = pos;
            _durationSeconds = dur;
            Notify(nameof(PositionSeconds));
            Notify(nameof(DurationSeconds));
            Notify(nameof(PositionText));
            Notify(nameof(DurationText));
        }

        // ── Commands ──────────────────────────────────────────────────────────

        public void TogglePlayPause() => App.Player.TogglePlayPause();
        public void Next()  => App.Player.Next();
        public void Prev()  => App.Player.Prev();

        public void Seek(double seconds)
            => App.Player.Seek(TimeSpan.FromSeconds(seconds));

        public void ToggleShuffle()
        {
            if (!_isShuffled)
            {
                if (App.Player.Queue.Count > 0)
                    App.Player.ShuffleCurrentQueue();
                IsShuffled = true;
            }
            else
            {
                App.Player.IsShuffled = false;
                IsShuffled = false;
            }
        }

        public void CycleRepeat()
        {
            switch (_repeatMode)
            {
                case RepeatMode.Off: RepeatMode = RepeatMode.All; break;
                case RepeatMode.All: RepeatMode = RepeatMode.One; break;
                case RepeatMode.One: RepeatMode = RepeatMode.Off; break;
            }
            App.Player.RepeatMode = _repeatMode;
            Notify(nameof(RepeatGlyph));
            Notify(nameof(RepeatActive));
        }

        public async void ToggleStar()
        {
            var song = App.Player.CurrentSong;
            if (song == null) return;
            IsStarred = !_isStarred;
            if (_isStarred)
                await App.Subsonic.StarAsync(song.Id);
            else
                await App.Subsonic.UnstarAsync(song.Id);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void RefreshFromPlayer()
        {
            var song = App.Player.CurrentSong;
            if (song != null)
            {
                Title      = song.Title;
                ArtistName = song.ArtistName;
                AlbumName  = song.AlbumName;
                CoverArtUrl = string.IsNullOrEmpty(song.CoverArtId)
                    ? "" : App.Subsonic.GetCoverArtUrl(song.CoverArtId, 600);
                IsStarred  = song.IsStarred;
            }
            IsPlaying  = App.Player.IsPlaying;
            IsShuffled = App.Player.IsShuffled;
            RepeatMode = App.Player.RepeatMode;
            Notify(nameof(RepeatGlyph));
            Notify(nameof(RepeatActive));
        }

        private async void OnTrackChanged(object sender, Song song)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Title      = song.Title;
                ArtistName = song.ArtistName;
                AlbumName  = song.AlbumName;
                CoverArtUrl = string.IsNullOrEmpty(song.CoverArtId)
                    ? "" : App.Subsonic.GetCoverArtUrl(song.CoverArtId, 600);
                IsStarred  = song.IsStarred;
                _positionSeconds = 0;
                _durationSeconds = song.Duration > 0 ? song.Duration : 1;
                Notify(nameof(PositionSeconds));
                Notify(nameof(DurationSeconds));
                Notify(nameof(PositionText));
                Notify(nameof(DurationText));
            });
        }

        private async void OnStateChanged(object sender, Windows.Media.Playback.MediaPlaybackState state)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IsPlaying = App.Player.IsPlaying;
            });
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
            return $"{t.Minutes}:{t.Seconds:D2}";
        }
    }
}
