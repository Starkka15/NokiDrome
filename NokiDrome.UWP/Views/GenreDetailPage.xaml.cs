using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Views
{
    public sealed partial class GenreDetailPage : Page
    {
        private List<Song> _songs = new List<Song>();

        public GenreDetailPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string genre)
                LoadGenre(genre);
        }

        private async void LoadGenre(string genre)
        {
            LoadingRing.IsActive = true;
            HeaderTitle.Text = genre;
            _songs = await App.Subsonic.GetSongsByGenreAsync(genre, 100);
            SongsList.ItemsSource = _songs;
            bool has = _songs.Count > 0;
            PlayAllBtn.IsEnabled = has;
            ShuffleBtn.IsEnabled = has;
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
