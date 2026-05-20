using System;
using Windows.UI.Xaml.Controls;
using NokiDrome.UWP.Views;

namespace NokiDrome.UWP.Services
{
    public class NavigationService
    {
        private Frame _frame;

        public void SetFrame(Frame frame) => _frame = frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;
        public void GoBack()  => _frame?.GoBack();

        public void NavigateTo(Type pageType, object parameter = null)
            => _frame?.Navigate(pageType, parameter);

        public void NavigateToShell()           => NavigateTo(typeof(ShellPage));
        public void NavigateToLibrary()         => NavigateTo(typeof(LibraryPage));
        public void NavigateToNowPlaying()
        {
            if (_frame?.Content is NowPlayingPage) return;
            NavigateTo(typeof(NowPlayingPage));
        }
        public void NavigateToSettings()        => NavigateTo(typeof(SettingsPage));
        public void NavigateToSearch()          => NavigateTo(typeof(SearchPage));
        public void NavigateToAlbum(string id)  => NavigateTo(typeof(AlbumDetailPage), id);
        public void NavigateToArtist(string id) => NavigateTo(typeof(ArtistDetailPage), id);
        public void NavigateToGenre(string name) => NavigateTo(typeof(GenreDetailPage), name);
        public void NavigateToPlaylist(string id) => NavigateTo(typeof(PlaylistDetailPage), id);
    }
}
