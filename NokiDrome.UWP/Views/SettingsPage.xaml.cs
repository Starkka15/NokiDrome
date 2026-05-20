using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.ViewModels;

namespace NokiDrome.UWP.Views
{
    public sealed partial class SettingsPage : Page
    {
        private readonly SettingsViewModel _vm = new SettingsViewModel();

        public SettingsPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _vm.Load();
            UrlBox.Text      = _vm.Url;
            UsernameBox.Text = _vm.Username;
            PasswordBox.Password = _vm.Password;
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
