using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.Services;

namespace NokiDrome.UWP
{
    sealed partial class App : Application
    {
        public static NavigationService Navigation { get; private set; }
        public static SettingsService   Settings   { get; private set; }
        public static PlayerService     Player     { get; private set; }
        public static SubsonicClient    Subsonic   { get; private set; }
        public static OfflineService    Offline    { get; private set; }

        private static Exception _startupException;

        public App()
        {
            this.UnhandledException += OnUnhandledException;
            try
            {
                this.InitializeComponent();
                this.Suspending += OnSuspending;

                Settings  = new SettingsService();
                Subsonic  = new SubsonicClient(Settings);
                Offline   = new OfflineService(Settings, Subsonic);
                Player    = new PlayerService();
                Navigation = new NavigationService();
                // Load the offline index in the background so IsOffline() is ready
                // by the time playback starts.
                var _ = Offline.InitAsync();
            }
            catch (Exception ex)
            {
                _startupException = ex;
            }
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var dialog = new Windows.UI.Popups.MessageDialog(
                "An unexpected error occurred. Please restart the app.", "Error");
            await dialog.ShowAsync();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (_startupException != null)
            {
                Window.Current.Content = new Frame();
                Window.Current.Activate();
                var _ = new Windows.UI.Popups.MessageDialog(
                    "Failed to start. Please reinstall the app.", "Startup Error").ShowAsync();
                return;
            }

            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                    rootFrame.Navigate(typeof(Views.ShellPage));
                Window.Current.Activate();
            }

            // ContentFrame is set by ShellPage.OnNavigatedTo
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
            => throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // Don't pause — backgroundMediaPlayback capability keeps audio running
            e.SuspendingOperation.GetDeferral().Complete();
        }
    }
}
