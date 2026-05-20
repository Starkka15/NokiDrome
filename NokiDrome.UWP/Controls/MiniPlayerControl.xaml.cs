using System;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Controls
{
    public sealed partial class MiniPlayerControl : UserControl
    {
        private DispatcherTimer _timer;

        public MiniPlayerControl()
        {
            this.InitializeComponent();
            Loaded   += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Player.TrackChanged += OnTrackChanged;
            App.Player.StateChanged += OnStateChanged;

            RefreshFromPlayer();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            App.Player.TrackChanged -= OnTrackChanged;
            App.Player.StateChanged -= OnStateChanged;
            _timer?.Stop();
            _timer = null;
        }

        private void RefreshFromPlayer()
        {
            var song = App.Player.CurrentSong;
            if (song == null) return;
            TitleLabel.Text  = song.Title;
            ArtistLabel.Text = song.ArtistName;
            UpdateCoverArt(song.CoverArtId);
            UpdatePlayPause();
            Root.Visibility = Visibility.Visible;
        }

        private async void OnTrackChanged(object sender, Song song)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TitleLabel.Text  = song.Title;
                ArtistLabel.Text = song.ArtistName;
                UpdateCoverArt(song.CoverArtId);
                UpdatePlayPause();
                Root.Visibility = Visibility.Visible;
            });
        }

        private async void OnStateChanged(object sender, MediaPlaybackState state)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdatePlayPause());
        }

        private void OnTimerTick(object sender, object e)
        {
            var dur = App.Player.Duration.TotalSeconds;
            if (dur <= 0) return;
            var ratio = App.Player.Position.TotalSeconds / dur;
            PositionFill.Width = Root.ActualWidth * Math.Max(0, Math.Min(1, ratio));
        }

        private void UpdatePlayPause()
        {
            PlayPauseBtn.Content = App.Player.IsPlaying ? "" : "";
        }

        private void UpdateCoverArt(string coverArtId)
        {
            ThumbImage.Source = string.IsNullOrEmpty(coverArtId)
                ? null
                : (Windows.UI.Xaml.Media.ImageSource)new BitmapImage(
                    new Uri(App.Subsonic.GetCoverArtUrl(coverArtId, 80)));
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
            => App.Player.TogglePlayPause();

        public void UpdateVisibility(bool onNowPlayingPage)
        {
            if (onNowPlayingPage)
                Root.Visibility = Visibility.Collapsed;
            else if (App.Player.CurrentSong != null)
                Root.Visibility = Visibility.Visible;
        }

        private void OnRootTapped(object sender, TappedRoutedEventArgs e)
            => App.Navigation.NavigateToNowPlaying();
    }
}
