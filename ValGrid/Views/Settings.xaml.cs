using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using AutoUpdaterDotNET;
using FontAwesome6;
using ValGrid.Helpers;
using ValGrid.Properties;
using static ValGrid.Helpers.Login;
using static ValGrid.Helpers.ValApi;

namespace ValGrid.Views;

public partial class Settings : UserControl
{
    private readonly List<CultureInfo> _languageList = new();

    private static readonly string[] WeaponNames =
    {
        "Classic", "Shorty", "Frenzy", "Ghost", "Bandit", "Sheriff", "Stinger", "Spectre",
        "Bucky", "Judge", "Bulldog", "Guardian", "Warden", "Phantom", "Vandal", "Marshal",
        "Outlaw", "Operator", "Ares", "Odin"
    };

    private bool _weaponInit;
    private bool _isInitializingLanguage;

    public Settings()
    {
        InitializeComponent();
        InitWeaponCombos();
        InitLanguageList();
        ApplyLocalization();

        Loaded += (s, e) =>
        {
            ApplyLocalization();
            var win = Window.GetWindow(this);
            if (win != null)
            {
                win.PreviewKeyDown += (ws, we) =>
                {
                    if (we.Key == Key.Escape)
                    {
                        if (ChatTemplatesModal != null && ChatTemplatesModal.Visibility == Visibility.Visible)
                        {
                            ChatTemplatesModal.Visibility = Visibility.Collapsed;
                            we.Handled = true;
                            return;
                        }

                        if (PlayerDataModal != null && PlayerDataModal.Visibility == Visibility.Visible)
                        {
                            PlayerDataModal.Visibility = Visibility.Collapsed;
                            we.Handled = true;
                            return;
                        }

                        if (PlayerListOverlay != null && PlayerListOverlay.Visibility == Visibility.Visible)
                        {
                            PlayerListOverlay.Visibility = Visibility.Collapsed;
                            PlayerListOverlay.IsHitTestVisible = false;
                            we.Handled = true;
                            return;
                        }

                        if (CareerOverlay != null && CareerOverlay.Visibility == Visibility.Visible)
                        {
                            CareerOverlay.Visibility = Visibility.Collapsed;
                            CareerOverlay.IsHitTestVisible = false;
                            we.Handled = true;
                            return;
                        }
                    }
                };
            }
            InitWatcherState();
        };
    }

