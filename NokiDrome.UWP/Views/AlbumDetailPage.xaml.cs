using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Views
{
    public sealed partial class AlbumDetailPage : Page
    {
        private List<Song> _songs = new List<Song>();

        public AlbumDetailPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string albumId)
                LoadAlbum(albumId);
        }

        private async void LoadAlbum(string albumId)
        {
            LoadingRing.IsActive = true;

            var result = await App.Subsonic.GetAlbumAsync(albumId);

            if (result?.Album != null)
            {
                var album = result.Album;
                HeaderTitle.Text = album.Name;
                ArtistLabel.Text = album.ArtistName;

                string meta = "";
                if (album.Year > 0)    meta += album.Year.ToString();
                if (album.SongCount > 0)
                    meta += (meta.Length > 0 ? "  ·  " : "") + album.SongCount + " songs";
                MetaLabel.Text = meta;

                if (!string.IsNullOrEmpty(album.CoverArtId))
                {
                    var url = App.Subsonic.GetCoverArtUrl(album.CoverArtId, 300);
                    CoverArtImage.Source = new BitmapImage(new Uri(url));
                }
            }

            _songs = result?.Songs ?? new List<Song>();
            SongsList.ItemsSource = _songs;

            bool hasSongs = _songs.Count > 0;
            PlayAllBtn.IsEnabled = hasSongs;
            ShuffleBtn.IsEnabled = hasSongs;

            LoadingRing.IsActive = false;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }

        private void OnPlayAllClick(object sender, RoutedEventArgs e)
        {
            if (_songs.Count == 0) return;
            App.Player.SetQueue(_songs, 0);
            App.Navigation.NavigateToNowPlaying();
        }

        private void OnShuffleClick(object sender, RoutedEventArgs e)
        {
            if (_songs.Count == 0) return;
            App.Player.ShuffleAndPlay(_songs);
            App.Navigation.NavigateToNowPlaying();
        }

        private void OnSongClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Song song)
            {
                int idx = _songs.IndexOf(song);
                App.Player.SetQueue(_songs, idx < 0 ? 0 : idx);
                App.Navigation.NavigateToNowPlaying();
            }
        }
    }
}
