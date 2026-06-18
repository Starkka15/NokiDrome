using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Services;
using NokiDrome.UWP.ViewModels;

namespace NokiDrome.UWP.Views
{
    public sealed partial class SettingsPage : Page
    {
        private readonly SettingsViewModel _vm = new SettingsViewModel();
        private bool _loadingOffline;

        public SettingsPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _vm.Load();
            UrlBox.Text      = _vm.Url;
            UsernameBox.Text = _vm.Username;
            PasswordBox.Password = _vm.Password;
            LoadOfflineSettings();
        }

        private void LoadOfflineSettings()
        {
            _loadingOffline = true;   // suppress Checked/Toggled handlers during init
            var loc = App.Settings.OfflineStorageLocation;
            StorageInternalRadio.IsChecked = loc == OfflineStorage.Internal;
            StorageSdRadio.IsChecked       = loc == OfflineStorage.SdCard;
            AutoCacheToggle.IsOn           = App.Settings.AutoCache;
            OfflineCountLabel.Text         = App.Offline.Count + " tracks downloaded";
            _loadingOffline = false;
        }

        private void OnStorageChanged(object sender, RoutedEventArgs e)
        {
            if (_loadingOffline) return;
            App.Settings.OfflineStorageLocation =
                StorageSdRadio.IsChecked == true ? OfflineStorage.SdCard : OfflineStorage.Internal;
        }

        private void OnAutoCacheToggled(object sender, RoutedEventArgs e)
        {
            if (_loadingOffline) return;
            App.Settings.AutoCache = AutoCacheToggle.IsOn;
        }

        private async void OnClearDownloadsClick(object sender, RoutedEventArgs e)
        {
            var confirm = new ContentDialog
            {
                Title              = "Clear downloads",
                Content            = "Remove all " + App.Offline.Count + " downloaded tracks?",
                PrimaryButtonText  = "Clear",
                CloseButtonText    = "Cancel"
            };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

            ClearDownloadsBtn.IsEnabled = false;
            await App.Offline.ClearAllAsync();
            OfflineCountLabel.Text      = App.Offline.Count + " tracks downloaded";
            ClearDownloadsBtn.IsEnabled = true;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            _vm.Url      = UrlBox.Text;
            _vm.Username = UsernameBox.Text;
            _vm.Password = PasswordBox.Password;
            _vm.Save();
            TestResultLabel.Text     = "Saved.";
            TestResultLabel.Foreground = (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"];
        }

        private async void OnTestClick(object sender, RoutedEventArgs e)
        {
            _vm.Url      = UrlBox.Text;
            _vm.Username = UsernameBox.Text;
            _vm.Password = PasswordBox.Password;

            TestBtn.IsEnabled = false;
            TestResultLabel.Text = "";

            await _vm.TestConnectionAsync();

            TestResultLabel.Text = _vm.TestResult;
            var ok = _vm.TestResult.StartsWith("Connected");
            TestResultLabel.Foreground = ok
                ? (Windows.UI.Xaml.Media.Brush)Application.Current.Resources["AccentBrush"]
                : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 80, 80));

            TestBtn.IsEnabled = true;
        }
    }
}
