using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FontAwesome6;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.Controls;

public partial class SkinInspectControl : UserControl
{
    private SkinInspectDetail _currentDetail;
    private bool _isPlaying = false;
    private bool _isMuted = false;
    private bool _isFullscreen = false;
    private bool _isDraggingSlider = false;
    private double _totalDurationSeconds = 0;
    public event Action Closed;

    private readonly DispatcherTimer _playbackTimer;
    private readonly DispatcherTimer _flashHideTimer;

    public SkinInspectControl()
    {
        InitializeComponent();
        Focusable = true;

        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _playbackTimer.Tick += PlaybackTimer_Tick;

        _flashHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _flashHideTimer.Tick += (s, e) =>
        {
            _flashHideTimer.Stop();
            SeekFlashBadge.Visibility = Visibility.Collapsed;
        };

        ApplyLocalization();

        Loaded += (s, e) =>
        {
            ApplyLocalization();
            var win = Window.GetWindow(this);
            if (win != null)
            {
                win.PreviewKeyDown += Win_PreviewKeyDown;
            }
        };
    }

    public void ApplyLocalization()
    {
        try
        {
            if (InspectCloseBtn != null) InspectCloseBtn.ToolTip = L10n.Get("InspectCloseToolTip");
            if (VideoLoadingText != null) VideoLoadingText.Text = L10n.Get("InspectVideoLoading");
            if (SeekBackwardBtn != null) SeekBackwardBtn.ToolTip = L10n.Get("InspectSeekBackwardToolTip");
            if (SeekForwardBtn != null) SeekForwardBtn.ToolTip = L10n.Get("InspectSeekForwardToolTip");
            if (PlayPauseBtn != null) PlayPauseBtn.ToolTip = L10n.Get("InspectPlayPauseToolTip");
            if (ReplayBtn != null) ReplayBtn.ToolTip = L10n.Get("InspectReplayToolTip");
            if (ShortcutsHintText != null) ShortcutsHintText.Text = L10n.Get("InspectShortcuts");
            if (MuteBtn != null) MuteBtn.ToolTip = L10n.Get("InspectMuteToolTip");
            if (FullscreenBtn != null) FullscreenBtn.ToolTip = _isFullscreen ? L10n.Get("InspectExitFullscreenToolTip") : L10n.Get("InspectFullscreenToolTip");
            if (LevelsSectionTitle != null) LevelsSectionTitle.Text = L10n.Get("InspectLevels");
            if (ChromasSectionTitle != null) ChromasSectionTitle.Text = L10n.Get("InspectVariants");
            if (UnupgradableText != null) UnupgradableText.Text = L10n.Get("InspectStandardModel");
        }
        catch { }
    }