    private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PlayerListOverlay != null)
            {
                PlayerListOverlay.IsHitTestVisible = true;
                PlayerListOverlay.Visibility = Visibility.Visible;
                PlayerListOverlay.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenPlayerList_Click failed in Settings");
        }
    }

    private void OpenCareer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CareerOverlay != null)
            {
                CareerOverlay.IsHitTestVisible = true;
                CareerOverlay.Visibility = Visibility.Visible;
                CareerOverlay.RefreshMatches();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenCareer_Click failed in Settings");
        }
    }

    private void InitWeaponCombos()
    {
        var combos = new[] { WeaponCombo1, WeaponCombo2, WeaponCombo3, WeaponCombo4 };
        var current = new[]
        {
            Properties.Settings.Default.Weapon1,
            Properties.Settings.Default.Weapon2,
            Properties.Settings.Default.Weapon3,
            Properties.Settings.Default.Weapon4
        };
        for (var i = 0; i < combos.Length; i++)
        {
            combos[i].Items.Clear();
            foreach (var w in WeaponNames)
                combos[i].Items.Add(w);
            combos[i].SelectedItem = current[i];
        }
        _weaponInit = true;
    }

    private void WeaponChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_weaponInit)
            return;
        var combo = (ComboBox)sender;
        if (combo.SelectedItem is not string weapon || string.IsNullOrEmpty(weapon))
            return;
        switch (combo.Tag as string)
        {
            case "1":
                Properties.Settings.Default.Weapon1 = weapon;
                break;
            case "2":
                Properties.Settings.Default.Weapon2 = weapon;
                break;
            case "3":
                Properties.Settings.Default.Weapon3 = weapon;
                break;
            case "4":
                Properties.Settings.Default.Weapon4 = weapon;
                break;
        }
        Properties.Settings.Default.Save();
    }

    private async Task CheckAuthAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (AuthStatusBox != null) AuthStatusBox.Text = Properties.Resources.Refreshing;
        });
        try
        {
            var loggedIn = await Checks.CheckLoginAsync().ConfigureAwait(false);
            if (!loggedIn)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (AuthStatusBox != null) AuthStatusBox.Text = Properties.Resources.AuthStatusFail;
                });
            }
            else
            {
                var username = await GetNameServiceGetUsernameAsync(Constants.Ppuuid).ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() =>
                {
                    if (AuthStatusBox != null)
                        AuthStatusBox.Text = $"{Properties.Resources.AuthStatusAuthAs} {username}";
                });
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CheckAuthAsync failed: {e}", ex.Message);
            await Dispatcher.InvokeAsync(() =>
            {
                if (AuthStatusBox != null) AuthStatusBox.Text = Properties.Resources.AuthStatusFail;
            });
        }
    }

    private async void Button_Click1Async(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) btn.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            string productVersion = System.Windows.Forms.Application.ProductVersion;
            CurrentVersion.Text = productVersion;
            LatestVersion.Text = L10n.Get("VersionChecking");

            var latest = await GetLatestVersionAsync(productVersion).ConfigureAwait(true);
            LatestVersion.Text = latest;

            _ = Task.Run(() =>
            {
                try
                {
                    AutoUpdater.InstalledVersion = new Version(productVersion);
                    AutoUpdater.Start("https://raw.githubusercontent.com/Efeumut0/ValGrid/main/ValGrid/VersionInfo.xml");
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Button_Click1Async failed: {e}", ex.Message);
            LatestVersion.Text = L10n.VersionUpToDate(System.Windows.Forms.Application.ProductVersion);
        }
        finally
        {
            if (btn != null) btn.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }

    private static async Task<string> GetLatestVersionAsync(string currentVersion)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var content = await client.GetStringAsync("https://raw.githubusercontent.com/Efeumut0/ValGrid/main/ValGrid/VersionInfo.xml").ConfigureAwait(false);
            var xml = new XmlDocument();
            xml.LoadXml(content);
            var result = xml.GetElementsByTagName("version");
            return result.Count > 0 ? result[0].InnerText : L10n.VersionUpToDate(currentVersion);
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("GetLatestVersionAsync network check failed: {e}", ex.Message);
            return L10n.VersionUpToDate(currentVersion);
        }
    }

    private async void Button_Click2Async(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) btn.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await CheckAuthAsync().ConfigureAwait(false);
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (btn != null) btn.IsEnabled = true;
                Mouse.OverrideCursor = null;
            });
        }
    }

    private async void Button_Click3Async(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) btn.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            if (await Checks.CheckLocalAsync().ConfigureAwait(false))
            {
                await LocalLoginAsync().ConfigureAwait(false);
                await LocalRegionAsync().ConfigureAwait(false);
                await CheckAuthAsync().ConfigureAwait(false);
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (AuthStatusBox != null) AuthStatusBox.Text = Properties.Resources.NoValGame;
                });
            }
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (btn != null) btn.IsEnabled = true;
                Mouse.OverrideCursor = null;
            });
        }
    }

    private async void Button_Click4Async(object sender, RoutedEventArgs e)
    {
        if (ForceDownloadButton != null) ForceDownloadButton.IsEnabled = false;
        if (CheckDownloadButton != null) CheckDownloadButton.IsEnabled = false;
        if (DownloadProgressBar != null) DownloadProgressBar.Visibility = Visibility.Visible;
        if (DownloadStatusBox != null)
        {
            DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            DownloadStatusBox.Text = L10n.Get("DownloadCheckingVersion");
        }
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                if (DownloadStatusBox != null) DownloadStatusBox.Text = msg;
            });

            var updated = await CheckAndUpdateJsonAsync(progress).ConfigureAwait(true);
            if (DownloadStatusBox != null)
            {
                DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
                DownloadStatusBox.Text = updated
                    ? L10n.Get("DownloadUpdatedSuccess")
                    : L10n.Get("DownloadAlreadyLatest");
            }
        }
        catch (Exception ex)
        {
            if (DownloadStatusBox != null)
            {
                DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff5555"));
                DownloadStatusBox.Text = "Hata: " + ex.Message;
            }
        }
        finally
        {
            if (DownloadProgressBar != null) DownloadProgressBar.Visibility = Visibility.Collapsed;
            if (ForceDownloadButton != null) ForceDownloadButton.IsEnabled = true;
            if (CheckDownloadButton != null) CheckDownloadButton.IsEnabled = true;
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

    private async void Button_Click5Async(object sender, RoutedEventArgs e)
    {
        if (ForceDownloadButton != null) ForceDownloadButton.IsEnabled = false;
        if (CheckDownloadButton != null) CheckDownloadButton.IsEnabled = false;
        if (DownloadProgressBar != null) DownloadProgressBar.Visibility = Visibility.Visible;
        if (DownloadStatusBox != null)
        {
            DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            DownloadStatusBox.Text = L10n.Get("DownloadForceUpdating");
        }
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            var progress = new Progress<string>(msg =>
            {
                if (DownloadStatusBox != null) DownloadStatusBox.Text = msg;
            });

            var result = await UpdateFilesAsync(progress).ConfigureAwait(true);
            if (DownloadStatusBox != null)
            {
                if (result.Success)
                {
                    DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
                    DownloadStatusBox.Text = L10n.FormatDownloadSuccess(result.SkinCount);
                }
                else
                {
                    DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff5555"));
                    DownloadStatusBox.Text = "Hata: " + (result.ErrorMessage ?? "Güncelleme başarısız.");
                }
            }
        }
        catch (Exception ex)
        {
            if (DownloadStatusBox != null)
            {
                DownloadStatusBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff5555"));
                DownloadStatusBox.Text = "Hata: " + ex.Message;
            }
        }
        finally
        {
            if (DownloadProgressBar != null) DownloadProgressBar.Visibility = Visibility.Collapsed;
            if (ForceDownloadButton != null) ForceDownloadButton.IsEnabled = true;
            if (CheckDownloadButton != null) CheckDownloadButton.IsEnabled = true;
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

    private void InitLanguageList()
    {
        _isInitializingLanguage = true;
        try
        {
            LanguageCombo.Items.Clear();
            _languageList.Clear();

            // Supported languages in ValGrid (Turkish & English)
            var supportedCodes = new[] { "tr", "en" };
            var selectedIndex = 0;
            var currentLang = Properties.Settings.Default.Language;
            if (string.IsNullOrWhiteSpace(currentLang))
                currentLang = "tr";

            for (var i = 0; i < supportedCodes.Length; i++)
            {
                var code = supportedCodes[i];
                var culture = new CultureInfo(code);
                _languageList.Add(culture);

                // Format native name nicely (capitalize first letter)
                var name = culture.NativeName;
                if (!string.IsNullOrEmpty(name))
                    name = char.ToUpper(name[0]) + (name.Length > 1 ? name.Substring(1) : "");

                LanguageCombo.Items.Add(name);

                if (string.Equals(code, currentLang, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                }
            }

            if (LanguageCombo.Items.Count > 0)
            {
                LanguageCombo.SelectedIndex = selectedIndex;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "InitLanguageList failed");
        }
        finally
        {
            _isInitializingLanguage = false;
        }
    }

    private void ListBox_SelectedAsync(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingLanguage) return;

        var combo = (ComboBox)sender;
        var index = combo.SelectedIndex;
        if (index < 0 || index >= _languageList.Count) return;

        var selectedCulture = _languageList[index];
        Thread.CurrentThread.CurrentCulture = selectedCulture;
        Thread.CurrentThread.CurrentUICulture = selectedCulture;
        CultureInfo.DefaultThreadCurrentUICulture = selectedCulture;
        Properties.Resources.Culture = selectedCulture;
        Properties.Settings.Default.Language = selectedCulture.TwoLetterISOLanguageName;
        Properties.Settings.Default.Save();

        UpdateFilesAsync().ConfigureAwait(false);
        App.RestartApp();
    }

    private void InitWatcherState()
    {
        try
        {
            var isEnabled = WatcherHelper.IsWatcherEnabled();
            UpdateWatcherUI(isEnabled);
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "InitWatcherState failed in Settings");
        }
    }

    private void UpdateWatcherUI(bool isEnabled)
    {
        if (WatcherToggleButton == null) return;

        if (isEnabled)
        {
            WatcherToggleButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152a28"));
            WatcherToggleButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            if (WatcherToggleIcon != null)
            {
                WatcherToggleIcon.Icon = EFontAwesomeIcon.Solid_Check;
                WatcherToggleIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            }
            if (WatcherToggleText != null)
            {
                WatcherToggleText.Text = L10n.Get("WatcherEnabled");
                WatcherToggleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            }
            if (WatcherStatusText != null)
            {
                WatcherStatusText.Text = L10n.Get("WatcherWaiting");
                WatcherStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32e2b2"));
            }
        }
        else
        {
            WatcherToggleButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#241920"));
            WatcherToggleButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"));
            if (WatcherToggleIcon != null)
            {
                WatcherToggleIcon.Icon = EFontAwesomeIcon.Solid_Xmark;
                WatcherToggleIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"));
            }
            if (WatcherToggleText != null)
            {
                WatcherToggleText.Text = L10n.Get("WatcherDisabled");
                WatcherToggleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"));
            }
            if (WatcherStatusText != null)
            {
                WatcherStatusText.Text = L10n.Get("WatcherOff");
                WatcherStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"));
            }
        }
    }

    private void ToggleWatcher_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currentlyEnabled = WatcherHelper.IsWatcherEnabled();
            var newState = !currentlyEnabled;
            var success = WatcherHelper.SetWatcherEnabled(newState);
            if (success)
            {
                UpdateWatcherUI(newState);
            }
            else
            {
                if (WatcherStatusText != null)
                {
                    WatcherStatusText.Text = L10n.IsEnglish ? "Failed to apply operation!" : "İşlem uygulanamadı!";
                    WatcherStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ToggleWatcher_Click failed");
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = string.IsNullOrEmpty(Constants.LocalAppDataPath)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValGrid")
                : Constants.LocalAppDataPath;

            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenDataFolder_Click failed");
        }
    }

    private async void ClearCareer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                L10n.Get("ClearCareerConfirmMsg"),
                L10n.Get("ClearCareerConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MatchHistoryManager.ClearHistory();

                if (CareerOverlay != null)
                {
                    CareerOverlay.RefreshMatches();
                }

                if (DataManageStatusText != null)
                {
                    DataManageStatusText.Text = L10n.Get("ClearCareerSuccessMsg");
                    DataManageStatusText.Visibility = Visibility.Visible;
                    await Task.Delay(2500);
                    DataManageStatusText.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearCareer_Click failed");
        }
    }

    private void ClearData_Click(object sender, RoutedEventArgs e) => ClearCareer_Click(sender, e);

    #region Player Data Management Modal Handlers

    private void OpenPlayerDataModal_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerDataModal != null)
        {
            PlayerDataModal.Visibility = Visibility.Visible;
            UpdateModalCounts();
        }
    }

    private void ClosePlayerDataModal_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerDataModal != null)
        {
            PlayerDataModal.Visibility = Visibility.Collapsed;
        }
    }

    private void PlayerDataModal_BackdropClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == PlayerDataModal)
        {
            PlayerDataModal.Visibility = Visibility.Collapsed;
        }
    }

    private void PlayerDataModal_InnerClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void UpdateModalCounts()
    {
        try
        {
            if (ModalEncountersBadge != null)
                ModalEncountersBadge.Text = L10n.IsEnglish ? $"{EncounterTracker.EncountersCount} Records" : $"{EncounterTracker.EncountersCount} Kayıt";
            if (ModalNotesBadge != null)
                ModalNotesBadge.Text = L10n.IsEnglish ? $"{EncounterTracker.NotesCount} Notes" : $"{EncounterTracker.NotesCount} Not";
            if (ModalMaleBadge != null)
                ModalMaleBadge.Text = L10n.IsEnglish ? $"{MaleTagManager.Count} Players" : $"{MaleTagManager.Count} Oyuncu";
            if (ModalFemaleBadge != null)
                ModalFemaleBadge.Text = L10n.IsEnglish ? $"{FemaleTagManager.Count} Players" : $"{FemaleTagManager.Count} Oyuncu";
            if (ModalBlacklistBadge != null)
                ModalBlacklistBadge.Text = L10n.IsEnglish ? $"{BlacklistManager.Count} Players" : $"{BlacklistManager.Count} Oyuncu";
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "UpdateModalCounts failed");
        }
    }

    public void ApplyLocalization()
    {
        try
        {
            // Top Nav
            if (HomeNavBtnText != null) HomeNavBtnText.Text = L10n.Get("HomeTitle");
            if (HomeNavBtn != null) HomeNavBtn.ToolTip = L10n.Get("HomeTitle");
            if (NavPlayerListText != null) NavPlayerListText.Text = L10n.Get("NavPlayerList");
            if (NavPlayerListBtn != null) NavPlayerListBtn.ToolTip = L10n.Get("NavPlayerListTooltip");
            if (NavCareerText != null) NavCareerText.Text = L10n.Get("NavCareer");
            if (NavCareerBtn != null) NavCareerBtn.ToolTip = L10n.Get("NavCareerTooltip");
            if (NavInfoBtn != null) NavInfoBtn.ToolTip = L10n.Get("NavInfoTooltip");

            // Card 1: Language & Watcher
            if (SelectLanguageTitleText != null) SelectLanguageTitleText.Text = L10n.Get("SettingsLanguageTitle");
            if (WatcherTitleText != null) WatcherTitleText.Text = L10n.Get("WatcherTitle");
            if (WatcherSubtitleText != null) WatcherSubtitleText.Text = L10n.Get("WatcherSubtitle");

            // Card 2: Info Card
            if (InfoCardTitleText != null) InfoCardTitleText.Text = L10n.Get("SettingsInfoTitle");
            if (InfoCardDesc1Text != null) InfoCardDesc1Text.Text = L10n.Get("SettingsInfoDesc1");
            if (InfoCardDesc2Text != null) InfoCardDesc2Text.Text = L10n.Get("SettingsInfoDesc2");

            // Card 3: Auto Login Card
            if (AutoLoginTitleText != null) AutoLoginTitleText.Text = L10n.Get("SettingsAutoLoginTitle");
            if (AutoLoginDescText != null) AutoLoginDescText.Text = L10n.Get("SettingsAutoLoginDesc");
            if (AutoLoginBtnText != null) AutoLoginBtnText.Text = L10n.Get("SettingsLoginBtn");

            // Card 4: Download Content Card
            if (CheckDownloadTitleText != null) CheckDownloadTitleText.Text = L10n.Get("SettingsContentTitle");
            if (ForceDownloadBtnText != null) ForceDownloadBtnText.Text = L10n.Get("SettingsForceUpdateBtn");
            if (CheckDownloadBtnText != null) CheckDownloadBtnText.Text = L10n.Get("SettingsCheckContentBtn");

            // Card 5: Authentication Status Card
            if (AuthStatusTitleText != null) AuthStatusTitleText.Text = L10n.Get("SettingsAuthStatusTitle");
            if (AuthStatusCheckBtnText != null) AuthStatusCheckBtnText.Text = L10n.Get("SettingsAuthCheckBtn");
            if (AuthStatusBox != null && (string.IsNullOrEmpty(AuthStatusBox.Text) ||
                AuthStatusBox.Text == "Güncellemek için aşağıya tıklayın" ||
                AuthStatusBox.Text == "Click below to update" ||
                AuthStatusBox.Text == "Güncelleme yapmak için aşağıya tıkla"))
            {
                AuthStatusBox.Text = L10n.Get("SettingsAuthStatusDefault");
            }

            // Card 6: Application Updates Card
            if (AppUpdatesTitleText != null) AppUpdatesTitleText.Text = L10n.Get("SettingsAppUpdatesTitle");
            if (LatestVersionLabelText != null) LatestVersionLabelText.Text = L10n.Get("SettingsLatestVersionLabel");
            if (CurrentVersionLabelText != null) CurrentVersionLabelText.Text = L10n.Get("SettingsCurrentVersionLabel");
            if (AppUpdatesBtnText != null) AppUpdatesBtnText.Text = L10n.Get("SettingsCheckAppUpdatesBtn");

            // Bottom Bar: Weapons & Data Management
            if (DisplayWeaponsTitleText != null) DisplayWeaponsTitleText.Text = L10n.Get("SettingsWeaponsHeader");
            if (DataManageTitleText != null) DataManageTitleText.Text = L10n.Get("DataManageHeader");
            if (OpenFolderBtnText != null) OpenFolderBtnText.Text = L10n.Get("OpenFolder");
            if (OpenFolderBtn != null) OpenFolderBtn.ToolTip = L10n.Get("OpenFolderToolTip");
            if (ClearCareerBtnText != null) ClearCareerBtnText.Text = L10n.Get("ClearCareer");
            if (ClearCareerBtn != null) ClearCareerBtn.ToolTip = L10n.Get("ClearCareerToolTip");
            if (PlayerDataBtnText != null) PlayerDataBtnText.Text = L10n.Get("PlayerData");
            if (PlayerDataBtn != null) PlayerDataBtn.ToolTip = L10n.Get("PlayerDataToolTip");

            // Modal elements
            if (ModalClearTitleText != null) ModalClearTitleText.Text = L10n.Get("ModalClearHeader");
            if (ModalClearSubtitleText != null) ModalClearSubtitleText.Text = L10n.Get("ModalClearSub");
            if (ModalCat1TitleText != null) ModalCat1TitleText.Text = L10n.Get("ModalClearEncountersTitle");
            if (ModalCat1SubText != null) ModalCat1SubText.Text = L10n.Get("ModalClearEncountersSub");
            if (ModalCat1BtnText != null) ModalCat1BtnText.Text = L10n.Get("ModalBtnClear");
            if (ModalCat2TitleText != null) ModalCat2TitleText.Text = L10n.Get("ModalClearNotesTitle");
            if (ModalCat2SubText != null) ModalCat2SubText.Text = L10n.Get("ModalClearNotesSub");
            if (ModalCat2BtnText != null) ModalCat2BtnText.Text = L10n.Get("ModalBtnClear");
            if (ModalCat3TitleText != null) ModalCat3TitleText.Text = L10n.Get("ModalClearMaleTitle");
            if (ModalCat3SubText != null) ModalCat3SubText.Text = L10n.Get("ModalClearMaleSub");
            if (ModalCat3BtnText != null) ModalCat3BtnText.Text = L10n.Get("ModalBtnClear");
            if (ModalCat4TitleText != null) ModalCat4TitleText.Text = L10n.Get("ModalClearFemaleTitle");
            if (ModalCat4SubText != null) ModalCat4SubText.Text = L10n.Get("ModalClearFemaleSub");
            if (ModalCat4BtnText != null) ModalCat4BtnText.Text = L10n.Get("ModalBtnClear");
            if (ModalCat5TitleText != null) ModalCat5TitleText.Text = L10n.Get("ModalClearBlacklistTitle");
            if (ModalCat5SubText != null) ModalCat5SubText.Text = L10n.Get("ModalClearBlacklistSub");
            if (ModalCat5BtnText != null) ModalCat5BtnText.Text = L10n.Get("ModalBtnClear");
            if (ModalClearAllBtnText != null) ModalClearAllBtnText.Text = L10n.Get("ModalBtnClearAll");
            if (ModalCloseBtnText != null) ModalCloseBtnText.Text = L10n.Get("ModalBtnClose");

            if (ChatTemplatesBtnText != null) ChatTemplatesBtnText.Text = L10n.Get("ChatTemplatesBtnText");
            if (ChatTemplatesBtn != null) ChatTemplatesBtn.ToolTip = L10n.Get("ChatTemplatesBtnToolTip");

            // Chat Templates modal elements
            if (ChatTemplatesModalTitleText != null) ChatTemplatesModalTitleText.Text = L10n.Get("ChatTemplatesModalTitle");
            if (ChatTemplatesModalSubtitleText != null) ChatTemplatesModalSubtitleText.Text = L10n.Get("ChatTemplatesModalSubtitle");
            if (TplMatchStartTitleText != null) TplMatchStartTitleText.Text = L10n.IsEnglish ? "Match Start (Right Click / Copy)" : "Maç Başlangıcı (Sağ Tık / Kopyalama)";
            if (TplMatchEndTitleText != null) TplMatchEndTitleText.Text = L10n.IsEnglish ? "Match End (Left Click / Standard Copy)" : "Maç Sonu (Sol Tık / Standart Kopyalama)";
            if (TplLeaderboardTitleText != null) TplLeaderboardTitleText.Text = L10n.IsEnglish ? "Leaderboard (Trophy / Rank Copy)" : "Liderlik Sıralaması (Kupa / Sıralama Kopyalama)";

            if (TplInsertVar1Text != null) TplInsertVar1Text.Text = L10n.Get("TemplateInsertVar");
            if (TplInsertVar2Text != null) TplInsertVar2Text.Text = L10n.Get("TemplateInsertVar");
            if (TplInsertVar3Text != null) TplInsertVar3Text.Text = L10n.Get("TemplateInsertVar");

            if (TplResetBtnText != null) TplResetBtnText.Text = L10n.Get("TemplateResetDefault");
            if (TplSaveBtnText != null) TplSaveBtnText.Text = L10n.Get("TemplateSave");
            if (TplCloseBtnText != null) TplCloseBtnText.Text = L10n.Get("ModalBtnClose");

            if (ChatTemplatesModal != null && ChatTemplatesModal.Visibility == Visibility.Visible)
            {
                LoadChatTemplates();
            }

            var isWatcher = WatcherHelper.IsWatcherEnabled();
            UpdateWatcherUI(isWatcher);
            UpdateModalCounts();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "Settings ApplyLocalization failed");
        }
    }

    private async void ShowModalStatus(string msg)
    {
        if (ModalStatusText == null) return;
        ModalStatusText.Text = msg;
        ModalStatusText.Visibility = Visibility.Visible;
        await Task.Delay(2500);
        ModalStatusText.Visibility = Visibility.Collapsed;
    }

    private void ClearEncounters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = EncounterTracker.EncountersCount;
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                $"Önceden karşılaşılan oyuncu kayıtlarını ve karşılaşma sayaçlarını ({count} kayıt) temizlemek istediğinizden emin misiniz?\n(Özel oyuncu notları ve etiketler korunacaktır)",
                "Önceden Denk Gelinenleri Temizle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                EncounterTracker.ClearEncounters();
                UpdateModalCounts();
                ShowModalStatus("✓ Karşılaşma kayıtları başarıyla temizlendi!");
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearEncounters_Click failed");
        }
    }

    private void ClearNotes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = EncounterTracker.NotesCount;
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                string.Format(L10n.Get("ClearNotesConfirmMsg"), count),
                L10n.Get("ClearNotesConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                EncounterTracker.ClearNotes();
                MatchHistoryManager.ClearAllNotes();
                UpdateModalCounts();
                ShowModalStatus(L10n.Get("ClearNotesSuccessMsg"));
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearNotes_Click failed");
        }
    }

    private void ClearMaleTags_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = MaleTagManager.Count;
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                string.Format(L10n.Get("ClearMaleConfirmMsg"), count),
                L10n.Get("ClearMaleConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MaleTagManager.Clear();
                UpdateModalCounts();
                ShowModalStatus(L10n.Get("ClearMaleSuccessMsg"));
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearMaleTags_Click failed");
        }
    }

    private void ClearFemaleTags_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = FemaleTagManager.Count;
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                string.Format(L10n.Get("ClearFemaleConfirmMsg"), count),
                L10n.Get("ClearFemaleConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                FemaleTagManager.Clear();
                UpdateModalCounts();
                ShowModalStatus(L10n.Get("ClearFemaleSuccessMsg"));
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearFemaleTags_Click failed");
        }
    }

    private void ClearBlacklist_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = BlacklistManager.Count;
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                string.Format(L10n.Get("ClearBlacklistConfirmMsg"), count),
                L10n.Get("ClearBlacklistConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BlacklistManager.Clear();
                UpdateModalCounts();
                ShowModalStatus(L10n.Get("ClearBlacklistSuccessMsg"));
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearBlacklist_Click failed");
        }
    }

    private void ClearAllPlayerData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = Window.GetWindow(this);
            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                L10n.Get("ClearAllConfirmMsg"),
                L10n.Get("ClearAllConfirmTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                EncounterTracker.ClearEncounters();
                EncounterTracker.ClearNotes();
                MatchHistoryManager.ClearAllNotes();
                MaleTagManager.Clear();
                FemaleTagManager.Clear();
                BlacklistManager.Clear();
                UpdateModalCounts();
                ShowModalStatus(L10n.Get("ClearAllSuccessMsg"));
                CareerOverlay?.RefreshMatches();
                PlayerListOverlay?.RefreshDirectory();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ClearAllPlayerData_Click failed");
        }
    }

    #endregion

    #region Chat Templates Modal Handlers

    private void OpenChatTemplatesModal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ChatTemplatesModal != null)
            {
                LoadChatTemplates();
                ChatTemplatesModal.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenChatTemplatesModal_Click failed");
        }
    }

    private void CloseChatTemplatesModal_Click(object sender, RoutedEventArgs e)
    {
        if (ChatTemplatesModal != null)
        {
            ChatTemplatesModal.Visibility = Visibility.Collapsed;
        }
    }

    private void ChatTemplatesModal_BackdropClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == ChatTemplatesModal)
        {
            ChatTemplatesModal.Visibility = Visibility.Collapsed;
        }
    }

    private void ChatTemplatesModal_InnerClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void LoadChatTemplates()
    {
        var isEn = L10n.IsEnglish;
        if (TplMatchStartBox != null)
            TplMatchStartBox.Text = ChatTemplateManager.GetTemplate(ChatTemplateType.MatchStart, isEn);
        if (TplMatchEndBox != null)
            TplMatchEndBox.Text = ChatTemplateManager.GetTemplate(ChatTemplateType.MatchEnd, isEn);
        if (TplLeaderboardBox != null)
            TplLeaderboardBox.Text = ChatTemplateManager.GetTemplate(ChatTemplateType.Leaderboard, isEn);

        UpdateAllPreviews();
    }

    private void UpdateAllPreviews()
    {
        var isEn = L10n.IsEnglish;
        if (TplMatchStartPreviewText != null && TplMatchStartBox != null)
            TplMatchStartPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplMatchStartBox.Text, ChatTemplateType.MatchStart, isEn);

        if (TplMatchEndPreviewText != null && TplMatchEndBox != null)
            TplMatchEndPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplMatchEndBox.Text, ChatTemplateType.MatchEnd, isEn);

        if (TplLeaderboardPreviewText != null && TplLeaderboardBox != null)
            TplLeaderboardPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplLeaderboardBox.Text, ChatTemplateType.Leaderboard, isEn);
    }

    private void TplMatchStartBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TplMatchStartPreviewText != null && TplMatchStartBox != null)
            TplMatchStartPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplMatchStartBox.Text, ChatTemplateType.MatchStart, L10n.IsEnglish);
    }

    private void TplMatchEndBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TplMatchEndPreviewText != null && TplMatchEndBox != null)
            TplMatchEndPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplMatchEndBox.Text, ChatTemplateType.MatchEnd, L10n.IsEnglish);
    }

    private void TplLeaderboardBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TplLeaderboardPreviewText != null && TplLeaderboardBox != null)
            TplLeaderboardPreviewText.Text = ChatTemplateManager.GetSamplePreview(TplLeaderboardBox.Text, ChatTemplateType.Leaderboard, L10n.IsEnglish);
    }

    private void InsertVarStart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            InsertTextAtCaret(TplMatchStartBox, tag);
        }
    }

    private void InsertVarEnd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            InsertTextAtCaret(TplMatchEndBox, tag);
        }
    }

    private void InsertVarLeaderboard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            InsertTextAtCaret(TplLeaderboardBox, tag);
        }
    }

    private void InsertTextAtCaret(TextBox? box, string text)
    {
        if (box == null) return;
        var caret = box.CaretIndex;
        var current = box.Text ?? "";
        if (caret < 0 || caret > current.Length) caret = current.Length;
        box.Text = current.Insert(caret, text);
        box.CaretIndex = caret + text.Length;
        box.Focus();
    }

    private async void SaveChatTemplates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var isEn = L10n.IsEnglish;
            var start = TplMatchStartBox?.Text ?? "";
            var end = TplMatchEndBox?.Text ?? "";
            var lb = TplLeaderboardBox?.Text ?? "";

            ChatTemplateManager.SaveTemplates(start, end, lb, isEn);

            if (TplStatusText != null)
            {
                TplStatusText.Text = L10n.Get("TemplateSavedStatus");
                TplStatusText.Visibility = Visibility.Visible;
                await Task.Delay(2500);
                TplStatusText.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "SaveChatTemplates_Click failed");
        }
    }

    private async void ResetChatTemplates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var isEn = L10n.IsEnglish;
            var win = Window.GetWindow(this);
            var confirmTitle = isEn ? "Reset Chat Templates" : "Chat Şablonlarını Sıfırla";
            var confirmMsg = isEn
                ? "Are you sure you want to reset chat templates to default values?"
                : "Chat şablonlarını varsayılan orijinal değerlerine döndürmek istediğinizden emin misiniz?";

            var result = MessageBox.Show(win ?? Application.Current.MainWindow,
                confirmMsg,
                confirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ChatTemplateManager.ResetToDefaults(isEn);
                LoadChatTemplates();

                if (TplStatusText != null)
                {
                    TplStatusText.Text = L10n.Get("TemplateResetStatus");
                    TplStatusText.Visibility = Visibility.Visible;
                    await Task.Delay(2500);
                    TplStatusText.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ResetChatTemplates_Click failed");
        }
    }

    #endregion
}

