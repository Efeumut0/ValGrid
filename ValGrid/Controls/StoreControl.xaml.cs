using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.Controls;

public partial class StoreControl : UserControl
{
    private CancellationTokenSource _cts;
    private bool _isLoading;
    private bool _hasLoadedData;
    private bool _isRefreshing;
    private DispatcherTimer _countdownTimer;
    private int _remainingSeconds;
    private List<FeaturedBundleOffer> _bundleOffers = new();
    private int _currentBundleIndex;
    private DispatcherTimer _bundleCarouselTimer;
    private bool _isCarouselHovered;

    #region Image Memory & Disk Caching

    private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

    private static string GetLocalCachedImagePath(Uri uri)
    {
        try
        {
            var cacheDir = Path.Combine(Constants.LocalAppDataPath ?? "", "ValAPI", "imgcache");
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(uri.AbsoluteUri));
            var hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(cacheDir, $"{hashStr}.png");
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage LoadBitmapFromPath(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static async Task CacheImageToDiskAsync(Uri uri, string localPath)
    {
        if (uri == null || string.IsNullOrEmpty(localPath) || File.Exists(localPath)) return;
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var bytes = await client.GetByteArrayAsync(uri).ConfigureAwait(false);
            var tempFile = localPath + ".tmp";
            await File.WriteAllBytesAsync(tempFile, bytes).ConfigureAwait(false);
            File.Move(tempFile, localPath, true);
        }
        catch { }
    }

    public static BitmapImage GetOrCreateCachedBitmap(Uri uri)
    {
        if (uri == null) return null;
        var key = uri.AbsoluteUri;
        if (_imageCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var localPath = GetLocalCachedImagePath(uri);
            if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
            {
                var diskBmp = LoadBitmapFromPath(localPath);
                if (diskBmp != null)
                {
                    _imageCache[key] = diskBmp;
                    return diskBmp;
                }
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();

            _imageCache[key] = bmp;

            if (!string.IsNullOrEmpty(localPath) && !File.Exists(localPath))
            {
                _ = CacheImageToDiskAsync(uri, localPath);
            }

            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private void PreloadStoreImages(DailyStoreData data)
    {
        Task.Run(async () =>
        {
            try
            {
                if (data.FeaturedBundles != null)
                {
                    foreach (var b in data.FeaturedBundles)
                    {
                        var u = b.DisplayIcon2 ?? b.DisplayIcon ?? b.VerticalPromoImage;
                        if (u != null) await PreloadImageAsync(u).ConfigureAwait(false);
                        if (b.Items != null)
                        {
                            foreach (var itm in b.Items)
                            {
                                if (itm.Image != null) await PreloadImageAsync(itm.Image).ConfigureAwait(false);
                                if (itm.CardLargeArt != null) await PreloadImageAsync(itm.CardLargeArt).ConfigureAwait(false);
                                if (itm.CardWideArt != null) await PreloadImageAsync(itm.CardWideArt).ConfigureAwait(false);
                                if (itm.CardSmallArt != null) await PreloadImageAsync(itm.CardSmallArt).ConfigureAwait(false);
                            }
                        }
                    }
                }

                if (data.DailyOffers != null)
                {
                    foreach (var o in data.DailyOffers)
                    {
                        if (o.Image != null) await PreloadImageAsync(o.Image).ConfigureAwait(false);
                    }
                }

                if (data.NightMarketOffers != null)
                {
                    foreach (var o in data.NightMarketOffers)
                    {
                        if (o.Image != null) await PreloadImageAsync(o.Image).ConfigureAwait(false);
                    }
                }
            }
            catch { }
        });
    }

    private static async Task PreloadImageAsync(Uri uri)
    {
        if (uri == null) return;
        var key = uri.AbsoluteUri;
        if (_imageCache.ContainsKey(key)) return;

        try
        {
            var localPath = GetLocalCachedImagePath(uri);
            if (!string.IsNullOrEmpty(localPath))
            {
                if (!File.Exists(localPath))
                {
                    await CacheImageToDiskAsync(uri, localPath).ConfigureAwait(false);
                }

                if (File.Exists(localPath))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (!_imageCache.ContainsKey(key))
                        {
                            var bmp = LoadBitmapFromPath(localPath);
                            if (bmp != null) _imageCache[key] = bmp;
                        }
                    });
                }
            }
        }
        catch { }
    }

    #endregion

    public StoreControl()
    {
        InitializeComponent();
        ApplyLocalization();

        Loaded += (s, e) =>
        {
            ApplyLocalization();
            if (InspectOverlay != null)
            {
                InspectOverlay.Closed += () =>
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        if (StoreScroll != null)
                            StoreScroll.IsHitTestVisible = true;
                    }));
                };
            }
        };

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                if (PlayerCardOverlay != null && PlayerCardOverlay.Visibility == Visibility.Visible)
                {
                    ClosePlayerCard_Click(null, null);
                    e.Handled = true;
                    return;
                }
                if (InspectOverlay != null && InspectOverlay.Visibility == Visibility.Visible)
                {
                    InspectOverlay.CloseInspect();
                    e.Handled = true;
                    return;
                }
                if (AllWeaponsPanel != null && AllWeaponsPanel.Visibility == Visibility.Visible)
                {
                    BackToStore_Click(null, null);
                    e.Handled = true;
                    return;
                }
            }
        };
    }

    public void LoadStore(bool isRefresh = false)
    {
        _ = LoadStoreInternalAsync(isRefresh);
    }

    private async Task LoadStoreInternalAsync(bool isRefresh = false)
    {
        if (_isLoading) return;
        _isLoading = true;
        _isRefreshing = isRefresh && _hasLoadedData;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            if (_isRefreshing)
            {
                // In refresh mode with existing data, keep cards visible and spin the refresh icon
                await Dispatcher.InvokeAsync(() =>
                {
                    if (RefreshIcon != null) RefreshIcon.Spin = true;
                    if (RefreshBtn != null) RefreshBtn.IsEnabled = false;
                });
            }
            else
            {
                ShowWaitingState(L10n.Get("StoreWaitingConnecting"), L10n.Get("StoreWaitingPreparing"), showRelaunchBtn: true);
            }

            // 1. Check if Riot Client is running and connected
            var isConnected = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);

            if (!isConnected)
            {
                if (!_isRefreshing)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (RelaunchClientBtn != null) RelaunchClientBtn.Visibility = Visibility.Visible;
                    });
                }

                // Launch Riot Client and wait for user login
                var loginSuccess = await RiotClientHelper.WaitForRiotLoginAsync(status =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (WaitingMessage != null && !_isRefreshing)
                            WaitingMessage.Text = status;
                    });
                }, token).ConfigureAwait(false);

                if (!loginSuccess)
                {
                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ShowErrorState(L10n.Get("StoreErrorMessageLogin"));
                        });
                    }
                    return;
                }
            }

            // 2. Fetch Storefront and Wallet
            if (!_isRefreshing)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (WaitingTitle != null) WaitingTitle.Text = L10n.Get("StoreWaitingFetching");
                    if (WaitingMessage != null) WaitingMessage.Text = L10n.Get("StoreWaitingFetchingSub");
                    if (RelaunchClientBtn != null) RelaunchClientBtn.Visibility = Visibility.Collapsed;
                });
            }

            var storeData = await StoreHelper.GetDailyStoreAsync().ConfigureAwait(false);

            // If empty, try refreshing credentials once from local Riot Client and retry
            if (storeData.DailyOffers == null || storeData.DailyOffers.Count == 0)
            {
                try
                {
                    var hasLocal = await Checks.CheckLocalAsync().ConfigureAwait(false);
                    if (hasLocal)
                    {
                        var relog = await Login.LocalLoginAsync().ConfigureAwait(false);
                        if (relog)
                        {
                            await Login.LocalRegionAsync().ConfigureAwait(false);
                            storeData = await StoreHelper.GetDailyStoreAsync().ConfigureAwait(false);
                        }
                    }
                }
                catch { }
            }

            if (token.IsCancellationRequested) return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (storeData.DailyOffers != null && storeData.DailyOffers.Count > 0)
                {
                    DisplayStoreData(storeData);
                }
                else
                {
                    ShowErrorState(L10n.Get("StoreErrorMessageServer"));
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Cancelled by user
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("StoreControl.LoadStoreInternalAsync error: {e}", ex);
            await Dispatcher.InvokeAsync(() =>
            {
                ShowErrorState($"Mağaza yüklenirken bir hata oluştu: {ex.Message}");
            });
        }
        finally
        {
            _isLoading = false;
            _isRefreshing = false;
            await Dispatcher.InvokeAsync(() =>
            {
                if (RefreshIcon != null) RefreshIcon.Spin = false;
                if (RefreshBtn != null) RefreshBtn.IsEnabled = true;
            });
        }
    }

    private void DisplayStoreData(DailyStoreData data)
    {
        _hasLoadedData = true;
        HideAllPanels();

        if (AllWeaponsBtn != null) AllWeaponsBtn.Visibility = Visibility.Visible;
        PreloadStoreImages(data);

        // 1. FeaturedBundles Carousel
        _bundleOffers = data.FeaturedBundles ?? new List<FeaturedBundleOffer>();
        if (_bundleOffers.Count > 0)
        {
            FeaturedBundlesContainer.Visibility = Visibility.Visible;
            ShowBundleSlide(0, animate: false);
            StartBundleCarousel();
        }
        else
        {
            FeaturedBundlesContainer.Visibility = Visibility.Collapsed;
            StopBundleCarousel();
        }

        // 2. Populate Daily Offers
        DailyOffersList.ItemsSource = data.DailyOffers;

        // 3. Populate Night Market if available or archived/demo
        if (data.HasNightMarket)
        {
            NightMarketContainer.Visibility = Visibility.Visible;
            NightMarketList.ItemsSource = data.NightMarketOffers;
            if (NightMarketTitleText != null)
                NightMarketTitleText.Text = data.NightMarketTitle;
            if (NightMarketSubtitleText != null)
                NightMarketSubtitleText.Text = data.NightMarketSubtitle;
        }
        else
        {
            NightMarketContainer.Visibility = Visibility.Collapsed;
        }

        // Live Countdown Timer with Seconds
        _countdownTimer?.Stop();
        _remainingSeconds = data.RemainingDurationSeconds;

        if (_remainingSeconds > 0)
        {
            TimerText.Text = StoreHelper.FormatCountdown(_remainingSeconds);
            TimerBadge.Visibility = Visibility.Visible;

            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _countdownTimer.Stop();
                    TimerText.Text = L10n.Get("RefreshingIn") + "...";
                    LoadStore();
                }
                else
                {
                    TimerText.Text = StoreHelper.FormatCountdown(_remainingSeconds);
                }
            };
            _countdownTimer.Start();
        }
        else if (!string.IsNullOrEmpty(data.RemainingTimeFormatted) && data.RemainingTimeFormatted != "--:--:--")
        {
            TimerText.Text = data.RemainingTimeFormatted;
            TimerBadge.Visibility = Visibility.Visible;
        }
        else
        {
            TimerBadge.Visibility = Visibility.Collapsed;
        }

        // Wallet Balances
        if (data.VpBalance >= 0)
        {
            VpText.Text = data.VpBalanceFormatted;
            VpBadge.Visibility = Visibility.Visible;
        }
        else
        {
            VpBadge.Visibility = Visibility.Collapsed;
        }

        if (data.RadianiteBalance >= 0)
        {
            RpText.Text = data.RadianiteBalanceFormatted;
            RpBadge.Visibility = Visibility.Visible;
        }
        else
        {
            RpBadge.Visibility = Visibility.Collapsed;
        }

        if (data.KcBalance >= 0)
        {
            KcText.Text = data.KcBalanceFormatted;
            KcBadge.Visibility = Visibility.Visible;
        }
        else
        {
            KcBadge.Visibility = Visibility.Collapsed;
        }

        if (StoreScroll != null)
        {
            StoreScroll.IsHitTestVisible = true;
            StoreScroll.Visibility = Visibility.Visible;
        }
    }

    #region Featured Bundle Carousel

    private void StartBundleCarousel()
    {
        StopBundleCarousel();
        if (_bundleOffers == null || _bundleOffers.Count <= 1) return;

        _bundleCarouselTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.5) };
        _bundleCarouselTimer.Tick += (s, e) =>
        {
            var currentBundle = (_bundleOffers != null && _currentBundleIndex >= 0 && _currentBundleIndex < _bundleOffers.Count)
                ? _bundleOffers[_currentBundleIndex]
                : null;

            bool isDrawerOpen = currentBundle != null && currentBundle.IsItemsExpanded;
            bool isSkinInspecting = InspectOverlay != null && InspectOverlay.Visibility == Visibility.Visible;
            bool isCardInspecting = PlayerCardOverlay != null && PlayerCardOverlay.Visibility == Visibility.Visible;
            bool isAllWeaponsOpen = AllWeaponsPanel != null && AllWeaponsPanel.Visibility == Visibility.Visible;

            if (!_isCarouselHovered && !isDrawerOpen && !isSkinInspecting && !isCardInspecting && !isAllWeaponsOpen && _bundleOffers.Count > 1)
            {
                _currentBundleIndex = (_currentBundleIndex + 1) % _bundleOffers.Count;
                ShowBundleSlide(_currentBundleIndex, animate: true);
            }
        };
        _bundleCarouselTimer.Start();
    }

    private void StopBundleCarousel()
    {
        _bundleCarouselTimer?.Stop();
        _bundleCarouselTimer = null;
    }

    private void ShowBundleSlide(int index, bool animate = true)
    {
        if (_bundleOffers == null || _bundleOffers.Count == 0) return;
        if (index < 0 || index >= _bundleOffers.Count) index = 0;
        _currentBundleIndex = index;

        var bundle = _bundleOffers[_currentBundleIndex];

        void ApplyData()
        {
            try
            {
                var iconUri = bundle.DisplayIcon2 ?? bundle.DisplayIcon ?? bundle.VerticalPromoImage;
                if (iconUri != null)
                {
                    var bmp = GetOrCreateCachedBitmap(iconUri);
                    if (bmp != null)
                    {
                        BundleBannerImage.Source = bmp;
                    }
                }
            }
            catch { }

            // Animated Video Banner for Champions or special animated bundles
            try
            {
                if (BundleBannerVideo != null)
                {
                    if (bundle.HasVideo && !string.IsNullOrEmpty(bundle.VideoUrl))
                    {
                        var videoUri = Uri.TryCreate(bundle.VideoUrl, UriKind.Absolute, out var parsedUri)
                            ? parsedUri
                            : new Uri(Path.GetFullPath(bundle.VideoUrl), UriKind.Absolute);
                        BundleBannerVideo.Source = videoUri;
                        BundleBannerVideo.Visibility = Visibility.Visible;
                        BundleBannerVideo.Play();
                    }
                    else
                    {
                        BundleBannerVideo.Stop();
                        BundleBannerVideo.Source = null;
                        BundleBannerVideo.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch { }

            BundleTitleText.Text = bundle.Name;
            BundleDescText.Text = string.IsNullOrEmpty(bundle.Description) ? bundle.ExtraDescription : bundle.Description;
            BundleTimerText.Text = string.IsNullOrEmpty(bundle.RemainingTimeFormatted) ? "Sınırlı Süre" : bundle.RemainingTimeFormatted;
            BundleDiscountText.Text = bundle.DiscountPercentText;
            BundleDiscountBadge.Visibility = bundle.DiscountVisibility;

            BundleVpText.Text = bundle.VpCostFormatted;
            BundleBaseVpText.Text = bundle.BaseVpCostFormatted;
            BundleTlText.Text = bundle.TlCostFormatted;
            BundleSlideIndexText.Text = $"{_currentBundleIndex + 1} / {_bundleOffers.Count}";

            BundleItemsList.ItemsSource = bundle.Items;
            BundleItemsDrawer.Visibility = bundle.IsItemsExpanded ? Visibility.Visible : Visibility.Collapsed;
            ToggleBundleItemsText.Text = bundle.IsItemsExpanded
                ? L10n.Get("StoreToggleDrawerOpen")
                : string.Format(L10n.Get("StoreToggleDrawerClosed"), bundle.ItemsCount);
        }

        if (animate && BundleBannerGrid != null)
        {
            BundleBannerGrid.BeginAnimation(UIElement.OpacityProperty, null);
            var fadeOut = new DoubleAnimation(1.0, 0.4, TimeSpan.FromMilliseconds(120));
            fadeOut.Completed += (s, e) =>
            {
                ApplyData();
                var fadeIn = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(160));
                BundleBannerGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            BundleBannerGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else
        {
            if (BundleBannerGrid != null) BundleBannerGrid.BeginAnimation(UIElement.OpacityProperty, null);
            ApplyData();
        }
    }

    private void BundleBannerVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (BundleBannerVideo != null)
            {
                BundleBannerVideo.Position = TimeSpan.Zero;
                BundleBannerVideo.Play();
            }
        }
        catch { }
    }

    private void NextBundle_Click(object sender, RoutedEventArgs e)
    {
        if (_bundleOffers == null || _bundleOffers.Count == 0) return;
        _currentBundleIndex = (_currentBundleIndex + 1) % _bundleOffers.Count;
        ShowBundleSlide(_currentBundleIndex, animate: true);
        StartBundleCarousel();
    }

    private void PrevBundle_Click(object sender, RoutedEventArgs e)
    {
        if (_bundleOffers == null || _bundleOffers.Count == 0) return;
        _currentBundleIndex = (_currentBundleIndex - 1 + _bundleOffers.Count) % _bundleOffers.Count;
        ShowBundleSlide(_currentBundleIndex, animate: true);
        StartBundleCarousel();
    }

    private void ToggleBundleItems_Click(object sender, RoutedEventArgs e)
    {
        if (_bundleOffers == null || _bundleOffers.Count == 0) return;
        var bundle = _bundleOffers[_currentBundleIndex];
        bundle.IsItemsExpanded = !bundle.IsItemsExpanded;
        BundleItemsDrawer.Visibility = bundle.IsItemsExpanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleBundleItemsText.Text = bundle.IsItemsExpanded
            ? L10n.Get("StoreToggleDrawerOpen")
            : string.Format(L10n.Get("StoreToggleDrawerClosed"), bundle.ItemsCount);
    }

    private void BundleCarousel_MouseEnter(object sender, MouseEventArgs e)
    {
        _isCarouselHovered = true;
    }

    private void BundleCarousel_MouseLeave(object sender, MouseEventArgs e)
    {
        _isCarouselHovered = false;
    }

    #endregion

    private void SkinCard_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            e.Handled = true;
            if (InspectOverlay != null && InspectOverlay.Visibility == Visibility.Visible)
            {
                return;
            }
            if (PlayerCardOverlay != null && PlayerCardOverlay.Visibility == Visibility.Visible)
            {
                return;
            }

            if (sender is FrameworkElement fe && fe.DataContext is DailyStoreOffer offer)
            {
                if (offer.IsPlayerCard || string.Equals(offer.WeaponType, "OYUNCU KARTI", StringComparison.OrdinalIgnoreCase))
                {
                    ShowPlayerCard(offer);
                    return;
                }

                if (InspectOverlay != null)
                {
                    if (StoreScroll != null)
                        StoreScroll.IsHitTestVisible = false;
                    InspectOverlay.ShowSkin(offer);
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("SkinCard_Click error: {e}", ex);
        }
    }

    private void NightMarketCard_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            e.Handled = true;
            if (InspectOverlay != null && InspectOverlay.Visibility == Visibility.Visible)
            {
                return;
            }
            if (PlayerCardOverlay != null && PlayerCardOverlay.Visibility == Visibility.Visible)
            {
                return;
            }

            if (sender is FrameworkElement fe && fe.DataContext is DailyStoreOffer offer)
            {
                if (!offer.IsRevealed)
                {
                    // Authentic 2-phase 3D card flip with easing
                    var scale = fe.RenderTransform as ScaleTransform;
                    if (scale == null)
                    {
                        scale = new ScaleTransform(1.0, 1.0);
                        fe.RenderTransform = scale;
                    }
                    fe.RenderTransformOrigin = new Point(0.5, 0.5);

                    var flipDown = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(180))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };

                    flipDown.Completed += (s, ev) =>
                    {
                        offer.IsRevealed = true;
                        if (offer.OfferId != Guid.Empty)
                        {
                            NightMarketHelper.SetOfferRevealed(offer.OfferId);
                        }

                        var flipUp = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(240))
                        {
                            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
                        };

                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, flipUp);
                    };

                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, flipDown);
                }
                else
                {
                    // Already revealed -> Open skin inspect overlay or player card modal
                    if (offer.IsPlayerCard || string.Equals(offer.WeaponType, "OYUNCU KARTI", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowPlayerCard(offer);
                        return;
                    }

                    if (InspectOverlay != null)
                    {
                        if (StoreScroll != null)
                            StoreScroll.IsHitTestVisible = false;
                        InspectOverlay.ShowSkin(offer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("NightMarketCard_Click error: {e}", ex);
        }
    }

    #region Player Card Inspector

    private void ShowPlayerCard(DailyStoreOffer offer)
    {
        if (offer == null) return;

        PlayerCardTitle.Text = offer.Name;
        PlayerCardVpText.Text = offer.VpCostFormatted;
        PlayerCardTlText.Text = offer.TlCostFormatted;

        var cardGuid = offer.ItemId;

        // Large Art (Dikey - Profil & Envanter)
        var largeUri = offer.CardLargeArt ?? new Uri($"https://media.valorant-api.com/playercards/{cardGuid}/largeart.png");
        PlayerCardLargeImg.Source = GetOrCreateCachedBitmap(largeUri);

        // Wide Art (Yatay - Lobi & Karşılaşma)
        var wideUri = offer.CardWideArt ?? new Uri($"https://media.valorant-api.com/playercards/{cardGuid}/wideart.png");
        PlayerCardWideImg.Source = GetOrCreateCachedBitmap(wideUri);

        // Small Art (Kare - Avatar & Simge)
        var smallUri = offer.CardSmallArt ?? new Uri($"https://media.valorant-api.com/playercards/{cardGuid}/smallart.png");
        PlayerCardSmallImg.Source = GetOrCreateCachedBitmap(smallUri);

        if (StoreScroll != null)
            StoreScroll.IsHitTestVisible = false;

        PlayerCardOverlay.Visibility = Visibility.Visible;
    }

    private void ClosePlayerCard_Click(object sender, RoutedEventArgs e)
    {
        PlayerCardOverlay.Visibility = Visibility.Collapsed;
        if (StoreScroll != null)
            StoreScroll.IsHitTestVisible = true;
    }

    #endregion

    private void RevealAllNightMarket_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (NightMarketList?.ItemsSource is System.Collections.Generic.IEnumerable<DailyStoreOffer> offers)
            {
                NightMarketHelper.RevealAll(offers);
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("RevealAllNightMarket_Click error: {e}", ex);
        }
    }

    #region Tüm Silahlar (All Weapons & Skins Catalog)

    private List<DailyStoreOffer> _allCatalogSkins = new();
    private bool _hasLoadedCatalog;

    private async void AllWeapons_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (AllWeaponsPanel == null) return;

            if (AllWeaponsPanel.Visibility == Visibility.Visible)
            {
                // Toggle back to Store view
                AllWeaponsPanel.Visibility = Visibility.Collapsed;
                if (StoreScroll != null) StoreScroll.Visibility = Visibility.Visible;
                return;
            }

            // Show Catalog view
            if (StoreScroll != null) StoreScroll.Visibility = Visibility.Collapsed;
            AllWeaponsPanel.Visibility = Visibility.Visible;

            if (!_hasLoadedCatalog)
            {
                if (CatalogCountText != null) CatalogCountText.Text = L10n.Get("InspectLoading");
                var skins = await StoreHelper.GetAllWeaponsCatalogAsync().ConfigureAwait(true);
                _allCatalogSkins = skins ?? new List<DailyStoreOffer>();
                _hasLoadedCatalog = true;
            }

            ApplyCatalogFilter();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("AllWeapons_Click error: {e}", ex);
        }
    }

    private void BackToStore_Click(object sender, RoutedEventArgs e)
    {
        if (AllWeaponsPanel != null) AllWeaponsPanel.Visibility = Visibility.Collapsed;
        if (StoreScroll != null) StoreScroll.Visibility = Visibility.Visible;
    }

    private void CatalogSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (CatalogSearchPlaceholder != null)
            CatalogSearchPlaceholder.Visibility = string.IsNullOrEmpty(CatalogSearchBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
        if (ClearSearchBtn != null)
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(CatalogSearchBox?.Text) ? Visibility.Collapsed : Visibility.Visible;

        ApplyCatalogFilter();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogSearchBox != null) CatalogSearchBox.Text = "";
    }

    private void CatalogFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        ApplyCatalogFilter();
    }

    private void ApplyCatalogFilter()
    {
        if (_allCatalogSkins == null || CatalogSkinsList == null) return;

        var query = CatalogSearchBox?.Text?.Trim() ?? "";
        var weaponIdx = CatalogWeaponFilter?.SelectedIndex ?? 0;
        var selectedWeapon = (CatalogWeaponFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var tierIdx = CatalogTierFilter?.SelectedIndex ?? 0;
        var selectedTier = (CatalogTierFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        var filtered = _allCatalogSkins.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(s =>
                (!string.IsNullOrEmpty(s.Name) && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.WeaponType) && s.WeaponType.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (weaponIdx > 0)
        {
            var isMelee = selectedWeapon.Equals("Yakın Dövüş", StringComparison.OrdinalIgnoreCase) ||
                          selectedWeapon.Equals("Melee", StringComparison.OrdinalIgnoreCase);

            if (isMelee)
            {
                filtered = filtered.Where(s =>
                    !string.IsNullOrEmpty(s.WeaponType) &&
                    (s.WeaponType.Equals("YAKIN DÖVÜŞ", StringComparison.OrdinalIgnoreCase) ||
                     s.WeaponType.Equals("MELEE", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                filtered = filtered.Where(s =>
                    !string.IsNullOrEmpty(s.WeaponType) &&
                    s.WeaponType.Equals(selectedWeapon, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (tierIdx > 0)
        {
            var tierKey = selectedTier.Replace(" Seri", "").Replace(" Serisi", "").Replace(" Edition", "").Trim();
            filtered = filtered.Where(s =>
                (!string.IsNullOrEmpty(s.TierDisplayName) && s.TierDisplayName.Contains(tierKey, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.TierDevName) && s.TierDevName.Contains(tierKey, StringComparison.OrdinalIgnoreCase)));
        }

        var resultList = filtered.ToList();
        CatalogSkinsList.ItemsSource = resultList;

        if (CatalogCountText != null)
        {
            CatalogCountText.Text = string.Format(L10n.Get("StoreSkinsCountFormat"), resultList.Count);
        }
    }

    #endregion

    private void ShowWaitingState(string title, string message, bool showRelaunchBtn = true)
    {
        HideAllPanels();
        if (TimerBadge != null) TimerBadge.Visibility = Visibility.Collapsed;
        if (VpBadge != null) VpBadge.Visibility = Visibility.Collapsed;
        if (RpBadge != null) RpBadge.Visibility = Visibility.Collapsed;
        if (KcBadge != null) KcBadge.Visibility = Visibility.Collapsed;
        if (AllWeaponsBtn != null) AllWeaponsBtn.Visibility = Visibility.Collapsed;

        WaitingTitle.Text = title;
        WaitingMessage.Text = message;
        if (RelaunchClientBtn != null)
            RelaunchClientBtn.Visibility = showRelaunchBtn ? Visibility.Visible : Visibility.Collapsed;
        WaitingPanel.Visibility = Visibility.Visible;

        try
        {
            if (TryFindResource("PulseSpinner") is Storyboard sb)
            {
                sb.Begin();
            }
        }
        catch { }
    }

    private void ShowErrorState(string error)
    {
        HideAllPanels();
        if (AllWeaponsBtn != null) AllWeaponsBtn.Visibility = Visibility.Collapsed;
        ErrorMessage.Text = error;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideAllPanels()
    {
        _countdownTimer?.Stop();
        StopBundleCarousel();
        if (WaitingPanel != null) WaitingPanel.Visibility = Visibility.Collapsed;
        if (ErrorPanel != null) ErrorPanel.Visibility = Visibility.Collapsed;
        if (StoreScroll != null) StoreScroll.Visibility = Visibility.Collapsed;
        if (AllWeaponsPanel != null) AllWeaponsPanel.Visibility = Visibility.Collapsed;
        if (PlayerCardOverlay != null) PlayerCardOverlay.Visibility = Visibility.Collapsed;
        try
        {
            if (BundleBannerVideo != null)
            {
                BundleBannerVideo.Stop();
                BundleBannerVideo.Source = null;
                BundleBannerVideo.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    public void CloseStore()
    {
        StopBundleCarousel();

        if (PlayerCardOverlay != null && PlayerCardOverlay.Visibility == Visibility.Visible)
        {
            ClosePlayerCard_Click(null, null);
            return;
        }

        if (InspectOverlay != null && InspectOverlay.Visibility == Visibility.Visible)
        {
            InspectOverlay.CloseInspect();
            return;
        }

        try
        {
            if (BundleBannerVideo != null)
            {
                BundleBannerVideo.Stop();
                BundleBannerVideo.Source = null;
                BundleBannerVideo.Visibility = Visibility.Collapsed;
            }
        }
        catch { }

        if (AllWeaponsPanel != null)
            AllWeaponsPanel.Visibility = Visibility.Collapsed;

        if (StoreScroll != null)
            StoreScroll.IsHitTestVisible = true;

        _countdownTimer?.Stop();
        _cts?.Cancel();
        _isLoading = false;
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseStore();
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        CloseStore();
    }

    private void RefreshStore_Click(object sender, RoutedEventArgs e)
    {
        LoadStore(isRefresh: true);
    }

    private void RelaunchClient_Click(object sender, RoutedEventArgs e)
    {
        RiotClientHelper.LaunchRiotClient();
        LoadStore(isRefresh: false);
    }

    public void ApplyLocalization()
    {
        if (StoreMainTitle != null) StoreMainTitle.Text = L10n.Get("StoreTitle");
        if (StoreMainSub != null) StoreMainSub.Text = L10n.Get("StoreSubtitle");
        if (TimerPrefixText != null) TimerPrefixText.Text = L10n.Get("StoreRefreshIn");
        if (AllWeaponsBtnText != null) AllWeaponsBtnText.Text = L10n.Get("StoreAllWeapons");
        if (AllWeaponsBtn != null) AllWeaponsBtn.ToolTip = L10n.Get("StoreAllWeaponsToolTip");
        if (VpBadge != null) VpBadge.ToolTip = L10n.Get("StoreVpToolTip");
        if (RpBadge != null) RpBadge.ToolTip = L10n.Get("StoreRpToolTip");
        if (KcBadge != null) KcBadge.ToolTip = L10n.Get("StoreKcToolTip");
        if (RefreshBtn != null) RefreshBtn.ToolTip = L10n.Get("StoreRefreshToolTip");
        if (StoreCloseBtn != null) StoreCloseBtn.ToolTip = L10n.Get("StoreCloseToolTip");
        if (WaitingTitle != null) WaitingTitle.Text = L10n.Get("StoreWaitingTitle");
        if (WaitingMessage != null) WaitingMessage.Text = L10n.Get("StoreWaitingMessage");
        if (WaitingCancelBtnText != null) WaitingCancelBtnText.Text = L10n.Get("StoreWaitingCancel");
        if (RelaunchClientBtnText != null) RelaunchClientBtnText.Text = L10n.Get("StoreWaitingRelaunch");
        if (StoreErrorTitle != null) StoreErrorTitle.Text = L10n.Get("StoreErrorTitle");
        if (ErrorCloseBtnText != null) ErrorCloseBtnText.Text = L10n.Get("StoreErrorClose");
        if (ErrorRetryBtnText != null) ErrorRetryBtnText.Text = L10n.Get("StoreErrorRetry");
        if (BundleFeaturedPillText != null) BundleFeaturedPillText.Text = L10n.Get("StoreFeaturedPill");
        if (BundleDrawerTitle != null) BundleDrawerTitle.Text = L10n.Get("StoreBundleDrawerTitle");
        if (BundleDrawerSub != null) BundleDrawerSub.Text = L10n.Get("StoreBundleDrawerSub");
        if (DailyOffersTitleText != null) DailyOffersTitleText.Text = L10n.Get("StoreDailyOffersTitle");
        if (DailyOffersSubText != null) DailyOffersSubText.Text = L10n.Get("StoreDailyOffersSub");
        if (NightMarketTitleText != null) NightMarketTitleText.Text = L10n.Get("StoreNightMarketTitle");
        if (NightMarketSubtitleText != null) NightMarketSubtitleText.Text = L10n.Get("StoreNightMarketSub");
        if (RevealAllNightMarketText != null) RevealAllNightMarketText.Text = L10n.Get("StoreRevealAll");
        if (BackToStoreText != null) BackToStoreText.Text = L10n.Get("StoreBackToStore");
        if (CatalogSearchPlaceholder != null) CatalogSearchPlaceholder.Text = L10n.Get("StoreCatalogSearchPlaceholder");
        if (CatalogWeaponLabel != null) CatalogWeaponLabel.Text = L10n.Get("StoreCatalogWeaponLabel");
        if (CatalogTierLabel != null) CatalogTierLabel.Text = L10n.Get("StoreCatalogTierLabel");
        if (PlayerCardBadgeText != null) PlayerCardBadgeText.Text = L10n.Get("StorePlayerCardBadge");
        if (PlayerCardSubText != null) PlayerCardSubText.Text = L10n.Get("StorePlayerCardSub");
        if (PlayerCardCloseBtn != null) PlayerCardCloseBtn.ToolTip = L10n.Get("StoreCloseToolTip");
        if (CardVerticalTitle != null) CardVerticalTitle.Text = L10n.Get("StoreCardVerticalTitle");
        if (CardVerticalSub != null) CardVerticalSub.Text = L10n.Get("StoreCardVerticalSub");
        if (CardHorizontalTitle != null) CardHorizontalTitle.Text = L10n.Get("StoreCardHorizontalTitle");
        if (CardHorizontalSub != null) CardHorizontalSub.Text = L10n.Get("StoreCardHorizontalSub");
        if (CardSquareTitle != null) CardSquareTitle.Text = L10n.Get("StoreCardSquareTitle");
        if (CardSquareSub != null) CardSquareSub.Text = L10n.Get("StoreCardSquareSub");
        if (CardSquarePill != null) CardSquarePill.Text = L10n.Get("StoreCardSquarePill");
        if (CardFooterInfo != null) CardFooterInfo.Text = L10n.Get("StoreCardFooterInfo");

        if (CatalogWeaponFilter != null && CatalogWeaponFilter.Items.Count > 0)
        {
            if (CatalogWeaponFilter.Items[0] is ComboBoxItem firstItem)
                firstItem.Content = L10n.Get("StoreAllWeaponsDropdown");
            if (CatalogWeaponFilter.Items.Count > 3 && CatalogWeaponFilter.Items[3] is ComboBoxItem meleeItem)
                meleeItem.Content = L10n.IsEnglish ? "Melee" : "Yakın Dövüş";
        }

        if (CatalogTierFilter != null && CatalogTierFilter.Items.Count > 0)
        {
            if (CatalogTierFilter.Items[0] is ComboBoxItem firstTier)
                firstTier.Content = L10n.Get("StoreAllTiersDropdown");
            if (CatalogTierFilter.Items.Count > 1 && CatalogTierFilter.Items[1] is ComboBoxItem t1)
                t1.Content = L10n.IsEnglish ? "Ultra Edition" : "Ultra Seri";
            if (CatalogTierFilter.Items.Count > 2 && CatalogTierFilter.Items[2] is ComboBoxItem t2)
                t2.Content = L10n.IsEnglish ? "Exclusive Edition" : "Seçkin Seri";
            if (CatalogTierFilter.Items.Count > 3 && CatalogTierFilter.Items[3] is ComboBoxItem t3)
                t3.Content = L10n.IsEnglish ? "Premium Edition" : "İhtişamlı Seri";
            if (CatalogTierFilter.Items.Count > 4 && CatalogTierFilter.Items[4] is ComboBoxItem t4)
                t4.Content = L10n.IsEnglish ? "Deluxe Edition" : "Üstün Seri";
            if (CatalogTierFilter.Items.Count > 5 && CatalogTierFilter.Items[5] is ComboBoxItem t5)
                t5.Content = L10n.IsEnglish ? "Select Edition" : "Özel Seri";
        }
    }
}