    private void Win_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Visibility != Visibility.Visible) return;

        if (e.Key == Key.Escape)
        {
            if (_isFullscreen)
            {
                ToggleFullscreen();
                e.Handled = true;
                return;
            }
            CloseInspect();
            e.Handled = true;
            return;
        }

        // F or F11 for Fullscreen
        if (e.Key is Key.F or Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        // Left / Right arrows: 5s Seek Backward / Forward
        if (e.Key == Key.Left)
        {
            SeekRelative(-5);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right)
        {
            SeekRelative(+5);
            e.Handled = true;
            return;
        }

        // Space: Play / Pause
        if (e.Key == Key.Space)
        {
            PlayPauseBtn_Click(this, null);
            e.Handled = true;
            return;
        }

        // M: Mute toggle
        if (e.Key == Key.M)
        {
            MuteBtn_Click(this, null);
            e.Handled = true;
            return;
        }

        // R: Replay
        if (e.Key == Key.R)
        {
            ReplayBtn_Click(this, null);
            e.Handled = true;
            return;
        }

        // Up / Down arrows: Cycle Chromas / Variants
        if (e.Key == Key.Up && _currentDetail?.Chromas?.Count > 1)
        {
            var curIdx = _currentDetail.Chromas.FindIndex(c => c.IsSelected);
            if (curIdx > 0)
            {
                SelectChroma(_currentDetail.Chromas[curIdx - 1]);
                e.Handled = true;
                return;
            }
        }
        else if (e.Key == Key.Down && _currentDetail?.Chromas?.Count > 1)
        {
            var curIdx = _currentDetail.Chromas.FindIndex(c => c.IsSelected);
            if (curIdx >= 0 && curIdx + 1 < _currentDetail.Chromas.Count)
            {
                SelectChroma(_currentDetail.Chromas[curIdx + 1]);
                e.Handled = true;
                return;
            }
        }

        // Keys 1-9 for Levels
        if (e.Key is >= Key.D1 and <= Key.D9)
        {
            int lvlNum = e.Key - Key.D0;
            var targetLvl = _currentDetail?.Levels?.FirstOrDefault(l => l.LevelNumber == lvlNum);
            if (targetLvl != null)
            {
                SelectLevel(targetLvl);
                e.Handled = true;
                return;
            }
        }
        else if (e.Key is >= Key.NumPad1 and <= Key.NumPad9)
        {
            int lvlNum = e.Key - Key.NumPad0;
            var targetLvl = _currentDetail?.Levels?.FirstOrDefault(l => l.LevelNumber == lvlNum);
            if (targetLvl != null)
            {
                SelectLevel(targetLvl);
                e.Handled = true;
                return;
            }
        }
    }

    public async void ShowSkin(DailyStoreOffer offer)
    {
        if (offer == null) return;

        // Reset state
        StopVideo();
        if (_isFullscreen) ToggleFullscreen(); // start in standard window
        Visibility = Visibility.Visible;
        IsHitTestVisible = true;

        // Populate initial header
        SkinNameTxt.Text = offer.Name;
        WeaponTypeTxt.Text = offer.WeaponType?.ToUpperInvariant() ?? (L10n.IsEnglish ? "WEAPON" : "SİLAH");
        PriceVpTxt.Text = offer.VpCostFormatted;
        PriceTlTxt.Text = $"({offer.TlCostFormatted})";

        if (offer.HasBundleDiscount)
        {
            BundleDiscountPill.Visibility = Visibility.Visible;
            BundleDiscountTxt.Text = string.Format(L10n.Get("InspectInBundle"), offer.BundleDiscountedCostFormatted, offer.DiscountPercentText);
        }
        else
        {
            BundleDiscountPill.Visibility = Visibility.Collapsed;
        }

        TierNameTxt.Text = string.IsNullOrEmpty(offer.TierDisplayName) ? offer.TierDevName?.ToUpperInvariant() ?? (L10n.IsEnglish ? "STANDARD" : "STANDART") : offer.TierDisplayName;

        TierBadgeBorder.BorderBrush = offer.TierBorderBrush;
        TierBadgeBorder.Background = offer.TierBackgroundBrush;
        TierNameTxt.Foreground = offer.TierBorderBrush;

        try
        {
            if (!string.IsNullOrEmpty(offer.TierIconUrl))
                TierIconImage.Source = new BitmapImage(new Uri(offer.TierIconUrl));
            else
                TierIconImage.Source = null;
        }
        catch { }

        try
        {
            VpIconImg.Source = new BitmapImage(new Uri(StoreHelper.VpIconUrl));
        }
        catch { }

        if (offer.Image != null)
        {
            StaticImage.Source = new BitmapImage(offer.Image);
            StaticImage.Visibility = Visibility.Visible;
        }

        CurrentPlayingLabel.Text = L10n.Get("InspectLoading");
        VideoLoadingOverlay.Visibility = Visibility.Visible;

        // Fetch complete inspect details
        var detail = await SkinInspectHelper.GetSkinInspectDetailAsync(offer).ConfigureAwait(true);
        _currentDetail = detail;

        // Bind Levels & Chromas
        LevelsList.ItemsSource = null;
        LevelsList.ItemsSource = detail.Levels;

        ChromasList.ItemsSource = null;
        ChromasList.ItemsSource = detail.Chromas;

        if (!detail.HasAnyVideo)
        {
            // 1-Level / Unupgradable weapon without videos
            StopVideo();
            VideoPlayer.Visibility = Visibility.Collapsed;
            VideoLoadingOverlay.Visibility = Visibility.Collapsed;
            VideoControlsBorder.Visibility = Visibility.Collapsed;
            StaticImage.Visibility = Visibility.Visible;
            LevelsSectionBorder.Visibility = Visibility.Collapsed;
            ChromasSectionBorder.Visibility = detail.HasChromas ? Visibility.Visible : Visibility.Collapsed;
            UnupgradableInfoBorder.Visibility = Visibility.Visible;
            CurrentPlayingLabel.Text = L10n.Get("InspectStandardModel");
        }
        else
        {
            UnupgradableInfoBorder.Visibility = Visibility.Collapsed;
            LevelsSectionBorder.Visibility = detail.HasLevels ? Visibility.Visible : Visibility.Collapsed;
            ChromasSectionBorder.Visibility = detail.HasChromas ? Visibility.Visible : Visibility.Collapsed;
            VideoControlsBorder.Visibility = Visibility.Visible;

            // Play default video (Level 4 Finisher or first available)
            var targetLevel = detail.Levels.LastOrDefault(l => l.HasVideo) ?? detail.Levels.FirstOrDefault(l => l.HasVideo);
            if (targetLevel != null)
            {
                SelectLevel(targetLevel);
            }
            else
            {
                VideoLoadingOverlay.Visibility = Visibility.Collapsed;
                CurrentPlayingLabel.Text = L10n.Get("InspectStaticImage");
            }
        }
    }

    public void SelectLevel(SkinLevelModel lvl)
    {
        if (lvl == null || _currentDetail == null) return;

        foreach (var l in _currentDetail.Levels)
            l.IsSelected = (l == lvl);
        LevelsList.Items.Refresh();

        CurrentPlayingLabel.Text = lvl.LevelTypeName;

        if (lvl.HasVideo)
        {
            VideoPlayer.Visibility = Visibility.Visible;
            StaticImage.Visibility = Visibility.Collapsed;
            VideoControlsBorder.Visibility = Visibility.Visible;
            PlayVideo(lvl.VideoUrl);
        }
        else
        {
            StopVideo();
            VideoPlayer.Visibility = Visibility.Collapsed;
            StaticImage.Visibility = Visibility.Visible;
            VideoControlsBorder.Visibility = Visibility.Collapsed;
            CurrentPlayingLabel.Text = string.Format(L10n.Get("InspectBaseModel"), lvl.LevelTypeName);
        }
    }

    public void SelectChroma(SkinChromaModel chr)
    {
        if (chr == null || _currentDetail == null) return;

        foreach (var c in _currentDetail.Chromas)
            c.IsSelected = (c == chr);
        ChromasList.Items.Refresh();

        CurrentPlayingLabel.Text = string.Format(L10n.Get("InspectVariantLabel"), chr.CleanStyleName);

        // Update static image to this chroma's full render
        if (!string.IsNullOrEmpty(chr.FullRenderUrl))
        {
            try
            {
                StaticImage.Source = new BitmapImage(new Uri(chr.FullRenderUrl));
            }
            catch { }
        }

        // Play chroma video if available
        if (chr.HasVideo)
        {
            VideoPlayer.Visibility = Visibility.Visible;
            StaticImage.Visibility = Visibility.Collapsed;
            VideoControlsBorder.Visibility = Visibility.Visible;
            PlayVideo(chr.VideoUrl);
        }
        else
        {
            StopVideo();
            VideoPlayer.Visibility = Visibility.Collapsed;
            StaticImage.Visibility = Visibility.Visible;
            VideoControlsBorder.Visibility = Visibility.Collapsed;
        }
    }

    public void SeekRelative(double deltaSeconds)
    {
        try
        {
            var curPos = VideoPlayer.Position;
            var maxPos = _totalDurationSeconds > 0 ? _totalDurationSeconds : 60;
            var newSeconds = Math.Clamp(curPos.TotalSeconds + deltaSeconds, 0, maxPos);
            var newPos = TimeSpan.FromSeconds(newSeconds);

            VideoPlayer.Position = newPos;
            if (!_isDraggingSlider)
                TimelineSlider.Value = newSeconds;

            UpdateTimeText(newPos, TimeSpan.FromSeconds(_totalDurationSeconds));

            // Show Seek Flash Badge
            SeekFlashIcon.Icon = deltaSeconds > 0 ? EFontAwesomeIcon.Solid_Forward : EFontAwesomeIcon.Solid_Backward;
            var unit = L10n.IsEnglish ? "s" : "sn";
            SeekFlashText.Text = deltaSeconds > 0
                ? $"+{deltaSeconds:0} {unit} ({newPos:mm\\:ss})"
                : $"{deltaSeconds:0} {unit} ({newPos:mm\\:ss})";

            SeekFlashBadge.Visibility = Visibility.Visible;
            _flashHideTimer.Stop();
            _flashHideTimer.Start();
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("SeekRelative failed: {e}", ex.Message);
        }
    }

    public void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;

        if (_isFullscreen)
        {
            // Fullscreen video layout
            HeaderRowDef.Height = new GridLength(0);
            FooterRowDef.Height = new GridLength(0);
            HeaderRowGrid.Visibility = Visibility.Collapsed;
            FooterRowGrid.Visibility = Visibility.Collapsed;

            ModalBorder.Margin = new Thickness(0);
            ModalBorder.CornerRadius = new CornerRadius(0);
            ModalBorder.MaxWidth = 10000;
            ModalBorder.MaxHeight = 10000;

            FullscreenIcon.Icon = EFontAwesomeIcon.Solid_Compress;
            FullscreenBtn.ToolTip = L10n.Get("InspectExitFullscreenToolTip");
        }
        else
        {
            // Standard modal layout
            HeaderRowDef.Height = GridLength.Auto;
            FooterRowDef.Height = GridLength.Auto;
            HeaderRowGrid.Visibility = Visibility.Visible;
            FooterRowGrid.Visibility = Visibility.Visible;

            ModalBorder.Margin = new Thickness(40, 25, 40, 25);
            ModalBorder.CornerRadius = new CornerRadius(18);
            ModalBorder.MaxWidth = 1280;
            ModalBorder.MaxHeight = 760;

            FullscreenIcon.Icon = EFontAwesomeIcon.Solid_Expand;
            FullscreenBtn.ToolTip = L10n.Get("InspectFullscreenToolTip");
        }
    }

    private void PlayVideo(string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl))
        {
            StopVideo();
            StaticImage.Visibility = Visibility.Visible;
            VideoLoadingOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            VideoLoadingOverlay.Visibility = Visibility.Visible;
            VideoPlayer.Source = new Uri(videoUrl);
            VideoPlayer.IsMuted = _isMuted;
            VideoPlayer.Play();
            _isPlaying = true;
            PlayPauseIcon.Icon = EFontAwesomeIcon.Solid_Pause;
            _playbackTimer.Start();
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("PlayVideo failed: {e}", ex.Message);
            StopVideo();
        }
    }

    private void StopVideo()
    {
        _playbackTimer.Stop();
        try
        {
            VideoPlayer.Stop();
            VideoPlayer.Source = null;
        }
        catch { }
        _isPlaying = false;
        PlayPauseIcon.Icon = EFontAwesomeIcon.Solid_Play;
        VideoLoadingOverlay.Visibility = Visibility.Collapsed;
        StaticImage.Visibility = Visibility.Visible;
        TimelineSlider.Value = 0;
        TimeText.Text = "00:00 / 00:00";
    }

    private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        VideoLoadingOverlay.Visibility = Visibility.Collapsed;
        StaticImage.Visibility = Visibility.Collapsed;

        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            var total = VideoPlayer.NaturalDuration.TimeSpan;
            _totalDurationSeconds = total.TotalSeconds;
            TimelineSlider.Maximum = _totalDurationSeconds;
            UpdateTimeText(VideoPlayer.Position, total);
        }
    }

    private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        // Continuous Loop like in-game inspect
        try
        {
            VideoPlayer.Position = TimeSpan.Zero;
            VideoPlayer.Play();
        }
        catch { }
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Constants.Log?.Warning("Video playback error: {err}", e.ErrorException?.Message);
        VideoLoadingOverlay.Visibility = Visibility.Collapsed;
        StaticImage.Visibility = Visibility.Visible;
        _playbackTimer.Stop();
    }

    private void EnsureDuration()
    {
        if (_totalDurationSeconds <= 0 && VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            _totalDurationSeconds = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            if (_totalDurationSeconds > 0)
            {
                TimelineSlider.Maximum = _totalDurationSeconds;
            }
        }
    }

    private void PlaybackTimer_Tick(object sender, EventArgs e)
    {
        EnsureDuration();
        if (!_isDraggingSlider && VideoPlayer.Source != null)
        {
            var pos = VideoPlayer.Position;
            TimelineSlider.Value = pos.TotalSeconds;
            var totalSpan = TimeSpan.FromSeconds(_totalDurationSeconds);
            UpdateTimeText(pos, totalSpan);
        }
    }

    private void UpdateTimeText(TimeSpan current, TimeSpan total)
    {
        TimeText.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
    }

    private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            VideoPlayer.Pause();
            _isPlaying = false;
            PlayPauseIcon.Icon = EFontAwesomeIcon.Solid_Play;
        }
        else
        {
            VideoPlayer.Play();
            _isPlaying = true;
            PlayPauseIcon.Icon = EFontAwesomeIcon.Solid_Pause;
        }
    }

    private void ReplayBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            VideoPlayer.Position = TimeSpan.Zero;
            VideoPlayer.Play();
            _isPlaying = true;
            PlayPauseIcon.Icon = EFontAwesomeIcon.Solid_Pause;
        }
        catch { }
    }

    private void SeekBackwardBtn_Click(object sender, RoutedEventArgs e)
    {
        SeekRelative(-5);
    }

    private void SeekForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        SeekRelative(+5);
    }

    private void MuteBtn_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        VideoPlayer.IsMuted = _isMuted;
        MuteIcon.Icon = _isMuted ? EFontAwesomeIcon.Solid_VolumeXmark : EFontAwesomeIcon.Solid_VolumeHigh;
    }

    private void FullscreenBtn_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    private void VideoArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void TimelineSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = true;
        TimelineSlider.CaptureMouse();
        SeekToMousePosition(e);
        e.Handled = true;
    }

    private void TimelineSlider_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingSlider && e.LeftButton == MouseButtonState.Pressed)
        {
            SeekToMousePosition(e);
            e.Handled = true;
        }
    }

    private void TimelineSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingSlider)
        {
            _isDraggingSlider = false;
            TimelineSlider.ReleaseMouseCapture();
            SeekToMousePosition(e);
            e.Handled = true;
        }
    }

    private void SeekToMousePosition(MouseEventArgs e)
    {
        EnsureDuration();
        if (TimelineSlider.ActualWidth <= 0 || _totalDurationSeconds <= 0) return;
        var mousePos = e.GetPosition(TimelineSlider);
        var ratio = Math.Clamp(mousePos.X / TimelineSlider.ActualWidth, 0.0, 1.0);
        var targetSeconds = ratio * _totalDurationSeconds;
        TimelineSlider.Value = targetSeconds;
        VideoPlayer.Position = TimeSpan.FromSeconds(targetSeconds);
        UpdateTimeText(TimeSpan.FromSeconds(targetSeconds), TimeSpan.FromSeconds(_totalDurationSeconds));
    }

    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isDraggingSlider)
        {
            var newPos = TimeSpan.FromSeconds(e.NewValue);
            UpdateTimeText(newPos, TimeSpan.FromSeconds(_totalDurationSeconds));
        }
    }

    private void LevelBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SkinLevelModel lvl)
        {
            SelectLevel(lvl);
        }
    }

    private void ChromaBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SkinChromaModel chr)
        {
            SelectChroma(chr);
        }
    }

    private void ModalBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Isolate clicks inside modal so they do not hit outer backdrop
    }

    public void CloseInspect()
    {
        if (_isFullscreen) ToggleFullscreen();
        StopVideo();
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
        Closed?.Invoke();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseInspect();
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        CloseInspect();
    }
}

