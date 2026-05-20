using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Views
{
    public sealed partial class SearchPage : Page
    {
        private List<Song>   _songs   = new List<Song>();
        private List<Album>  _albums  = new List<Album>();
        private List<Artist> _artists = new List<Artist>();
        private bool _searching;

        public SearchPage() { this.InitializeComponent(); }

        private void OnQueryKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) DoSearch();
        }

        private void OnSearchClick(object sender, RoutedEventArgs e) => DoSearch();

        private async void DoSearch()
        {
            var query = QueryBox.Text?.Trim() ?? "";
            if (query.Length == 0 || _searching) return;

            _searching = true;
            SearchBtn.IsEnabled = false;
            SearchProgress.IsActive = true;

            var songsTask   = App.Subsonic.SearchSongsAsync(query,   20);
            var albumsTask  = App.Subsonic.SearchAlbumsAsync(query,  10);
            var artistsTask = App.Subsonic.SearchArtistsAsync(query, 10);

            await Task.WhenAll(songsTask, albumsTask, artistsTask);

            _songs   = songsTask.Result;
            _albums  = albumsTask.Result;
            _artists = artistsTask.Result;

            SongsList.ItemsSource   = _songs;
            AlbumsList.ItemsSource  = _albums;
            ArtistsList.ItemsSource = _artists;

            SongsPivotItem.Header   = _songs.Count   > 0 ? $"Songs ({_songs.Count})"   : "Songs";
            AlbumsPivotItem.Header  = _albums.Count  > 0 ? $"Albums ({_albums.Count})" : "Albums";
            ArtistsPivotItem.Header = _artists.Count > 0 ? $"Artists ({_artists.Count})" : "Artists";

            SearchProgress.IsActive = false;
            SearchBtn.IsEnabled = true;
            _searching = false;
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

        private void OnAlbumClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album album)
                Frame.Navigate(typeof(AlbumDetailPage), album.Id);
        }

        private void OnArtistClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Artist artist)
                Frame.Navigate(typeof(ArtistDetailPage), artist.Id);
        }
    }
}
