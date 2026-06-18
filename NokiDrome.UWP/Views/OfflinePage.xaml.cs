using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Models;

namespace NokiDrome.UWP.Views
{
    public sealed partial class OfflinePage : Page
    {
        private List<Song> _songs = new List<Song>();

        public OfflinePage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Refresh();
        }

        private void Refresh()
        {
            _songs = App.Offline.GetOfflineSongs().ToList();
            SongsList.ItemsSource = _songs;
            bool empty = _songs.Count == 0;
            EmptyLabel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            SongsList.Visibility  = empty ? Visibility.Collapsed : Visibility.Visible;
            ShuffleBtn.IsEnabled  = !empty;
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

        private void OnShuffleClick(object sender, RoutedEventArgs e)
        {
            if (_songs.Count == 0) return;
            App.Player.ShuffleAndPlay(_songs);
            App.Navigation.NavigateToNowPlaying();
        }

        private async void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string songId)
            {
                await App.Offline.RemoveAsync(songId);
                Refresh();
            }
        }
    }
}
