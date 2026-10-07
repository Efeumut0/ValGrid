using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.ViewModels;

public partial class MatchViewModel : ObservableObject
{
    public delegate void EventAction();

    [ObservableProperty]
    private int _countdownTime = 60;

    [ObservableProperty]
    private DispatcherTimer _countTimer;

    [ObservableProperty]
    private List<Player> _leftPlayerList;

    [ObservableProperty]
    private MatchDetails _match;

    [ObservableProperty]
    private LoadingOverlay _overlay;

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
                    CountdownTime = _resettime;
                    if (_countTimer != null && !_countTimer.IsEnabled)
                        _countTimer.Start();
                }
            }
        }
    }

    [ObservableProperty]
    private string _refreshTime = "-";
    private int _resettime = 60;
    private int _isRefreshing = 0;

    [ObservableProperty]
    private List<Player> _rightPlayerList;

    private bool _isEnemyPlaceholderVisible;
    public bool IsEnemyPlaceholderVisible
    {
        get => _isEnemyPlaceholderVisible;
        set => SetProperty(ref _isEnemyPlaceholderVisible, value);
    }

    [ObservableProperty]
    private int _uniformGridRows = 5;

    public MatchViewModel()
    {
        _countTimer = new DispatcherTimer();
        _countTimer.Tick += UpdateTimersAsync;
        _countTimer.Interval = new TimeSpan(0, 0, 1);

        Match = new MatchDetails();
        Overlay = new LoadingOverlay
        {
            Header = "Yükleniyor...",
            Content = "Oyuncu Detayları Alınıyor...",
            IsBusy = false
        };

        LeftPlayerList = new List<Player>();
        RightPlayerList = new List<Player>();

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
            CountdownTime = _resettime;
            if (_countTimer != null && !_countTimer.IsEnabled)
                _countTimer.Start();
        }
    }


    public event EventAction GoHomeEvent;

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
            await GetMatchInfoAsync().ConfigureAwait(false);
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
        if (CountdownTime <= 0)
        {
            // Defer refresh while user is editing a note
            if (Controls.PlayerControl.IsAnyNotePopupOpen)
            {
                CountdownTime = 5; // retry in 5 seconds
                return;
            }
            CountdownTime = _resettime;
            await GetMatchInfoAsync().ConfigureAwait(false);
        }

        CountdownTime--;
    }

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
    private async Task GetMatchInfoAsync()
    {
        if (Interlocked.CompareExchange(ref _isRefreshing, 1, 0) != 0)
            return;

        SetOverlay(true, "Yükleniyor...", "Oyuncu Detayları Alınıyor...", 0);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var fetchTask = FetchMatchInfoInternalAsync();
            var completedTask = await Task.WhenAny(fetchTask, Task.Delay(25000, cts.Token)).ConfigureAwait(false);
            if (completedTask != fetchTask)
            {
                Constants.Log?.Warning("GetMatchInfoAsync timed out after 25 seconds.");
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "Error in GetMatchInfoAsync");
        }
        finally
        {
            SetOverlay(false);
            Interlocked.Exchange(ref _isRefreshing, 0);
        }
    }

    private async Task FetchMatchInfoInternalAsync()
    {
        try
        {
            LiveMatch newLiveMatch = new();
            if (await LiveMatch.LiveMatchChecksAsync().ConfigureAwait(false))
            {
                var AllPlayers = new List<Player>();
                if (Overlay != null)
                    Overlay.Content = "Oyuncu Detayları Alınıyor...";
                AllPlayers = await newLiveMatch
                    .LiveMatchOutputAsync(UpdatePercentage)
                    .ConfigureAwait(false);

                if (newLiveMatch.Status == "PREGAME" || LiveMatch.Stage == "pre")
                {
                    _resettime = 10;
                    CountdownTime = 10;
                }
                else
                {
                    _resettime = 60;
                    CountdownTime = 60;
                }

                bool isArenaMode = (newLiveMatch.QueueId?.ToLowerInvariant() == "abilitydraftarena")
                                || (newLiveMatch.MatchInfo?.GameMode?.ToLowerInvariant().Contains("arena") == true)
                                || (AllPlayers.Count == 16);

                if (newLiveMatch.QueueId == "deathmatch" || AllPlayers.Count > 10)
                {
                    // Group players by TeamId to keep duos/teams together
                    var teamGroups = AllPlayers
                        .GroupBy(p => p.TeamId ?? "")
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .Select(g => g.ToList())
                        .ToList();

                    // If TeamId gave multiple groups (e.g. 8 duos), use them
                    if (teamGroups.Count > 2)
                    {
                        // Bring our duo to the very front so it appears at the top left
                        var myGroupIndex = teamGroups.FindIndex(g => g.Any(p => p.PlayerUiData?.Puuid == Constants.Ppuuid));
                        if (myGroupIndex > 0)
                        {
                            var myGroup = teamGroups[myGroupIndex];
                            teamGroups.RemoveAt(myGroupIndex);
                            teamGroups.Insert(0, myGroup);
                        }

                        var leftGroupCount = (teamGroups.Count + 1) / 2;
                        LeftPlayerList = teamGroups.Take(leftGroupCount).SelectMany(g => g).ToList();
                        RightPlayerList = teamGroups.Skip(leftGroupCount).SelectMany(g => g).ToList();
                    }
                    else if (isArenaMode && AllPlayers.Count == 16)
                    {
                        // 16-player arena where TeamId was not differentiated: group into 8 duos of 2
                        var pairs = new List<List<Player>>();
                        for (int i = 0; i < AllPlayers.Count; i += 2)
                        {
                            pairs.Add(AllPlayers.Skip(i).Take(2).ToList());
                        }

                        var myPairIndex = pairs.FindIndex(pair => pair.Any(p => p.PlayerUiData?.Puuid == Constants.Ppuuid));
                        if (myPairIndex > 0)
                        {
                            var myPair = pairs[myPairIndex];
                            pairs.RemoveAt(myPairIndex);
                            pairs.Insert(0, myPair);
                        }

                        LeftPlayerList = pairs.Take(4).SelectMany(p => p).ToList();
                        RightPlayerList = pairs.Skip(4).SelectMany(p => p).ToList();
                    }
                    else
                    {
                        // Deathmatch or generic split
                        var mid = AllPlayers.Count / 2;
                        LeftPlayerList = AllPlayers.Take(mid).ToList();
                        RightPlayerList = AllPlayers.Skip(mid).ToList();
                    }
                }
                else
                {
                    LeftPlayerList.Clear();
                    RightPlayerList.Clear();

                    // Sol tarafta her zaman benim olduğum takım, sağ tarafta karşı takım olmalı
                    var myPlayer = AllPlayers.FirstOrDefault(p => p.PlayerUiData?.Puuid == Constants.Ppuuid);
                    var myTeam = myPlayer?.TeamId ?? "Blue";

                    foreach (var player in AllPlayers)
                    {
                        if (player.TeamId == myTeam)
                            LeftPlayerList.Add(player);
                        else
                            RightPlayerList.Add(player);
                    }

                    // Fallback: If AllPlayers has more than 5 players (e.g. 10 players) but RightPlayerList is empty
                    if (RightPlayerList.Count == 0 && AllPlayers.Count > 5)
                    {
                        LeftPlayerList.Clear();
                        RightPlayerList.Clear();
                        var mid = AllPlayers.Count / 2;
                        LeftPlayerList.AddRange(AllPlayers.Take(mid));
                        RightPlayerList.AddRange(AllPlayers.Skip(mid));
                    }

                    LeftPlayerList = LeftPlayerList.ToList();
                    RightPlayerList = RightPlayerList.ToList();
                }

                IsEnemyPlaceholderVisible = (RightPlayerList.Count == 0);
                AllPlayers.Clear();

                if (newLiveMatch.MatchInfo != null)
                    Match = newLiveMatch.MatchInfo;

                if (isArenaMode)
                {
                    UniformGridRows = Math.Max(8, Math.Max(LeftPlayerList.Count, RightPlayerList.Count));
                    Match.TeamBlueLabel = "Sol (4 Takım): ";
                    Match.TeamRedLabel = "Sağ (4 Takım): ";
                }
                else if (newLiveMatch.QueueId == "deathmatch")
                {
                    UniformGridRows = Math.Max(6, Math.Max(LeftPlayerList.Count, RightPlayerList.Count));
                    Match.TeamBlueLabel = "Sol Taraf: ";
                    Match.TeamRedLabel = "Sağ Taraf: ";
                }
                else
                {
                    UniformGridRows = 5;
                    Match.TeamBlueLabel = "Bizim Takım: ";
                    Match.TeamRedLabel = "Karşı Takım: ";
                }

                if (LeftPlayerList.Count > 0 && RightPlayerList.Count > 0)
                {
                    Match.TeamBlueValueVp = LeftPlayerList.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
                    Match.TeamRedValueVp = RightPlayerList.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
                }

                Views.Match.UpdateMatchLeaderboard(this);

                UpdateStats();

                SetOverlay(false);
            }
            else
            {
                CountTimer?.Stop();
                GoHomeEvent?.Invoke();
                _ = Helpers.EncounterTracker.UpdatePendingMatchResultsAsync();
                _ = Helpers.MatchHistoryManager.UpdatePendingMatchesAsync();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "Error in FetchMatchInfoInternalAsync");
        }
        finally
        {
            SetOverlay(false);
        }
    }

    private async void UpdateStats()
    {
        // List<Task> tasks = new();
        var AllPlayers = LeftPlayerList.Concat(RightPlayerList).ToList();
        foreach (var player in AllPlayers)
        {
            if (player.PlayerUiData is null)
                continue;
            // var t1 = LiveMatch.GetMatchHistoryAsync(player.PlayerUiData.Puuid);
            // player.MatchHistoryData = t1.Result;
            player.MatchHistoryData = await LiveMatch
                .GetMatchHistoryAsync(player.PlayerUiData.Puuid)
                .ConfigureAwait(false);
        }
    }

    private void UpdatePercentage(int percentage)
    {
        try
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                if (Overlay != null)
                    Overlay.Progress = percentage;
            }, DispatcherPriority.Background);
        }
        catch { }
    }
}

