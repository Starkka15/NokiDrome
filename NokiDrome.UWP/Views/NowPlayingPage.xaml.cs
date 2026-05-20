using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using NokiDrome.UWP.ViewModels;

namespace NokiDrome.UWP.Views
{
    public sealed partial class NowPlayingPage : Page
    {
        private NowPlayingViewModel _vm;
        private DispatcherTimer _timer;

        public NowPlayingPage() { this.InitializeComponent(); }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _vm = new NowPlayingViewModel();
            _vm.Initialize(Dispatcher);
            _vm.PropertyChanged += OnVmPropertyChanged;

            ScrubSlider.AddHandler(PointerPressedEvent,
                new PointerEventHandler(ScrubSlider_PointerPressed), true);
            ScrubSlider.AddHandler(PointerReleasedEvent,
                new PointerEventHandler(ScrubSlider_PointerReleased), true);
            ScrubSlider.AddHandler(PointerCaptureLostEvent,
                new PointerEventHandler(ScrubSlider_PointerCaptureLost), true);

            SyncAll();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, a) => _vm.PollPosition();
            _timer.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _timer?.Stop();
            _timer = null;
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.Cleanup();
            _vm = null;
        }

        private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(NowPlayingViewModel.Title):
                    TitleLabel.Text = _vm.Title; break;
                case nameof(NowPlayingViewModel.ArtistName):
                    ArtistLabel.Text = _vm.ArtistName; break;
                case nameof(NowPlayingViewModel.AlbumName):
                    AlbumLabel.Text = _vm.AlbumName; break;
                case nameof(NowPlayingViewModel.CoverArtUrl):
                    UpdateCoverArt(); break;
                case nameof(NowPlayingViewModel.IsPlaying):
                    UpdatePlayPause(); break;
                case nameof(NowPlayingViewModel.IsShuffled):
                    UpdateShuffleState(); break;
                case nameof(NowPlayingViewModel.IsStarred):
                    UpdateStarState(); break;
                case nameof(NowPlayingViewModel.RepeatGlyph):
                    UpdateRepeatState(); break;
                case nameof(NowPlayingViewModel.PositionSeconds):
                    PositionLabel.Text = _vm.PositionText;
                    if (!_vm.IsSeeking)
                        ScrubSlider.Value = _vm.PositionSeconds;
                    break;
                case nameof(NowPlayingViewModel.DurationSeconds):
                    DurationLabel.Text   = _vm.DurationText;
                    ScrubSlider.Maximum  = _vm.DurationSeconds;
                    break;
            }
        }

        private void SyncAll()
        {
            TitleLabel.Text    = _vm.Title;
            ArtistLabel.Text   = _vm.ArtistName;
            AlbumLabel.Text    = _vm.AlbumName;
            PositionLabel.Text = _vm.PositionText;
            DurationLabel.Text = _vm.DurationText;
            ScrubSlider.Maximum = _vm.DurationSeconds;
            ScrubSlider.Value   = _vm.PositionSeconds;
            UpdateCoverArt();
            UpdatePlayPause();
            UpdateShuffleState();
            UpdateStarState();
            UpdateRepeatState();
        }

        private void UpdateCoverArt()
        {
            CoverArtImage.Source = string.IsNullOrEmpty(_vm.CoverArtUrl)
                ? null
                : (ImageSource)new BitmapImage(new Uri(_vm.CoverArtUrl));
        }

        private void UpdatePlayPause()
        {
            PlayPauseBtn.Content = _vm.IsPlaying ? "" : "";
        }

        private void UpdateShuffleState()
        {
            ShuffleBtn.Foreground = _vm.IsShuffled
                ? (Brush)Application.Current.Resources["AccentBrush"]
                : (Brush)Application.Current.Resources["TextDimBrush"];
        }

        private void UpdateStarState()
        {
            StarBtn.Content   = _vm.IsStarred ? "" : "";
            StarBtn.Foreground = _vm.IsStarred
                ? (Brush)Application.Current.Resources["AccentBrush"]
                : (Brush)Application.Current.Resources["TextDimBrush"];
        }

        private void UpdateRepeatState()
        {
            RepeatBtn.Content   = _vm.RepeatGlyph;
            RepeatBtn.Foreground = _vm.RepeatActive
                ? (Brush)Application.Current.Resources["AccentBrush"]
                : (Brush)Application.Current.Resources["TextDimBrush"];
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e) => _vm.TogglePlayPause();
        private void OnPrevClick(object sender, RoutedEventArgs e)      => _vm.Prev();
        private void OnNextClick(object sender, RoutedEventArgs e)      => _vm.Next();
        private void OnShuffleClick(object sender, RoutedEventArgs e)   => _vm.ToggleShuffle();
        private void OnStarClick(object sender, RoutedEventArgs e)      => _vm.ToggleStar();
        private void OnRepeatClick(object sender, RoutedEventArgs e)    => _vm.CycleRepeat();

        private void ScrubSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _vm.IsSeeking = true;
        }

        private void ScrubSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _vm.Seek(ScrubSlider.Value);
            _vm.IsSeeking = false;
        }

        private void ScrubSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (!_vm.IsSeeking) return;
            _vm.Seek(ScrubSlider.Value);
            _vm.IsSeeking = false;
        }
    }
}
