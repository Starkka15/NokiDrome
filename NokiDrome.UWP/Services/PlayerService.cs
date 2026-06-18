using System;
using System.Collections.Generic;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Services
{
    public class PlayerService
    {
        private readonly MediaPlayer _player;
        private readonly SystemMediaTransportControls _smtc;
        private PlayQueue _queue = new PlayQueue();

        // Guards the MediaFailed -> Next() cascade: a transient failure shouldn't
        // race through the whole queue. Reset whenever a track actually starts.
        private int _consecutiveFailures;
        private const int MaxConsecutiveFailures = 3;

        // ── Events ────────────────────────────────────────────────────────────

        public event EventHandler<Song>              TrackChanged;
        public event EventHandler<MediaPlaybackState> StateChanged;
        public event EventHandler<TimeSpan>          PositionChanged;

        // ── Properties ────────────────────────────────────────────────────────

        public Song             CurrentSong => _queue.CurrentSong;
        public bool             IsPlaying   => _player.PlaybackSession?.PlaybackState == MediaPlaybackState.Playing;
        public bool             HasQueue    => _queue.Items.Count > 0;
        public RepeatMode       RepeatMode  { get => _queue.RepeatMode; set { _queue.RepeatMode = value; } }
        public bool             IsShuffled  { get => _queue.IsShuffled; set => _queue.IsShuffled = value; }
        public List<Song>       Queue       => _queue.Items;
        public TimeSpan         Position    => _player.PlaybackSession?.Position ?? TimeSpan.Zero;
        public TimeSpan         Duration    => _player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;

        public PlayerService()
        {
            _player = new MediaPlayer();
            _player.AudioCategory = MediaPlayerAudioCategory.Media;

            // Disable the built-in command manager. With a single MediaSource (we drive
            // the queue manually, not via MediaPlaybackList) it has no next/previous item,
            // so it swallows the SMTC/Bluetooth Next & Previous commands before our manual
            // ButtonPressed handler sees them — leaving only Play/Pause working. Turning it
            // off routes every SMTC button to OnSmtcButtonPressed below.
            _player.CommandManager.IsEnabled = false;

            _player.MediaEnded   += OnMediaEnded;
            _player.MediaFailed  += OnMediaFailed;
            _player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            _player.PlaybackSession.PositionChanged      += OnPositionChanged;

            _smtc = _player.SystemMediaTransportControls;
            _smtc.IsEnabled         = true;
            _smtc.IsPlayEnabled     = true;
            _smtc.IsPauseEnabled    = true;
            _smtc.IsNextEnabled     = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.ButtonPressed     += OnSmtcButtonPressed;
        }

        // ── Queue control ─────────────────────────────────────────────────────

        public void SetQueue(List<Song> songs, int startIndex = 0)
        {
            _queue.Items        = new List<Song>(songs);
            _queue.CurrentIndex = -1;
            _queue.IsShuffled   = false;
            _queue.ShuffleOrder.Clear();
            PlayAt(startIndex);
        }

        public void ShuffleCurrentQueue()
        {
            var items = _queue.Items;
            if (items.Count < 2) { _queue.IsShuffled = true; return; }
            int start = Math.Max(_queue.CurrentIndex + 1, 0);
            var rng = new Random();
            for (int i = items.Count - 1; i > start; i--)
            {
                int j = rng.Next(start, i + 1);
                var tmp = items[i]; items[i] = items[j]; items[j] = tmp;
            }
            _queue.IsShuffled = true;
        }

        public void ShuffleAndPlay(List<Song> songs)
        {
            var shuffled = new List<Song>(songs);
            var rng = new Random();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j   = rng.Next(i + 1);
                var tmp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = tmp;
            }
            SetQueue(shuffled, 0);
            _queue.IsShuffled = true;
        }

        public void Next()
        {
            if (_queue.Items.Count == 0) return;

            if (_queue.RepeatMode == RepeatMode.One)
            {
                Seek(TimeSpan.Zero);
                Play();
                return;
            }

            var nextIdx = _queue.CurrentIndex + 1;
            if (nextIdx >= _queue.Items.Count)
            {
                if (_queue.RepeatMode == RepeatMode.All)
                    nextIdx = 0;
                else
                    return;
            }
            PlayAt(nextIdx);
        }

        public void Prev()
        {
            if (_queue.Items.Count == 0) return;
            if (Position.TotalSeconds > 3)
            {
                Seek(TimeSpan.Zero);
                return;
            }
            var prevIdx = _queue.CurrentIndex - 1;
            if (prevIdx < 0)
                prevIdx = _queue.RepeatMode == RepeatMode.All ? _queue.Items.Count - 1 : 0;
            PlayAt(prevIdx);
        }

        public void PlayAt(int index)
        {
            if (index < 0 || index >= _queue.Items.Count) return;
            _queue.CurrentIndex = index;
            var song = _queue.CurrentSong;

            // Metadata/UI update immediately; the audio source is resolved async
            // (a cached local file may need to be opened before playback).
            UpdateSmtc(song);
            TrackChanged?.Invoke(this, song);
            TileService.Update(song);
            var nowPlaying = App.Subsonic.ScrobbleAsync(song.Id, submission: false);

            var _ = SetSourceAndPlayAsync(song);
        }

        private async System.Threading.Tasks.Task SetSourceAndPlayAsync(Song song)
        {
            MediaSource source = null;

            // Prefer a downloaded copy — works with no network.
            if (App.Offline != null && App.Offline.IsOffline(song.Id))
            {
                var file = await App.Offline.GetFileAsync(song.Id);
                if (file != null) source = MediaSource.CreateFromStorageFile(file);
            }

            if (source == null)
                source = MediaSource.CreateFromUri(new Uri(App.Subsonic.GetStreamUrl(song.Id)));

            // Guard against a newer PlayAt having superseded this call.
            if (_queue.CurrentSong?.Id != song.Id) return;

            _player.Source = source;
            _player.Play();

            // Auto-cache the track as it plays, if enabled and not already stored.
            if (App.Offline != null && App.Settings.AutoCache && !App.Offline.IsOffline(song.Id))
            {
                var cache = App.Offline.DownloadAsync(song);
            }
        }

        // ── Transport ─────────────────────────────────────────────────────────

        public void Play()  => _player.Play();
        public void Pause() => _player.Pause();

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause(); else Play();
        }

        public void Seek(TimeSpan position)
            => _player.PlaybackSession.Position = position;

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnMediaEnded(MediaPlayer sender, object args)
        {
            var song = _queue.CurrentSong;
            if (song != null)
            {
                var submit = App.Subsonic.ScrobbleAsync(song.Id, submission: true);
            }
            Next();
        }

        private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            // Stop auto-advancing once several tracks fail in a row — otherwise a
            // network drop skips through the entire queue in a fraction of a second.
            if (++_consecutiveFailures >= MaxConsecutiveFailures)
            {
                _consecutiveFailures = 0;
                Pause();
                return;
            }
            Next();
        }

        private void OnPlaybackStateChanged(MediaPlaybackSession session, object args)
        {
            var state = session.PlaybackState;
            if (state == MediaPlaybackState.Playing) _consecutiveFailures = 0;
            StateChanged?.Invoke(this, state);
            _smtc.PlaybackStatus = state == MediaPlaybackState.Playing
                ? MediaPlaybackStatus.Playing
                : MediaPlaybackStatus.Paused;
        }

        private void OnPositionChanged(MediaPlaybackSession session, object args)
            => PositionChanged?.Invoke(this, session.Position);

        private void OnSmtcButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:     Play();  break;
                case SystemMediaTransportControlsButton.Pause:    Pause(); break;
                case SystemMediaTransportControlsButton.Next:     Next();  break;
                case SystemMediaTransportControlsButton.Previous: Prev();  break;
            }
        }

        private void UpdateSmtc(Song song)
        {
            if (song == null) return;
            var updater = _smtc.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title       = song.Title;
            updater.MusicProperties.Artist      = song.ArtistName;
            updater.MusicProperties.AlbumTitle  = song.AlbumName;
            updater.MusicProperties.TrackNumber = (uint)song.TrackNumber;

            // Album art on the lock screen / Bluetooth display.
            if (!string.IsNullOrEmpty(song.CoverArtId))
            {
                try
                {
                    var artUrl = App.Subsonic.GetCoverArtUrl(song.CoverArtId);
                    updater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference
                        .CreateFromUri(new Uri(artUrl));
                }
                catch { /* leave thumbnail unset on a bad art id/url */ }
            }

            updater.Update();
        }
    }
}
