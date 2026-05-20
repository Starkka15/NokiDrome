using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Views
{
    public sealed partial class ArtistDetailPage : Page
    {
        public ArtistDetailPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string artistId)
                LoadArtist(artistId);
        }

        private async void LoadArtist(string artistId)
        {
            LoadingRing.IsActive = true;
            var result = await App.Subsonic.GetArtistAsync(artistId);
            if (result?.Artist != null)
                HeaderTitle.Text = result.Artist.Name;
            AlbumsList.ItemsSource = result?.Albums;
            LoadingRing.IsActive = false;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }

        private void OnAlbumClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album album)
                Frame.Navigate(typeof(AlbumDetailPage), album.Id);
        }
    }
}
