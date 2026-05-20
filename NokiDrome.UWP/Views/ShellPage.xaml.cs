using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NokiDrome.UWP.Views
{
    public sealed partial class ShellPage : Page
    {
        private enum Tab { Library, NowPlaying, Search, Settings }
        private Tab _activeTab = Tab.Library;

        public ShellPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            App.Navigation.SetFrame(ContentFrame);
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            ContentFrame.Navigated += OnContentFrameNavigated;

            var server = App.Settings.GetServer();
            if (string.IsNullOrEmpty(server.Url))
                NavigateTo(Tab.Settings);
            else
                NavigateTo(Tab.Library);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ContentFrame.Navigated -= OnContentFrameNavigated;
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
        }

        private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            MiniPlayer.UpdateVisibility(ContentFrame.Content is NowPlayingPage);
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (ContentFrame.CanGoBack)
            {
                e.Handled = true;
                ContentFrame.GoBack();
            }
        }

        private void OnNavLibrary(object sender, RoutedEventArgs e)    => NavigateTo(Tab.Library);
        private void OnNavNowPlaying(object sender, RoutedEventArgs e) => NavigateTo(Tab.NowPlaying);
        private void OnNavSearch(object sender, RoutedEventArgs e)     => NavigateTo(Tab.Search);
        private void OnNavSettings(object sender, RoutedEventArgs e)   => NavigateTo(Tab.Settings);

        private void NavigateTo(Tab tab)
        {
            _activeTab = tab;
            UpdateNavHighlight();
            switch (tab)
            {
                case Tab.Library:    ContentFrame.Navigate(typeof(LibraryPage));    break;
                case Tab.NowPlaying: ContentFrame.Navigate(typeof(NowPlayingPage)); break;
                case Tab.Search:     ContentFrame.Navigate(typeof(SearchPage));     break;
                case Tab.Settings:   ContentFrame.Navigate(typeof(SettingsPage));   break;
            }
        }

        private void UpdateNavHighlight()
        {
            var accent    = (Windows.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["AccentBrush"];
            var secondary = (Windows.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["TextSecondaryBrush"];

            NavLibraryIcon.Foreground    = _activeTab == Tab.Library    ? accent : secondary;
            NavLibraryLabel.Foreground   = _activeTab == Tab.Library    ? accent : secondary;
            NavNowPlayingIcon.Foreground  = _activeTab == Tab.NowPlaying ? accent : secondary;
            NavNowPlayingLabel.Foreground = _activeTab == Tab.NowPlaying ? accent : secondary;
            NavSearchIcon.Foreground     = _activeTab == Tab.Search     ? accent : secondary;
            NavSearchLabel.Foreground    = _activeTab == Tab.Search     ? accent : secondary;
            NavSettingsIcon.Foreground   = _activeTab == Tab.Settings   ? accent : secondary;
            NavSettingsLabel.Foreground  = _activeTab == Tab.Settings   ? accent : secondary;
        }
    }
}
