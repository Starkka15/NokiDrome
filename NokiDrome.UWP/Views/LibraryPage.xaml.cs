using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;
using NokiDrome.UWP.ViewModels;

namespace NokiDrome.UWP.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly LibraryViewModel _vm = new LibraryViewModel();
        private readonly bool[] _loaded = new bool[6];
        private List<Song> _currentSongs = new List<Song>();
        private List<Song> _starredSongs = new List<Song>();

        public LibraryPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!_loaded[0]) LoadAlbums();
        }

        private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (PivotMain.SelectedIndex)
            {
                case 0: if (!_loaded[0]) LoadAlbums();    break;
                case 1: if (!_loaded[1]) LoadArtists();   break;
                case 2: if (!_loaded[2]) LoadSongs();     break;
                case 3: if (!_loaded[3]) LoadGenres();    break;
                case 4: if (!_loaded[4]) LoadPlaylists(); break;
                case 5: if (!_loaded[5]) LoadStarred();   break;
            }
        }

        private async void LoadAlbums()
        {
            AlbumsProgress.IsActive = true;
            var albums = await _vm.GetAlbumsAsync();
            AlbumsList.ItemsSource = albums;
            AlbumsProgress.IsActive = false;
            _loaded[0] = true;
        }

        private async void LoadArtists()
        {
            ArtistsProgress.IsActive = true;
            var artists = await _vm.GetArtistsAsync();
            ArtistsList.ItemsSource = artists;
            ArtistsProgress.IsActive = false;
            _loaded[1] = true;
        }

        private async void LoadSongs()
        {
            SongsProgress.IsActive = true;
            ShuffleAllBtn.IsEnabled = false;
            _currentSongs = await _vm.GetRandomSongsAsync();
            SongsList.ItemsSource = _currentSongs;
            ShuffleAllBtn.IsEnabled = _currentSongs.Count > 0;
            SongsProgress.IsActive = false;
            _loaded[2] = true;
        }

        private async void LoadGenres()
        {
            GenresProgress.IsActive = true;
            var genres = await _vm.GetGenresAsync();
            GenresList.ItemsSource = genres;
            GenresProgress.IsActive = false;
            _loaded[3] = true;
        }

        private async void LoadPlaylists()
        {
            PlaylistsProgress.IsActive = true;
            var lists = await _vm.GetPlaylistsAsync();
            PlaylistsList.ItemsSource = lists;
            PlaylistsProgress.IsActive = false;
            _loaded[4] = true;
        }

        private async void LoadStarred()
        {
            StarredProgress.IsActive = true;
            ShuffleStarredBtn.IsEnabled = false;
            _starredSongs = await _vm.GetStarredSongsAsync();
            StarredList.ItemsSource = _starredSongs;
            ShuffleStarredBtn.IsEnabled = _starredSongs.Count > 0;
            StarredProgress.IsActive = false;
            _loaded[5] = true;
        }

        // ── Item click handlers ───────────────────────────────────────────────

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

        private void OnSongClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Song song)
                _vm.PlaySong(song, _currentSongs);
        }

        private void OnGenreClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Genre genre)
                Frame.Navigate(typeof(GenreDetailPage), genre.Name);
        }

        private void OnPlaylistClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Playlist playlist)
                Frame.Navigate(typeof(PlaylistDetailPage), playlist.Id);
        }

        private void OnStarredSongClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Song song)
                _vm.PlaySong(song, _starredSongs);
        }

        private void OnShuffleAllClick(object sender, RoutedEventArgs e)
        {
            if (_currentSongs.Count > 0)
                _vm.ShuffleAll(_currentSongs);
        }

        private void OnShuffleStarredClick(object sender, RoutedEventArgs e)
        {
            if (_starredSongs.Count > 0)
                _vm.ShuffleAll(_starredSongs);
        }
    }
}
