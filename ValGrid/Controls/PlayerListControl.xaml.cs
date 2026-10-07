using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome6;
using FontAwesome6.Fonts;
using ValGrid.Helpers;

namespace ValGrid.Controls;

public enum DirectoryTab
{
    Female,
    Male,
    Noted,
    Encounter,
    Blacklist,
    All
}

public class PlayerMatchDisplayItem
{
    public string TeamText { get; set; } = "";
    public string TeamColor { get; set; } = "#32e2b2";
    public string Map { get; set; } = "";
    public string AgentName { get; set; } = "";
    public string Date { get; set; } = "";
}

public class DirectoryPlayerItem : INotifyPropertyChanged
{
    public string Puuid { get; set; } = "";
    private string _username = "";
    public string Username
    {
        get => _username;
        set
        {
            if (_username == value) return;
            _username = value;
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(InitialLetter));
        }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Username) ? (L10n.IsEnglish ? "Unknown Player" : "Bilinmeyen Oyuncu") : Username;

    public string InitialLetter
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Username)) return "?";
            var trimmed = Username.Trim();
            return trimmed.Length > 0 ? trimmed.Substring(0, 1).ToUpperInvariant() : "?";
        }
    }

    public string PuuidShort
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Puuid)) return "";
            if (Puuid.Length > 16)
                return "PUUID: " + Puuid.Substring(0, 8) + "..." + Puuid.Substring(Puuid.Length - 4);
            return "PUUID: " + Puuid;
        }
    }

    private bool _isFemale;
    public bool IsFemale
    {
        get => _isFemale;
        set
        {
            if (_isFemale == value) return;
            _isFemale = value;
            OnPropertyChanged(nameof(IsFemale));
            OnPropertyChanged(nameof(FemaleBadgeVisibility));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(FemaleToggleText));
            OnPropertyChanged(nameof(FemaleToggleColor));
            OnPropertyChanged(nameof(FemaleActionBg));
            OnPropertyChanged(nameof(FemaleActionBorder));
            OnPropertyChanged(nameof(CardBackground));
        }
    }

    private bool _isMale;
    public bool IsMale
    {
        get => _isMale;
        set
        {
            if (_isMale == value) return;
            _isMale = value;
            OnPropertyChanged(nameof(IsMale));
            OnPropertyChanged(nameof(MaleBadgeVisibility));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(MaleToggleText));
            OnPropertyChanged(nameof(MaleToggleColor));
            OnPropertyChanged(nameof(MaleActionBg));
            OnPropertyChanged(nameof(MaleActionBorder));
        }
    }

    private bool _isBlacklisted;
    public bool IsBlacklisted
    {
        get => _isBlacklisted;
        set
        {
            if (_isBlacklisted == value) return;
            _isBlacklisted = value;
            OnPropertyChanged(nameof(IsBlacklisted));
            OnPropertyChanged(nameof(BlacklistBadgeVisibility));
            OnPropertyChanged(nameof(BlacklistToggleText));
            OnPropertyChanged(nameof(CardBorderBrush));
        }
    }

    public int AllyCount { get; set; }
    public int EnemyCount { get; set; }
    public int TotalEncounters => AllyCount + EnemyCount;
    public string TotalEncountersText => TotalEncounters + (L10n.IsEnglish ? "x Met" : "x Denk");

    public string EncounterBreakdown
    {
        get
        {
            if (TotalEncounters <= 0)
                return L10n.IsEnglish ? "0 Matches" : "0 Maç";
            return L10n.IsEnglish
                ? $"🟢 {AllyCount} Ally • 🔴 {EnemyCount} Enemy"
                : $"🟢 {AllyCount} Dost • 🔴 {EnemyCount} Rakip";
        }
    }

    public string LastSeenRaw { get; set; } = "";
    public string LastSeenText { get; set; } = L10n.IsEnglish ? "Unknown" : "Bilinmiyor";
    public string LastSeenMap { get; set; } = "";
    public string LastSeenAgent { get; set; } = "";
    public string LastSeenAgentIcon { get; set; } = "";
    public bool HasAgentIcon => !string.IsNullOrEmpty(LastSeenAgentIcon);
    public Visibility HasAgentIconVisibility => HasAgentIcon ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoAgentIconVisibility => HasAgentIcon ? Visibility.Collapsed : Visibility.Visible;

    public string LastSeenDetails
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(LastSeenMap)) parts.Add(LastSeenMap);
            if (!string.IsNullOrWhiteSpace(LastSeenAgent) && LastSeenAgent != "Bilinmeyen" && LastSeenAgent != "Unknown") parts.Add(LastSeenAgent);
            return parts.Count > 0 ? $"({string.Join(" • ", parts)})" : "";
        }
    }

    public string LastSeenSummary
    {
        get
        {
            var unk = L10n.IsEnglish ? "Unknown" : "Bilinmiyor";
            if (string.IsNullOrWhiteSpace(LastSeenText) || LastSeenText == "Bilinmiyor" || LastSeenText == "Unknown")
            {
                return !string.IsNullOrWhiteSpace(LastSeenMap) ? LastSeenMap : unk;
            }
            if (!string.IsNullOrWhiteSpace(LastSeenMap))
            {
                return $"{LastSeenText} • {LastSeenMap}";
            }
            return LastSeenText;
        }
    }

    public string LastSeenFullTooltip
    {
        get
        {
            var details = LastSeenDetails;
            var prefix = L10n.IsEnglish ? "Last Seen: " : "Son Görülme: ";
            return string.IsNullOrWhiteSpace(details)
                ? $"{prefix}{LastSeenText}"
                : $"{prefix}{LastSeenText} {details}";
        }
    }

    private string _note = "";
    public string Note
    {
        get => _note;
        set
        {
            if (_note == value) return;
            _note = value;
            OnPropertyChanged(nameof(Note));
            OnPropertyChanged(nameof(HasNote));
            OnPropertyChanged(nameof(HasNoteVisibility));
            OnPropertyChanged(nameof(NoNoteVisibility));
            OnPropertyChanged(nameof(CardBorderBrush));
        }
    }

    public bool HasNote => !string.IsNullOrWhiteSpace(_note);
    public Visibility HasNoteVisibility => HasNote ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoNoteVisibility => HasNote ? Visibility.Collapsed : Visibility.Visible;

    private bool _isEditingNote;
    public bool IsEditingNote
    {
        get => _isEditingNote;
        set
        {
            if (_isEditingNote == value) return;
            _isEditingNote = value;
            OnPropertyChanged(nameof(IsEditingNote));
            OnPropertyChanged(nameof(EditNoteVisibility));
            OnPropertyChanged(nameof(DisplayNoteVisibility));
        }
    }

    public Visibility EditNoteVisibility => _isEditingNote ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DisplayNoteVisibility => _isEditingNote ? Visibility.Collapsed : Visibility.Visible;

    private string _editNoteText = "";
    public string EditNoteText
    {
        get => _editNoteText;
        set
        {
            if (_editNoteText == value) return;
            _editNoteText = value;
            OnPropertyChanged(nameof(EditNoteText));
        }
    }

    public Visibility FemaleBadgeVisibility => IsFemale ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MaleBadgeVisibility => IsMale ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BlacklistBadgeVisibility => IsBlacklisted ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EncounterBadgeVisibility => TotalEncounters > 0 ? Visibility.Visible : Visibility.Collapsed;

    public string FemaleBadgeText => L10n.IsEnglish ? "FEMALE" : "KIZ";
    public string MaleBadgeText => L10n.IsEnglish ? "MALE" : "ERKEK";
    public string AddNoteText => L10n.IsEnglish ? "Add Note" : "Not Ekle";
    public string EditNoteHeader => L10n.IsEnglish ? "Edit Player Note:" : "Oyuncu Notunu Düzenle:";
    public string SaveNoteText => L10n.IsEnglish ? "Save" : "Kaydet";
    public string DeleteNoteText => L10n.IsEnglish ? "Delete" : "Sil";
    public string CancelNoteText => L10n.IsEnglish ? "✕ Cancel" : "✕ İptal";
    public string MatchesHeader => L10n.IsEnglish ? "Recent Matches Together:" : "Birlikte Oynanan Son Maçlar:";
    public string CopyUsernameTooltip => L10n.IsEnglish ? "Copy Username" : "Kullanıcı Adını Kopyala";
    public string DeleteMenuTooltip => L10n.IsEnglish ? "Delete / Manage Data" : "Verileri Sil / Yönet";
    public string FemaleToggleTooltip => L10n.IsEnglish ? "Toggle female player tag" : "Kız oyuncu etiketini aç/kapat";
    public string MaleToggleTooltip => L10n.IsEnglish ? "Toggle male player tag" : "Erkek oyuncu etiketini aç/kapat";

    public string FemaleToggleText => IsFemale ? (L10n.IsEnglish ? "♀ Female" : "♀ Kız Oyuncu") : (L10n.IsEnglish ? "♀ Tag Female" : "♀ Kız Ekle");
    public string FemaleToggleColor => IsFemale ? "#ff4b82" : "#94a3b8";
    public string FemaleActionBg => IsFemale ? "#38142b" : "#181e30";
    public string FemaleActionBorder => IsFemale ? "#ff4b82" : "#2e3852";

    public string MaleToggleText => IsMale ? (L10n.IsEnglish ? "♂ Male" : "♂ Erkek Oyuncu") : (L10n.IsEnglish ? "♂ Tag Male" : "♂ Erkek Ekle");
    public string MaleToggleColor => IsMale ? "#38bdf8" : "#94a3b8";
    public string MaleActionBg => IsMale ? "#102540" : "#181e30";
    public string MaleActionBorder => IsMale ? "#38bdf8" : "#2e3852";

    public string BlacklistToggleText => IsBlacklisted ? (L10n.IsEnglish ? "Blocked" : "Engelli") : (L10n.IsEnglish ? "Block" : "Engelle");
    public string BlacklistToggleColor => IsBlacklisted ? "#ef4444" : "#94a3b8";
    public string BlacklistActionBg => IsBlacklisted ? "#331414" : "#181e30";
    public string BlacklistActionBorder => IsBlacklisted ? "#ef4444" : "#2e3852";

    public string CardBackground => IsFemale ? "#2b1424" : "#14192b";

    public string CardBorderBrush
    {
        get
        {
            if (IsBlacklisted) return "#ef4444";
            if (IsFemale) return "#ff4b82";
            if (IsMale) return "#38bdf8";
            if (HasNote) return "#f59e0b";
            if (TotalEncounters >= 2) return "#00d2ff";
            return "#242e4c";
        }
    }

    private bool _showMatches;
    public bool ShowMatches
    {
        get => _showMatches;
        set
        {
            if (_showMatches == value) return;
            _showMatches = value;
            OnPropertyChanged(nameof(ShowMatches));
            OnPropertyChanged(nameof(MatchesVisibility));
            OnPropertyChanged(nameof(MatchesToggleIcon));
        }
    }

    public Visibility MatchesVisibility => _showMatches ? Visibility.Visible : Visibility.Collapsed;
    public string MatchesToggleIcon => _showMatches ? "▲" : "▼";
    public string MatchesButtonText => (L10n.IsEnglish ? "Matches" : "Maçlar") + $" ({Matches.Count})";

    public List<PlayerMatchDisplayItem> Matches { get; set; } = new();
    public bool HasMatches => Matches != null && Matches.Count > 0;
    public Visibility HasMatchesVisibility => HasMatches ? Visibility.Visible : Visibility.Collapsed;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public partial class PlayerListControl : UserControl
{
    private List<DirectoryPlayerItem> _allPlayers = new();
    private List<DirectoryPlayerItem> _filteredQueryResults = new();
    private ObservableCollection<DirectoryPlayerItem> _pagedPlayers = new();
    private const int BATCH_SIZE = 30;
    private bool _isLoadingBatch = false;
    private bool _isRefreshing = false;
    private bool _needsRefresh = true;

    private DirectoryTab _currentTab = DirectoryTab.Female;
    private string _searchFilter = "";
    private int _sortMode = 0; // 0: En Son Görülen, 1: En Çok Karşılaşılan, 2: İsim A-Z
    private bool _isInitialized = false;

    private readonly HashSet<string> _inProgressOrResolvedPuuids = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _puuidLock = new();
    private CancellationTokenSource? _bgResolveCts;
    private bool _isInternalUpdating = false;
    private System.Windows.Threading.DispatcherTimer? _riotStatusTimer;
    private bool? _lastRiotStatus = null;

    public PlayerListControl()
    {
        InitializeComponent();
        _isInitialized = true;

        if (PlayersItemsControl != null)
        {
            PlayersItemsControl.ItemsSource = _pagedPlayers;
        }

        ApplyLocalization();

        IsVisibleChanged += (s, e) =>
        {
            var isVis = Visibility == Visibility.Visible;
            IsHitTestVisible = isVis;
            if (isVis)
            {
                if (_needsRefresh)
                {
                    RefreshDirectory();
                }
                StartRiotStatusTimer();
            }
            else
            {
                try { _bgResolveCts?.Cancel(); } catch { }
                StopRiotStatusTimer();
            }
        };

        Loaded += (s, e) =>
        {
            ApplyLocalization();
            if (Visibility == Visibility.Visible)
            {
                RefreshDirectory();
                StartRiotStatusTimer();
            }
            // Start the initial pulse animation
            try
            {
                var pulse = FindResource("PulseGreen") as System.Windows.Media.Animation.Storyboard;
                pulse?.Begin(this, true);
            }
            catch { }
        };

        // Live react to tag/note changes
        FemaleTagManager.FemaleTagsChanged += () =>
        {
            if (!_isInitialized || _isInternalUpdating) return;
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.InvokeAsync(RefreshDirectory);
            }
            else
            {
                _needsRefresh = true;
            }
        };

        MaleTagManager.MaleTagsChanged += () =>
        {
            if (!_isInitialized || _isInternalUpdating) return;
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.InvokeAsync(RefreshDirectory);
            }
            else
            {
                _needsRefresh = true;
            }
        };

        EncounterTracker.EncountersChanged += () =>
        {
            if (!_isInitialized || _isInternalUpdating) return;
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.InvokeAsync(RefreshDirectory);
            }
            else
            {
                _needsRefresh = true;
            }
        };

        BlacklistManager.BlacklistChanged += () =>
        {
            if (!_isInitialized || _isInternalUpdating) return;
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.InvokeAsync(RefreshDirectory);
            }
            else
            {
                _needsRefresh = true;
            }
        };
    }

    public void ApplyLocalization()
    {
        try
        {
            if (PlayerDirectoryTitleText != null) PlayerDirectoryTitleText.Text = L10n.Get("PlayerDirectoryTitle");
            if (PlayerDirectorySubText != null) PlayerDirectorySubText.Text = L10n.Get("PlayerDirectorySubtitle");
            if (StatTotalLabel != null) StatTotalLabel.Text = L10n.Get("StatTotal");
            if (StatFemaleLabel != null) StatFemaleLabel.Text = L10n.Get("StatFemale");
            if (StatMaleLabel != null) StatMaleLabel.Text = L10n.Get("StatMale");
            if (StatNotedLabel != null) StatNotedLabel.Text = L10n.Get("StatNoted");
            if (StatEncounterLabel != null) StatEncounterLabel.Text = L10n.Get("StatEncounter");
            if (StatBlacklistLabel != null) StatBlacklistLabel.Text = L10n.Get("StatBlacklist");
            if (CloseEscText != null) CloseEscText.Text = L10n.Get("CloseEsc");

            if (TabFemaleTitle != null) TabFemaleTitle.Text = L10n.Get("TabFemale");
            if (TabMaleTitle != null) TabMaleTitle.Text = L10n.Get("TabMale");
            if (TabNotedTitle != null) TabNotedTitle.Text = L10n.Get("TabNoted");
            if (TabEncounterTitle != null) TabEncounterTitle.Text = L10n.Get("TabEncounter");
            if (TabBlacklistTitle != null) TabBlacklistTitle.Text = L10n.Get("TabBlacklist");
            if (TabAllTitle != null) TabAllTitle.Text = L10n.Get("TabAll");

            if (SearchPlaceholder != null) SearchPlaceholder.Text = L10n.Get("SearchPlaceholder");
            if (SortLabel != null) SortLabel.Text = L10n.Get("SortLabel");
            if (SortItemRecent != null) SortItemRecent.Content = L10n.Get("SortRecent");
            if (SortItemCount != null) SortItemCount.Content = L10n.Get("SortCount");
            if (SortItemName != null) SortItemName.Content = L10n.Get("SortName");

            if (LoadMoreBtnText != null) LoadMoreBtnText.Text = L10n.Get("LoadMoreText");
            if (LoadAllBtnText != null) LoadAllBtnText.Text = L10n.Get("LoadAllText");

            if (EmptyStateTip != null)
            {
                EmptyStateTip.Text = L10n.IsEnglish
                    ? "Tip: On the live match screen, click ♀ to tag female players and click the pencil icon to save notes."
                    : "İpucu: Maç ekranındaki oyuncu kartlarında ♀ butonuna basarak kız oyuncuları, kalem simgesine basarak notları kaydedebilirsin.";
            }

            if (_lastRiotStatus.HasValue && RiotStatusText != null)
            {
                RiotStatusText.Text = _lastRiotStatus.Value ? L10n.Get("RiotConnected") : L10n.Get("RiotDisconnected");
            }
        }
        catch { }
    }

    private void UpdateRiotStatus(bool isConnected)
    {
        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (RiotStatusBadge == null || RiotStatusText == null || RiotStatusDot == null) return;

                // Skip redundant updates
                if (_lastRiotStatus.HasValue && _lastRiotStatus.Value == isConnected) return;
                _lastRiotStatus = isConnected;

                // Stop any running pulse animations
                try
                {
                    var oldPulse = FindResource(isConnected ? "PulseRed" : "PulseGreen") as System.Windows.Media.Animation.Storyboard;
                    oldPulse?.Stop(this);
                }
                catch { }

                if (isConnected)
                {
                    RiotStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#112b23"));
                    RiotStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"));
                    RiotStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"));
                    RiotStatusText.Text = L10n.Get("RiotConnected");
                    RiotStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ade80"));
                    RiotStatusBadge.ToolTip = L10n.IsEnglish
                        ? "Riot Client connected. Hidden and missing player names are automatically resolved from server."
                        : "Riot Client bağlı. Gizli ve eksik oyuncu isimleri otomatik olarak sunucudan çözümleniyor.";
                }
                else
                {
                    RiotStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2c141d"));
                    RiotStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e"));
                    RiotStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e"));
                    RiotStatusText.Text = L10n.Get("RiotDisconnected");
                    RiotStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fb7185"));
                    RiotStatusBadge.ToolTip = L10n.IsEnglish
                        ? "Riot Client is not running. Names cannot be resolved live; displaying saved offline data."
                        : "Riot Client / Valorant açık olmadığı için yeni veya gizli isimler Riot sunucularından çekilemiyor. Mevcut kayıtlı veriler gösteriliyor.";
                }

                // Start the appropriate pulse animation
                try
                {
                    var newPulse = FindResource(isConnected ? "PulseGreen" : "PulseRed") as System.Windows.Media.Animation.Storyboard;
                    newPulse?.Begin(this, true);
                }
                catch { }
            }
            catch { }
        });
    }

    private void StartRiotStatusTimer()
    {
        if (_riotStatusTimer != null && _riotStatusTimer.IsEnabled) return;

        _riotStatusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _riotStatusTimer.Tick += async (s, e) =>
        {
            try
            {
                var isOnline = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);
                UpdateRiotStatus(isOnline);
            }
            catch { }
        };
        _riotStatusTimer.Start();

        // Also do an immediate check
        Task.Run(async () =>
        {
            try
            {
                var isOnline = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);
                UpdateRiotStatus(isOnline);
            }
            catch { }
        });
    }

    private void StopRiotStatusTimer()
    {
        _riotStatusTimer?.Stop();
    }

    public void RefreshDirectory()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        Task.Run(async () =>
        {
            try
            {
                var isRiotOnline = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);
                UpdateRiotStatus(isRiotOnline);

                var encounters = EncounterTracker.GetAllRecords();
                var femaleEntries = FemaleTagManager.GetAll();
                var maleEntries = MaleTagManager.GetAll();
                var blacklistEntries = BlacklistManager.GetAll();

                var playerMap = new Dictionary<string, DirectoryPlayerItem>(StringComparer.OrdinalIgnoreCase);
                var usernameMap = new Dictionary<string, DirectoryPlayerItem>(StringComparer.OrdinalIgnoreCase);

                // 1. Process EncounterTracker records
                foreach (var kvp in encounters)
                {
                    var puuidKey = kvp.Key?.Trim() ?? "";
                    if (string.IsNullOrEmpty(puuidKey)) continue;

                    var rec = kvp.Value;
                    if (rec == null) continue;

                    var isFem = FemaleTagManager.IsFemale(puuidKey, rec.Name);
                    var isMale = MaleTagManager.IsMale(puuidKey, rec.Name);
                    var isBl = BlacklistManager.IsBlacklisted(puuidKey, rec.Name);

                    // Ignore dummy / ghost records
                    var isDummyNote = !string.IsNullOrEmpty(rec.Note) &&
                        (rec.Note.Contains("Pro Jett", StringComparison.OrdinalIgnoreCase) ||
                         rec.Note.Contains("Çok iyi aim", StringComparison.OrdinalIgnoreCase));
                    if (isDummyNote) continue;

                    var resolvedName = rec.Name ?? "";
                    if (string.IsNullOrWhiteSpace(resolvedName) || resolvedName == "Bilinmeyen Oyuncu" || resolvedName == "----" || resolvedName == "Gizli Profil")
                    {
                        var known = EncounterTracker.GetKnownPlayerName(puuidKey);
                        if (!string.IsNullOrWhiteSpace(known))
                        {
                            resolvedName = known;
                        }
                    }

                    var hasNoValidName = string.IsNullOrWhiteSpace(resolvedName) || resolvedName.Trim() == "Bilinmeyen Oyuncu";
                    if (hasNoValidName && (rec.Ally + rec.Enemy) == 0 && string.IsNullOrWhiteSpace(rec.Note) && !isFem && !isMale)
                    {
                        continue;
                    }

                    var item = new DirectoryPlayerItem
                    {
                        Puuid = puuidKey,
                        Username = resolvedName,
                        AllyCount = rec.Ally,
                        EnemyCount = rec.Enemy,
                        Note = rec.Note ?? "",
                        IsFemale = isFem,
                        IsMale = isMale,
                        IsBlacklisted = isBl,
                        LastSeenRaw = rec.LastSeen ?? ""
                    };

                    // Parse last seen text
                    if (!string.IsNullOrEmpty(rec.LastSeen))
                    {
                        item.LastSeenText = EncounterTracker.CalculateRelativeTime(rec.LastSeen);
                    }

                    // Process matches
                    if (rec.Matches != null && rec.Matches.Count > 0)
                    {
                        var lastMatch = rec.Matches[0];
                        item.LastSeenMap = MapHelper.ResolveMapName(lastMatch.Map);
                        item.LastSeenAgent = !string.IsNullOrWhiteSpace(lastMatch.AgentName) && lastMatch.AgentName != "Bilinmeyen" ? lastMatch.AgentName : (L10n.IsEnglish ? "Unknown" : "Bilinmeyen");
                        item.LastSeenAgentIcon = EncounterTracker.ResolveAgentIcon(lastMatch.AgentIcon, lastMatch.AgentName);

                        if (string.IsNullOrEmpty(item.LastSeenAgentIcon))
                        {
                            foreach (var m in rec.Matches)
                            {
                                var cand = EncounterTracker.ResolveAgentIcon(m.AgentIcon, m.AgentName);
                                if (!string.IsNullOrEmpty(cand))
                                {
                                    item.LastSeenAgentIcon = cand;
                                    break;
                                }
                            }
                        }

                        foreach (var m in rec.Matches)
                        {
                            item.Matches.Add(new PlayerMatchDisplayItem
                            {
                                TeamText = m.IsAlly ? (L10n.IsEnglish ? "ALLY" : "DOST") : (L10n.IsEnglish ? "ENEMY" : "RAKİP"),
                                TeamColor = m.IsAlly ? "#32e2b2" : "#e55b5b",
                                Map = MapHelper.ResolveMapName(m.Map),
                                AgentName = !string.IsNullOrWhiteSpace(m.AgentName) && m.AgentName != "Bilinmeyen" ? m.AgentName : (L10n.IsEnglish ? "Unknown" : "Bilinmeyen"),
                                Date = m.Date ?? ""
                            });
                        }
                    }

                    playerMap[puuidKey] = item;
                    var cleanU = FemaleTagManager.CleanUsername(rec.Name);
                    if (!string.IsNullOrEmpty(cleanU) && !usernameMap.ContainsKey(cleanU))
                    {
                        usernameMap[cleanU] = item;
                    }
                }

                // 2. Process FemaleTagManager entries
                foreach (var fEntry in femaleEntries)
                {
                    var pClean = FemaleTagManager.CleanPuuid(fEntry.Puuid);
                    var uClean = FemaleTagManager.CleanUsername(fEntry.Username);

                    DirectoryPlayerItem item = null;

                    if (!string.IsNullOrEmpty(pClean) && playerMap.TryGetValue(pClean, out var existingByPuuid))
                    {
                        item = existingByPuuid;
                        item.IsFemale = true;
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrWhiteSpace(uClean))
                            item.Username = uClean;
                    }
                    else if (!string.IsNullOrEmpty(uClean) && usernameMap.TryGetValue(uClean, out var existingByUsername))
                    {
                        item = existingByUsername;
                        item.IsFemale = true;
                    }

                    if (item == null)
                    {
                        // Standalone female entry not yet in encounters
                        var resolvedUsername = uClean;
                        if ((string.IsNullOrWhiteSpace(resolvedUsername) || resolvedUsername == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                resolvedUsername = known;
                        }

                        var newKey = !string.IsNullOrEmpty(pClean) ? pClean : ("fem_" + resolvedUsername);
                        item = new DirectoryPlayerItem
                        {
                            Puuid = pClean,
                            Username = resolvedUsername,
                            IsFemale = true,
                            IsBlacklisted = BlacklistManager.IsBlacklisted(pClean, resolvedUsername),
                            LastSeenRaw = fEntry.AddedAt ?? "",
                            LastSeenText = !string.IsNullOrEmpty(fEntry.AddedAt) ? EncounterTracker.CalculateRelativeTime(fEntry.AddedAt) : (L10n.IsEnglish ? "Tagged" : "Kayıtlı")
                        };
                        playerMap[newKey] = item;
                        if (!string.IsNullOrEmpty(resolvedUsername) && !usernameMap.ContainsKey(resolvedUsername))
                            usernameMap[resolvedUsername] = item;
                    }
                    else
                    {
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                item.Username = known;
                        }
                    }
                }

                // 3. Process MaleTagManager entries
                foreach (var mEntry in maleEntries)
                {
                    var pClean = MaleTagManager.CleanPuuid(mEntry.Puuid);
                    var uClean = MaleTagManager.CleanUsername(mEntry.Username);

                    DirectoryPlayerItem item = null;

                    if (!string.IsNullOrEmpty(pClean) && playerMap.TryGetValue(pClean, out var existingByPuuid))
                    {
                        item = existingByPuuid;
                        item.IsMale = true;
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrWhiteSpace(uClean))
                            item.Username = uClean;
                    }
                    else if (!string.IsNullOrEmpty(uClean) && usernameMap.TryGetValue(uClean, out var existingByUsername))
                    {
                        item = existingByUsername;
                        item.IsMale = true;
                    }

                    if (item == null)
                    {
                        // Standalone male entry not yet in encounters
                        var resolvedUsername = uClean;
                        if ((string.IsNullOrWhiteSpace(resolvedUsername) || resolvedUsername == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                resolvedUsername = known;
                        }

                        var newKey = !string.IsNullOrEmpty(pClean) ? pClean : ("male_" + resolvedUsername);
                        item = new DirectoryPlayerItem
                        {
                            Puuid = pClean,
                            Username = resolvedUsername,
                            IsMale = true,
                            IsBlacklisted = BlacklistManager.IsBlacklisted(pClean, resolvedUsername),
                            LastSeenRaw = mEntry.AddedAt ?? "",
                            LastSeenText = !string.IsNullOrEmpty(mEntry.AddedAt) ? EncounterTracker.CalculateRelativeTime(mEntry.AddedAt) : (L10n.IsEnglish ? "Tagged" : "Kayıtlı")
                        };
                        playerMap[newKey] = item;
                        if (!string.IsNullOrEmpty(resolvedUsername) && !usernameMap.ContainsKey(resolvedUsername))
                            usernameMap[resolvedUsername] = item;
                    }
                    else
                    {
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                item.Username = known;
                        }
                    }
                }

                // 4. Process BlacklistManager entries
                foreach (var bEntry in blacklistEntries)
                {
                    var pClean = BlacklistManager.CleanPuuid(bEntry.Puuid);
                    var uClean = BlacklistManager.CleanUsername(bEntry.Username);

                    DirectoryPlayerItem item = null;

                    if (!string.IsNullOrEmpty(pClean) && playerMap.TryGetValue(pClean, out var existingByPuuid))
                    {
                        item = existingByPuuid;
                        item.IsBlacklisted = true;
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrWhiteSpace(uClean))
                            item.Username = uClean;
                    }
                    else if (!string.IsNullOrEmpty(uClean) && usernameMap.TryGetValue(uClean, out var existingByUsername))
                    {
                        item = existingByUsername;
                        item.IsBlacklisted = true;
                    }

                    if (item == null)
                    {
                        // Standalone blacklist entry not yet in encounters
                        var resolvedUsername = uClean;
                        if ((string.IsNullOrWhiteSpace(resolvedUsername) || resolvedUsername == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                resolvedUsername = known;
                        }

                        var newKey = !string.IsNullOrEmpty(pClean) ? pClean : ("bl_" + resolvedUsername);
                        item = new DirectoryPlayerItem
                        {
                            Puuid = pClean,
                            Username = resolvedUsername,
                            IsBlacklisted = true,
                            IsFemale = FemaleTagManager.IsFemale(pClean, resolvedUsername),
                            IsMale = MaleTagManager.IsMale(pClean, resolvedUsername),
                            LastSeenRaw = bEntry.AddedAt ?? "",
                            LastSeenText = !string.IsNullOrEmpty(bEntry.AddedAt) ? EncounterTracker.CalculateRelativeTime(bEntry.AddedAt) : (L10n.IsEnglish ? "Blocked" : "Engellendi")
                        };
                        playerMap[newKey] = item;
                        if (!string.IsNullOrEmpty(resolvedUsername) && !usernameMap.ContainsKey(resolvedUsername))
                            usernameMap[resolvedUsername] = item;
                    }
                    else
                    {
                        if ((string.IsNullOrWhiteSpace(item.Username) || item.Username == "Bilinmeyen Oyuncu") && !string.IsNullOrEmpty(pClean))
                        {
                            var known = EncounterTracker.GetKnownPlayerName(pClean);
                            if (!string.IsNullOrWhiteSpace(known))
                                item.Username = known;
                        }
                    }
                }

                var playersList = playerMap.Values
                    .Where(p => (!string.IsNullOrWhiteSpace(p.Username) && p.Username != "Bilinmeyen Oyuncu") || p.HasNote || p.IsFemale || p.IsMale || p.IsBlacklisted || p.TotalEncounters > 0)
                    .ToList();

                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        _allPlayers = playersList;
                        _needsRefresh = false;
                        UpdateHeaderStats();
                        ApplyFilter();

                        if (isRiotOnline)
                        {
                            StartBackgroundLazyResolution(playersList);
                        }
                    }
                    catch (Exception ex)
                    {
                        Constants.Log?.Error(ex, "PlayerListControl ApplyFilter failed on Dispatcher");
                    }
                    finally
                    {
                        _isRefreshing = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("PlayerListControl background RefreshDirectory failed: {e}", ex);
                _isRefreshing = false;
            }
        });
    }

    private async Task ResolveItemsAsync(List<DirectoryPlayerItem> items)
    {
        if (items == null || items.Count == 0) return;

        var isOnline = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);
        UpdateRiotStatus(isOnline);
        if (!isOnline) return;

        var toQuery = new List<DirectoryPlayerItem>();
        lock (_puuidLock)
        {
            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.Puuid)) continue;
                if (!Guid.TryParse(it.Puuid, out var g) || g == Guid.Empty) continue;
                if (!string.IsNullOrWhiteSpace(it.Username) && it.Username != "Bilinmeyen Oyuncu" && it.Username.Contains("#"))
                {
                    _inProgressOrResolvedPuuids.Add(it.Puuid.ToLowerInvariant());
                    continue;
                }
                if (_inProgressOrResolvedPuuids.Add(it.Puuid.ToLowerInvariant()))
                {
                    toQuery.Add(it);
                }
            }
        }

        if (toQuery.Count == 0) return;

        try
        {
            _isInternalUpdating = true;
            var guids = toQuery.Select(x => Guid.Parse(x.Puuid)).ToArray();
            var names = await Login.GetNameServiceGetUsernamesAsync(guids).ConfigureAwait(false);
            if (names != null && names.Length == guids.Length)
            {
                var resolvedDict = new Dictionary<string, string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    var rName = names[i];
                    if (!string.IsNullOrWhiteSpace(rName) && rName.Contains("#"))
                    {
                        var target = toQuery[i];
                        await Dispatcher.InvokeAsync(() =>
                        {
                            target.Username = rName;
                        });
                        resolvedDict[target.Puuid] = rName;
                        if (target.IsFemale)
                        {
                            FemaleTagManager.AssociateUsername(target.Puuid, rName);
                        }
                        if (target.IsMale)
                        {
                            MaleTagManager.AssociateUsername(target.Puuid, rName);
                        }
                    }
                }

                if (resolvedDict.Count > 0)
                {
                    EncounterTracker.RecordKnownPlayerNames(resolvedDict, notify: false);
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("ResolveItemsAsync failed: {e}", ex);
        }
        finally
        {
            _isInternalUpdating = false;
        }
    }

    private void QueueImmediateResolution(IEnumerable<DirectoryPlayerItem> visibleItems)
    {
        if (visibleItems == null) return;
        var needed = visibleItems
            .Where(p => (string.IsNullOrWhiteSpace(p.Username) || p.Username == "Bilinmeyen Oyuncu" || !p.Username.Contains("#"))
                        && Guid.TryParse(p.Puuid, out var g) && g != Guid.Empty)
            .ToList();

        if (needed.Count > 0)
        {
            Task.Run(async () =>
            {
                await ResolveItemsAsync(needed).ConfigureAwait(false);
            });
        }
    }

    private void StartBackgroundLazyResolution(List<DirectoryPlayerItem> allPlayers)
    {
        try
        {
            _bgResolveCts?.Cancel();
            _bgResolveCts?.Dispose();
        }
        catch { }

        _bgResolveCts = new CancellationTokenSource();
        var token = _bgResolveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                // Visible cards get initial priority and network bandwidth
                await Task.Delay(2000, token).ConfigureAwait(false);

                var remaining = allPlayers
                    .Where(p => (string.IsNullOrWhiteSpace(p.Username) || p.Username == "Bilinmeyen Oyuncu" || !p.Username.Contains("#"))
                                && Guid.TryParse(p.Puuid, out var g) && g != Guid.Empty)
                    .ToList();

                var batchSize = 15;
                for (int i = 0; i < remaining.Count; i += batchSize)
                {
                    if (token.IsCancellationRequested) break;

                    var isOnline = await Checks.IsRiotConnectedAsync().ConfigureAwait(false);
                    UpdateRiotStatus(isOnline);
                    if (!isOnline) break;

                    var batch = remaining.Skip(i).Take(batchSize).ToList();
                    await ResolveItemsAsync(batch).ConfigureAwait(false);

                    // Aralıklı güncelleme: her paket arasında 3.0 saniye mola
                    await Task.Delay(3000, token).ConfigureAwait(false);
                }

                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try { UpdateHeaderStats(); } catch { }
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Constants.Log?.Error("Background lazy resolution error: {e}", ex);
            }
        }, token);
    }

    private void UpdateHeaderStats()
    {
        if (!_isInitialized || StatTotalText == null || StatFemaleText == null || StatMaleText == null || StatNotedText == null || StatEncounterText == null)
            return;

        var total = _allPlayers.Count;
        var femaleCount = _allPlayers.Count(p => p.IsFemale);
        var maleCount = _allPlayers.Count(p => p.IsMale);
        var notedCount = _allPlayers.Count(p => p.HasNote);
        var encounterCount = _allPlayers.Count(p => p.TotalEncounters > 0);
        var blacklistCount = _allPlayers.Count(p => p.IsBlacklisted);

        StatTotalText.Text = total.ToString();
        StatFemaleText.Text = femaleCount.ToString();
        StatMaleText.Text = maleCount.ToString();
        StatNotedText.Text = notedCount.ToString();
        StatEncounterText.Text = encounterCount.ToString();
        if (StatBlacklistText != null) StatBlacklistText.Text = blacklistCount.ToString();

        TabFemaleBadge.Text = femaleCount.ToString();
        TabMaleBadge.Text = maleCount.ToString();
        TabNotedBadge.Text = notedCount.ToString();
        TabEncounterBadge.Text = encounterCount.ToString();
        if (TabBlacklistBadge != null) TabBlacklistBadge.Text = blacklistCount.ToString();
        TabAllBadge.Text = total.ToString();
    }

    private void ApplyFilter()
    {
        if (!_isInitialized || PlayersItemsControl == null || EmptyStateBorder == null || CardsScrollViewer == null)
            return;

        IEnumerable<DirectoryPlayerItem> query = _allPlayers;

        // 1. Tab Filter
        switch (_currentTab)
        {
            case DirectoryTab.Female:
                query = query.Where(p => p.IsFemale);
                break;
            case DirectoryTab.Male:
                query = query.Where(p => p.IsMale);
                break;
            case DirectoryTab.Noted:
                query = query.Where(p => p.HasNote);
                break;
            case DirectoryTab.Encounter:
                query = query.Where(p => p.TotalEncounters > 0);
                break;
            case DirectoryTab.Blacklist:
                query = query.Where(p => p.IsBlacklisted);
                break;
            case DirectoryTab.All:
            default:
                break;
        }

        // 2. Search Box Filter
        if (!string.IsNullOrWhiteSpace(_searchFilter))
        {
            var filter = _searchFilter.Trim().ToLowerInvariant();
            query = query.Where(p =>
                (p.Username != null && p.Username.ToLowerInvariant().Contains(filter)) ||
                (p.Note != null && p.Note.ToLowerInvariant().Contains(filter)) ||
                (p.LastSeenMap != null && p.LastSeenMap.ToLowerInvariant().Contains(filter)) ||
                (p.LastSeenAgent != null && p.LastSeenAgent.ToLowerInvariant().Contains(filter))
            );
        }

        // 3. Sorting
        switch (_sortMode)
        {
            case 1: // En Çok Karşılaşılan
                query = query.OrderByDescending(p => p.TotalEncounters)
                             .ThenByDescending(p => p.LastSeenRaw);
                break;
            case 2: // İsim (A-Z)
                query = query.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase);
                break;
            case 0: // En Son Görülen
            default:
                query = query.OrderByDescending(p => p.LastSeenRaw);
                break;
        }

        _filteredQueryResults = query.ToList();
        _pagedPlayers.Clear();

        if (CardsScrollViewer != null)
        {
            CardsScrollViewer.ScrollToTop();
        }

        // Empty state handling
        if (_filteredQueryResults.Count == 0)
        {
            EmptyStateBorder.Visibility = Visibility.Visible;
            CardsScrollViewer.Visibility = Visibility.Collapsed;
            if (PagingFooterBorder != null) PagingFooterBorder.Visibility = Visibility.Collapsed;

            switch (_currentTab)
            {
                case DirectoryTab.Female:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_Venus;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff4b82"));
                    EmptyStateTitle.Text = L10n.IsEnglish ? "No Female Players Found" : "Kayıtlı Kız Oyuncu Bulunamadı";
                    EmptyStateDesc.Text = L10n.IsEnglish ? "You can tag female players by clicking the ♀ button on player cards." : "Maçlarında denk geldiğin kız oyuncuları kartların yanındaki ♀ butonuna basarak kaydedebilirsin.";
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: In the live match screen, click ♀ to quickly tag female players." : "İpucu: Maç ekranındaki oyuncu panellerinde engel butonunun solundaki ♀ butonuna basarak anında listeye ekleyebilirsin.";
                    break;
                case DirectoryTab.Male:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_Mars;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
                    EmptyStateTitle.Text = L10n.IsEnglish ? "No Male Players Found" : "Kayıtlı Erkek Oyuncu Bulunamadı";
                    EmptyStateDesc.Text = L10n.IsEnglish ? "You can tag male players by clicking the ♂ button on player cards." : "Maçlarında denk geldiğin erkek oyuncuları kartların yanındaki ♂ butonuna basarak kaydedebilirsin.";
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: In the live match screen, click ♂ to quickly tag male players." : "İpucu: Maç ekranındaki oyuncu panellerinde kız butonunun sağındaki ♂ butonuna basarak anında listeye ekleyebilirsin.";
                    break;
                case DirectoryTab.Noted:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_PenToSquare;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b"));
                    EmptyStateTitle.Text = L10n.IsEnglish ? "No Noted Players Found" : "Not Eklenen Oyuncu Bulunamadı";
                    EmptyStateDesc.Text = L10n.IsEnglish ? "No custom notes have been added for any players yet." : "Henüz hiçbir oyuncu için özel not eklenmemiş.";
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: Select a player from 'Past Encounters' or 'All' to add personal notes." : "İpucu: 'Önceden Denk Geldiklerin' veya 'Tümü' sekmesinden bir oyuncu seçip '+ Not Ekle' diyerek not bırakabilirsin.";
                    break;
                case DirectoryTab.Encounter:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_Users;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
                    EmptyStateTitle.Text = L10n.IsEnglish ? "No Encounters Recorded" : "Henüz Denk Gelinen Oyuncu Yok";
                    EmptyStateDesc.Text = L10n.IsEnglish ? "Players from your matches will automatically be archived in this list." : "Oynadığın maçlardaki oyuncular otomatik olarak bu listeye arşivlenir.";
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: Whenever you enter a live match, players are recorded here." : "İpucu: Canlı bir Valorant maçına girdiğinde sistem oyuncuları tespit edip burada saklar.";
                    break;
                case DirectoryTab.Blacklist:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_Ban;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444"));
                    EmptyStateTitle.Text = L10n.IsEnglish ? "No Blocked Players" : "Engellenen Oyuncu Bulunamadı";
                    EmptyStateDesc.Text = L10n.IsEnglish ? "You have not blocked any players yet." : "Henüz engellediğin bir oyuncu bulunmuyor.";
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: Click the 'Block' button on player cards to add toxic players to this list." : "İpucu: Kartların üzerindeki veya maç ekranındaki 'Engelle' butonuna basarak toksik veya istemediğin oyuncuları bu listeye alabilirsin.";
                    break;
                case DirectoryTab.All:
                default:
                    EmptyStateIcon.Icon = EFontAwesomeIcon.Solid_MagnifyingGlass;
                    EmptyStateIcon.PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"));
                    EmptyStateTitle.Text = L10n.Get("EmptyStateTitle");
                    EmptyStateDesc.Text = string.IsNullOrWhiteSpace(_searchFilter)
                        ? L10n.Get("EmptyStateDesc")
                        : (L10n.IsEnglish ? $"No players found matching '{_searchFilter}'." : $"'{_searchFilter}' aramasına uyan hiçbir oyuncu bulunamadı.");
                    EmptyStateTip.Text = L10n.IsEnglish ? "Tip: Clear the search box to view all players." : "İpucu: Arama kutusunu temizleyerek tüm listeyi tekrar görüntüleyebilirsin.";
                    break;
            }
        }
        else
        {
            EmptyStateBorder.Visibility = Visibility.Collapsed;
            CardsScrollViewer.Visibility = Visibility.Visible;
            LoadNextBatch(BATCH_SIZE);
        }
    }

    private void LoadNextBatch(int count = BATCH_SIZE)
    {
        if (_isLoadingBatch) return;
        _isLoadingBatch = true;

        try
        {
            var currentCount = _pagedPlayers.Count;
            var totalCount = _filteredQueryResults.Count;

            if (currentCount >= totalCount)
            {
                UpdatePagingFooter();
                return;
            }

            var nextBatch = _filteredQueryResults.Skip(currentCount).Take(count).ToList();
            foreach (var item in nextBatch)
            {
                _pagedPlayers.Add(item);
            }

            QueueImmediateResolution(nextBatch);

            UpdatePagingFooter();
        }
        finally
        {
            _isLoadingBatch = false;
        }
    }

    private void UpdatePagingFooter()
    {
        if (PagingFooterBorder == null || PagingStatusText == null) return;

        var current = _pagedPlayers.Count;
        var total = _filteredQueryResults.Count;

        if (total == 0)
        {
            PagingFooterBorder.Visibility = Visibility.Collapsed;
        }
        else if (current >= total)
        {
            PagingFooterBorder.Visibility = Visibility.Visible;
            PagingStatusText.Text = string.Format(L10n.Get("PagingStatusFormat"), total, total) + " ✓";
            if (LoadMoreBtn != null) LoadMoreBtn.Visibility = Visibility.Collapsed;
            if (LoadAllBtn != null) LoadAllBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            PagingFooterBorder.Visibility = Visibility.Visible;
            PagingStatusText.Text = string.Format(L10n.Get("PagingStatusFormat"), current, total);
            if (LoadMoreBtn != null) LoadMoreBtn.Visibility = Visibility.Visible;
            if (LoadAllBtn != null) LoadAllBtn.Visibility = Visibility.Visible;
        }
    }

    private void CardsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isLoadingBatch) return;
        if (e.VerticalChange <= 0) return;

        // If user scrolled near bottom (within 300px)
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 300)
        {
            if (_pagedPlayers.Count < _filteredQueryResults.Count)
            {
                LoadNextBatch(BATCH_SIZE);
            }
        }
    }

    private void LoadMoreBtn_Click(object sender, RoutedEventArgs e)
    {
        LoadNextBatch(BATCH_SIZE);
    }

    private void LoadAllBtn_Click(object sender, RoutedEventArgs e)
    {
        LoadNextBatch(_filteredQueryResults.Count - _pagedPlayers.Count);
    }

    #region Tab Selection Handlers

    private void SetActiveTab(DirectoryTab tab)
    {
        if (!_isInitialized || TabFemaleBorder == null) return;
        _currentTab = tab;

        // Reset all tab styling
        ResetTabStyle(TabFemaleBorder, TabFemaleTitle, TabFemaleBadge, "#ff4b82");
        ResetTabStyle(TabMaleBorder, TabMaleTitle, TabMaleBadge, "#38bdf8");
        ResetTabStyle(TabNotedBorder, TabNotedTitle, TabNotedBadge, "#f59e0b");
        ResetTabStyle(TabEncounterBorder, TabEncounterTitle, TabEncounterBadge, "#00d2ff");
        ResetTabStyle(TabBlacklistBorder, TabBlacklistTitle, TabBlacklistBadge, "#ef4444");
        ResetTabStyle(TabAllBorder, TabAllTitle, TabAllBadge, "#38bdf8");

        // Highlight active tab
        switch (tab)
        {
            case DirectoryTab.Female:
                HighlightTabStyle(TabFemaleBorder, TabFemaleTitle, TabFemaleBadge, "#2e1226", "#ff4b82", "#ff8cb1");
                break;
            case DirectoryTab.Male:
                HighlightTabStyle(TabMaleBorder, TabMaleTitle, TabMaleBadge, "#102540", "#38bdf8", "#7dd3fc");
                break;
            case DirectoryTab.Noted:
                HighlightTabStyle(TabNotedBorder, TabNotedTitle, TabNotedBadge, "#261c0d", "#f59e0b", "#fcd34d");
                break;
            case DirectoryTab.Encounter:
                HighlightTabStyle(TabEncounterBorder, TabEncounterTitle, TabEncounterBadge, "#0d2436", "#00d2ff", "#7dd3fc");
                break;
            case DirectoryTab.Blacklist:
                HighlightTabStyle(TabBlacklistBorder, TabBlacklistTitle, TabBlacklistBadge, "#2b1418", "#ef4444", "#fca5a5");
                break;
            case DirectoryTab.All:
                HighlightTabStyle(TabAllBorder, TabAllTitle, TabAllBadge, "#17233d", "#38bdf8", "#bae6fd");
                break;
        }

        ApplyFilter();
    }

    private void ResetTabStyle(Border border, TextBlock title, TextBlock badge, string themeColor)
    {
        if (border == null || title == null || badge == null) return;
        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161c2e"));
        border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2e3b5e"));
        border.BorderThickness = new Thickness(1);
        title.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8e9bb5"));
        title.FontWeight = FontWeights.SemiBold;
        badge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8e9bb5"));
    }

    private void HighlightTabStyle(Border border, TextBlock title, TextBlock badge, string bgHex, string borderHex, string textHex)
    {
        if (border == null || title == null || badge == null) return;
        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex));
        border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex));
        border.BorderThickness = new Thickness(1.5);
        title.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textHex));
        title.FontWeight = FontWeights.Bold;
        badge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex));
    }

    private void TabFemale_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.Female);
    private void TabMale_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.Male);
    private void TabNoted_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.Noted);
    private void TabEncounter_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.Encounter);
    private void TabBlacklist_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.Blacklist);
    private void TabAll_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(DirectoryTab.All);

    #endregion

    #region Search & Sort Handlers

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized || SearchTextBox == null) return;
        _searchFilter = SearchTextBox.Text;
        if (SearchPlaceholder != null)
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchFilter) ? Visibility.Visible : Visibility.Collapsed;
        if (ClearSearchBtn != null)
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(_searchFilter) ? Visibility.Collapsed : Visibility.Visible;
        ApplyFilter();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        if (SearchTextBox != null)
        {
            SearchTextBox.Text = "";
            SearchTextBox.Focus();
        }
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || SortComboBox == null) return;
        _sortMode = SortComboBox.SelectedIndex;
        ApplyFilter();
    }

    #endregion

    #region Card Action Handlers

    private async void CardCopyUsername_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null || string.IsNullOrWhiteSpace(item.Username)) return;

        var clean = item.Username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
        var success = await ClipboardHelper.SetTextAsync(clean);

        if (btn.Content is TextBlock tb)
        {
            var oldText = tb.Text;
            tb.Text = success ? "✓" : "❌";
            await Task.Delay(1400);
            tb.Text = oldText;
        }
    }

    private void CardToggleFemale_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        var newStatus = !item.IsFemale;
        FemaleTagManager.SetFemale(item.Puuid, item.Username, newStatus);
        item.IsFemale = newStatus;
        if (newStatus && item.IsMale)
        {
            item.IsMale = false;
        }

        UpdateHeaderStats();
        if (_currentTab == DirectoryTab.Female && !newStatus)
        {
            ApplyFilter();
        }
        else if (_currentTab == DirectoryTab.Male && newStatus)
        {
            ApplyFilter();
        }
    }

    private void CardToggleMale_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        var newStatus = !item.IsMale;
        MaleTagManager.SetMale(item.Puuid, item.Username, newStatus);
        item.IsMale = newStatus;
        if (newStatus && item.IsFemale)
        {
            item.IsFemale = false;
        }

        UpdateHeaderStats();
        if (_currentTab == DirectoryTab.Male && !newStatus)
        {
            ApplyFilter();
        }
        else if (_currentTab == DirectoryTab.Female && newStatus)
        {
            ApplyFilter();
        }
    }

    private void CardToggleBlacklist_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        var newStatus = !item.IsBlacklisted;
        BlacklistManager.SetBlacklisted(item.Puuid, item.Username, newStatus);
        item.IsBlacklisted = newStatus;
    }

    private void CardEditNote_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        item.EditNoteText = item.Note ?? "";
        item.IsEditingNote = true;
    }

    private void CardSaveNote_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        var newNote = (item.EditNoteText ?? "").Trim();
        EncounterTracker.SetNote(item.Puuid, newNote, item.Username);
        item.Note = newNote;
        item.IsEditingNote = false;

        UpdateHeaderStats();
        if (_currentTab == DirectoryTab.Noted)
        {
            ApplyFilter();
        }
    }

    private void CardDeleteNote_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        EncounterTracker.SetNote(item.Puuid, "", item.Username);
        item.Note = "";
        item.EditNoteText = "";
        item.IsEditingNote = false;

        UpdateHeaderStats();
        if (_currentTab == DirectoryTab.Noted)
        {
            ApplyFilter();
        }
    }

    private void CardCancelNote_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        item.EditNoteText = item.Note ?? "";
        item.IsEditingNote = false;
    }

    private void CardToggleMatches_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        item.ShowMatches = !item.ShowMatches;
    }

    private void CardDeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var item = btn?.Tag as DirectoryPlayerItem;
        if (item == null) return;

        var cmStyle = FindResource("ModernContextMenu") as Style;
        var miStyle = FindResource("ModernMenuItem") as Style;
        var sepStyle = FindResource("ModernMenuSeparator") as Style;

        var cm = new ContextMenu
        {
            PlacementTarget = btn,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            Style = cmStyle,
            ItemContainerStyle = miStyle
        };

        bool hasSpecificOption = false;

        // 1. Notu Sil (HasNote == true)
        if (item.HasNote)
        {
            var mi = new MenuItem
            {
                Header = L10n.IsEnglish ? "Delete Note" : "Notu Sil",
                Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_PenToSquare, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24")), Width = 12, Height = 12 },
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24")),
                Style = miStyle
            };
            mi.Click += (s, args) =>
            {
                EncounterTracker.SetNote(item.Puuid, "", item.Username);
                item.Note = "";
                item.EditNoteText = "";
                item.IsEditingNote = false;
                UpdateHeaderStats();
                if (_currentTab == DirectoryTab.Noted)
                {
                    ApplyFilter();
                }
            };
            cm.Items.Add(mi);
            hasSpecificOption = true;
        }

        // 2. Karşılaşma Geçmişini Sıfırla (TotalEncounters > 0 veya Matches > 0)
        if (item.TotalEncounters > 0 || (item.Matches != null && item.Matches.Count > 0))
        {
            var mi = new MenuItem
            {
                Header = L10n.IsEnglish ? "Reset Match History" : "Karşılaşma Geçmişini Sıfırla",
                Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_Rotate, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8")), Width = 12, Height = 12 },
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8")),
                Style = miStyle
            };
            mi.Click += (s, args) =>
            {
                EncounterTracker.ClearMatchHistory(item.Puuid);
                item.AllyCount = 0;
                item.EnemyCount = 0;
                item.Matches?.Clear();
                item.OnPropertyChanged(nameof(item.TotalEncounters));
                item.OnPropertyChanged(nameof(item.TotalEncountersText));
                item.OnPropertyChanged(nameof(item.EncounterBreakdown));
                item.OnPropertyChanged(nameof(item.EncounterBadgeVisibility));
                item.OnPropertyChanged(nameof(item.HasMatchesVisibility));
                item.OnPropertyChanged(nameof(item.CardBorderBrush));
                UpdateHeaderStats();
                if (_currentTab == DirectoryTab.Encounter)
                {
                    ApplyFilter();
                }
            };
            cm.Items.Add(mi);
            hasSpecificOption = true;
        }

        // 3. Kız Etiketini Kaldır (IsFemale == true)
        if (item.IsFemale)
        {
            var mi = new MenuItem
            {
                Header = L10n.IsEnglish ? "Remove Female Tag" : "Kız Etiketini Kaldır",
                Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_Venus, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff4b82")), Width = 12, Height = 12 },
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff4b82")),
                Style = miStyle
            };
            mi.Click += (s, args) =>
            {
                FemaleTagManager.SetFemale(item.Puuid, item.Username, false);
                item.IsFemale = false;
                UpdateHeaderStats();
                if (_currentTab == DirectoryTab.Female)
                {
                    ApplyFilter();
                }
            };
            cm.Items.Add(mi);
            hasSpecificOption = true;
        }

        // 4. Erkek Etiketini Kaldır (IsMale == true)
        if (item.IsMale)
        {
            var mi = new MenuItem
            {
                Header = L10n.IsEnglish ? "Remove Male Tag" : "Erkek Etiketini Kaldır",
                Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_Mars, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8")), Width = 12, Height = 12 },
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8")),
                Style = miStyle
            };
            mi.Click += (s, args) =>
            {
                MaleTagManager.SetMale(item.Puuid, item.Username, false);
                item.IsMale = false;
                UpdateHeaderStats();
                if (_currentTab == DirectoryTab.Male)
                {
                    ApplyFilter();
                }
            };
            cm.Items.Add(mi);
            hasSpecificOption = true;
        }

        // 5. Engeli Kaldır (IsBlacklisted == true)
        if (item.IsBlacklisted)
        {
            var mi = new MenuItem
            {
                Header = L10n.IsEnglish ? "Remove Block" : "Engeli Kaldır",
                Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_Ban, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e55b5b")), Width = 12, Height = 12 },
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e55b5b")),
                Style = miStyle
            };
            mi.Click += (s, args) =>
            {
                BlacklistManager.SetBlacklisted(item.Puuid, item.Username, false);
                item.IsBlacklisted = false;
                UpdateHeaderStats();
                if (_currentTab == DirectoryTab.Blacklist)
                {
                    ApplyFilter();
                }
            };
            cm.Items.Add(mi);
            hasSpecificOption = true;
        }

        if (hasSpecificOption)
        {
            cm.Items.Add(new Separator { Style = sepStyle });
        }

        // 6. Tüm Verilerini Sil (Her zaman - Onay Dialogu ile)
        var miAll = new MenuItem
        {
            Header = L10n.IsEnglish ? "Delete All Data..." : "Tüm Verilerini Sil...",
            Icon = new ImageAwesome { Icon = EFontAwesomeIcon.Solid_TrashCan, PrimaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")), Width = 12, Height = 12 },
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
            FontWeight = FontWeights.Bold,
            Style = miStyle
        };
        miAll.Click += (s, args) =>
        {
            var result = MessageBox.Show(
                L10n.IsEnglish
                    ? $"All saved data (encounters, note, female/male tag, and block) for '{item.DisplayName}' will be permanently deleted.\n\nAre you sure?"
                    : $"'{item.DisplayName}' adlı oyuncuya ait kayıtlı tüm veriler (karşılaşmalar, not, kız/erkek etiketi ve engel) kalıcı olarak silinecek.\n\nEmin misiniz?",
                L10n.IsEnglish ? "Delete All Data" : "Tüm Verileri Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                _isInternalUpdating = true;
                try
                {
                    EncounterTracker.RemoveRecord(item.Puuid);
                    FemaleTagManager.SetFemale(item.Puuid, item.Username, false);
                    MaleTagManager.SetMale(item.Puuid, item.Username, false);
                    BlacklistManager.SetBlacklisted(item.Puuid, item.Username, false);

                    item.Note = "";
                    item.EditNoteText = "";
                    item.IsEditingNote = false;
                    item.IsFemale = false;
                    item.IsMale = false;
                    item.IsBlacklisted = false;
                    item.AllyCount = 0;
                    item.EnemyCount = 0;
                    item.Matches?.Clear();

                    item.OnPropertyChanged(nameof(item.TotalEncounters));
                    item.OnPropertyChanged(nameof(item.TotalEncountersText));
                    item.OnPropertyChanged(nameof(item.EncounterBreakdown));
                    item.OnPropertyChanged(nameof(item.EncounterBadgeVisibility));
                    item.OnPropertyChanged(nameof(item.HasMatchesVisibility));
                    item.OnPropertyChanged(nameof(item.CardBorderBrush));

                    _allPlayers.Remove(item);
                    _filteredQueryResults.Remove(item);
                    _pagedPlayers.Remove(item);

                    UpdateHeaderStats();
                    ApplyFilter();
                }
                finally
                {
                    _isInternalUpdating = false;
                }
            }
        };
        cm.Items.Add(miAll);

        cm.IsOpen = true;
    }

    #endregion

    #region Window Close & Backdrop Handlers

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }

    private void Backdrop_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender)
        {
            Visibility = Visibility.Collapsed;
        }
    }

    private void InnerBorder_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    #endregion
}

