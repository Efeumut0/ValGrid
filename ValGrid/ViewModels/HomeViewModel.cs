using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FontAwesome6;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using ValGrid.Helpers;
using ValGrid.Objects;
using ValGrid.Views;
using static ValGrid.Helpers.Login;

namespace ValGrid.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    public delegate void EventAction();

    [ObservableProperty]
    private int _countdownTime = 20;

    [ObservableProperty]
    private DispatcherTimer _countTimer;
    private int _cycle = 3;

    private List<Player> _playerList;
    public List<Player> PlayerList
    {
        get => _playerList;
        set
        {
            if (SetProperty(ref _playerList, value))
            {
                HasPartyPlayers = value != null && value.Count > 0;
                EmptyPartyVisibility = HasPartyPlayers ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }

    [ObservableProperty]
    private bool _hasPartyPlayers;

    [ObservableProperty]
    private Visibility _emptyPartyVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _storeButtonVisibility = RiotClientHelper.IsValorantInstalled() ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private string _refreshTime = "-";

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetProperty(ref _isPaused, value))
            {
                if (RefreshManager.IsPaused != value)
                    RefreshManager.IsPaused = value;

                if (value)
                {
                    _countTimer?.Stop();
                    RefreshTime = "-";
                }
                else
                {
                    CountdownTime = 20;
                    if (_countTimer != null && !_countTimer.IsEnabled)
                        _countTimer.Start();
                }
            }
        }
    }

    [ObservableProperty]
    private LoadingOverlay _overlay;

    private int _isRefreshing = 0;

    public HomeViewModel()
    {
        _countTimer = new DispatcherTimer();
        _countTimer.Tick += UpdateTimersAsync;
        _countTimer.Interval = new TimeSpan(0, 0, 1);

        Overlay = new LoadingOverlay
        {
            Header = "Yükleniyor...",
            Content = "Oyuncu Detayları Alınıyor...",
            IsBusy = false
        };

        _isPaused = RefreshManager.IsPaused;
        if (_isPaused)
        {
            RefreshTime = "-";
        }

        RefreshManager.OnPauseChanged += HandlePauseChanged;
    }

    private void HandlePauseChanged(bool paused)
    {
        if (IsPaused != paused)
            IsPaused = paused;

        if (paused)
        {
            _countTimer?.Stop();
            RefreshTime = "-";
        }
        else
        {
            CountdownTime = 20;
            if (_countTimer != null && !_countTimer.IsEnabled)
                _countTimer.Start();
        }
    }


    public event EventAction GoMatchEvent;

    private void SetOverlay(bool isBusy, string header = "", string content = "", int progress = 0)
    {
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.HasShutdownStarted)
            {
                dispatcher.InvokeAsync(() =>
                {
                    if (Overlay == null)
                        Overlay = new LoadingOverlay();

                    if (isBusy)
                    {
                        Overlay.Header = header;
                        Overlay.Content = content;
                        Overlay.Progress = progress;
                    }
                    Overlay.IsBusy = isBusy;
                }, DispatcherPriority.Send);
            }
        }
        catch { }
    }

    [ICommand]
    private async Task LoadNowAsync()
    {
        if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0)
            return;

        CountdownTime = 20;
        SetOverlay(true, "Yükleniyor...", "Oyuncu Detayları Alınıyor...", 0);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var updateTask = UpdateChecksAsync(true);
            var completedTask = await Task.WhenAny(updateTask, Task.Delay(20000, cts.Token)).ConfigureAwait(false);
            if (completedTask != updateTask)
            {
                Constants.Log?.Warning("LoadNowAsync timed out after 20 seconds.");
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "Error in LoadNowAsync");
        }
        finally
        {
            SetOverlay(false);
            Interlocked.Exchange(ref _isRefreshing, 0);
        }
    }

    [ICommand]
    private void PassiveLoadAsync()
    {
        IsPaused = false;
    }

    [ICommand]
    private async Task PassiveLoadCheckAsync()
    {
        if (RefreshManager.IsPaused)
        {
            _countTimer?.Stop();
            RefreshTime = "-";
            return;
        }

        if (!_countTimer.IsEnabled)
        {
            _countTimer.Start();
            _ = Task.Run(EncounterTracker.UpdatePendingMatchResultsAsync);
            _ = Task.Run(MatchHistoryManager.UpdatePendingMatchesAsync);
            await UpdateChecksAsync(true).ConfigureAwait(false);
        }
    }

    [ICommand]
    private void StopPassiveLoadAsync()
    {
        IsPaused = true;
    }

    [ICommand]
    private void UnloadView()
    {
        _countTimer?.Stop();
        RefreshManager.OnPauseChanged -= HandlePauseChanged;
    }

    private async void UpdateTimersAsync(object sender, EventArgs e)
    {
        if (RefreshManager.IsPaused)
        {
            _countTimer?.Stop();
            RefreshTime = "-";
            return;
        }

        RefreshTime = CountdownTime + "s";
        if (CountdownTime == 0)
        {
            CountdownTime = 15;
            await UpdateChecksAsync(false).ConfigureAwait(false);
        }

        CountdownTime--;
    }

    private static void SafeSetStatus(Action action)
    {
        try
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        action();
                    }
                    catch { }
                });
            }
        }
        catch { }
    }

    [ICommand]
    private async Task UpdateChecksAsync(bool forcePartyUpdate)
    {
        SafeSetStatus(() =>
        {
            if (Home.ValorantStatus != null)
            {
                Home.ValorantStatus.Icon = EFontAwesomeIcon.Solid_Question;
                Home.ValorantStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            }
            if (Home.AccountStatus != null)
            {
                Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Question;
                Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            }
            if (Home.MatchStatus != null)
            {
                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Question;
                Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 126, 249));
            }
        });

        var isLocalConnected = await Checks.CheckLocalAsync().ConfigureAwait(false);
        if (!isLocalConnected)
        {
            if (System.Diagnostics.Process.GetProcessesByName("RiotClientServices").Length > 0 ||
                System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
            {
                await Task.Delay(300).ConfigureAwait(false);
                isLocalConnected = await Checks.CheckLocalAsync().ConfigureAwait(false);
            }
        }

        if (isLocalConnected)
        {
            SafeSetStatus(() =>
            {
                if (Home.ValorantStatus != null)
                {
                    Home.ValorantStatus.Icon = EFontAwesomeIcon.Solid_Check;
                    Home.ValorantStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 226, 178));
                }
            });
            if (await Checks.CheckLoginAsync().ConfigureAwait(false))
            {
                SafeSetStatus(() =>
                {
                    if (Home.AccountStatus != null)
                    {
                        Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Check;
                        Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 226, 178));
                    }
                });
                if (await Checks.CheckMatchAsync().ConfigureAwait(false))
                {
                    SafeSetStatus(() =>
                    {
                        if (Home.MatchStatus != null)
                        {
                            Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Check;
                            Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 226, 178));
                        }
                    });
                    CountTimer?.Stop();
                    GoMatchEvent?.Invoke();
                }
                else
                {
                    SafeSetStatus(() =>
                    {
                        if (Home.MatchStatus != null)
                        {
                            Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                            Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                        }
                    });
                    if (forcePartyUpdate)
                    {
                        _cycle++;
                        await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        if (_cycle == 0)
                        {
                            await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                            _cycle = 3;
                        }

                        _cycle--;
                    }
                }
            }
            else
            {
                await LocalLoginAsync().ConfigureAwait(false);
                await LocalRegionAsync().ConfigureAwait(false);
                if (await Checks.CheckLoginAsync().ConfigureAwait(false))
                {
                    SafeSetStatus(() =>
                    {
                        if (Home.AccountStatus != null)
                        {
                            Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Check;
                            Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 226, 178));
                        }
                    });
                    if (await Checks.CheckMatchAsync().ConfigureAwait(false))
                    {
                        SafeSetStatus(() =>
                        {
                            if (Home.MatchStatus != null)
                            {
                                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Check;
                                Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(50, 226, 178));
                            }
                        });
                        CountTimer?.Stop();
                        GoMatchEvent?.Invoke();
                    }
                    else
                    {
                        SafeSetStatus(() =>
                        {
                            if (Home.MatchStatus != null)
                            {
                                Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                                Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                            }
                        });
                        if (forcePartyUpdate)
                        {
                            _cycle++;
                            await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            if (_cycle == 0)
                            {
                                await GetPartyPlayerInfoAsync().ConfigureAwait(false);
                                _cycle = 3;
                            }

                            _cycle--;
                        }
                    }
                }
                else
                {
                    SafeSetStatus(() =>
                    {
                        if (Home.AccountStatus != null)
                        {
                            Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                            Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                        }
                        if (Home.MatchStatus != null)
                        {
                            Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                            Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                        }
                    });
                }
            }
        }
        else
        {
            SafeSetStatus(() =>
            {
                if (Home.ValorantStatus != null)
                {
                    Home.ValorantStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                    Home.ValorantStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                }
                if (Home.AccountStatus != null)
                {
                    Home.AccountStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                    Home.AccountStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                }
                if (Home.MatchStatus != null)
                {
                    Home.MatchStatus.Icon = EFontAwesomeIcon.Solid_Xmark;
                    Home.MatchStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 70, 84));
                }
            });
        }
    }

    [ICommand]
    private async Task GetPartyPlayerInfoAsync()
    {
        try
        {
            LiveMatch newLiveMatch = new();
            if (await newLiveMatch.CheckAndSetPartyIdAsync().ConfigureAwait(false))
                PlayerList = await newLiveMatch.PartyOutputAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}

