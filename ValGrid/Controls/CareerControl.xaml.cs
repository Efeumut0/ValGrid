using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ValGrid.Helpers;
using ValGrid.Objects;

namespace ValGrid.Controls;

public partial class CareerControl : UserControl
{
    private MatchHistoryPlayerItem _editingPlayer;
    private MatchHistoryItem _selectedMatch;
    private bool _needsRefresh = true;
    private List<MatchHistoryItem> _allMatches = new();
    private bool _isControlLoaded = false;

    public CareerControl()
    {
        InitializeComponent();
        _isControlLoaded = true;

        ApplyLocalization();

        IsVisibleChanged += (s, e) =>
        {
            var isVis = Visibility == Visibility.Visible;
            IsHitTestVisible = isVis;
            if (isVis && _needsRefresh)
            {
                RefreshMatches();
                _ = MatchHistoryManager.UpdatePendingMatchesAsync();
            }
        };

        Loaded += CareerControl_Loaded;
        AddHandler(InventoryControl.CloseButtonEvent, new RoutedEventHandler(CloseInventoryModal));
        MatchHistoryManager.MatchHistoryUpdated += () =>
        {
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.InvokeAsync(RefreshMatches);
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
            if (CareerTitleText != null) CareerTitleText.Text = L10n.Get("CareerTitle");
            if (CareerSubText != null) CareerSubText.Text = L10n.Get("CareerSubtitle");
            if (MatchCountLabel != null) MatchCountLabel.Text = L10n.Get("CareerMatchCount");
            if (PlayerCountLabel != null) PlayerCountLabel.Text = L10n.Get("CareerTotalPlayers");
            if (WinRateLabel != null) WinRateLabel.Text = L10n.Get("CareerWinRate");
            if (CareerCloseEscText != null) CareerCloseEscText.Text = L10n.Get("CloseEsc");

            if (MatchesHeaderLabel != null) MatchesHeaderLabel.Text = L10n.Get("CareerPlayedMatches");
            if (CareerSearchPlaceholder != null) CareerSearchPlaceholder.Text = L10n.Get("CareerSearchPlaceholder");

            if (FilterAllRadio != null) FilterAllRadio.Content = L10n.Get("CareerFilterAll");
            if (FilterWinRadio != null) FilterWinRadio.Content = L10n.Get("CareerFilterWin");
            if (FilterLossRadio != null) FilterLossRadio.Content = L10n.Get("CareerFilterLoss");

            if (NoMatchesPrompt != null) NoMatchesPrompt.Text = L10n.Get("CareerNoMatchesPrompt");

            if (MatchTotalValueLabel != null) MatchTotalValueLabel.Text = L10n.IsEnglish ? "Total Inventory Value: " : "Maçın Toplam Envanter Değeri: ";
            if (EquippedOnlyLabel != null) EquippedOnlyLabel.Text = L10n.IsEnglish ? " (EQUIPPED ONLY)" : " (SADECE TAKILI OLANLAR)";
            if (OurTeamTitleText != null) OurTeamTitleText.Text = L10n.IsEnglish ? "OUR TEAM" : "BİZİM TAKIM";
            if (EnemyTeamTitleText != null) EnemyTeamTitleText.Text = L10n.IsEnglish ? "ENEMY TEAM" : "KARŞI TAKIM";
        }
        catch { }
    }

    private void CareerControl_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        if (Visibility == Visibility.Visible)
        {
            RefreshMatches();
            _ = MatchHistoryManager.UpdatePendingMatchesAsync();
        }
    }

    public void RefreshMatches()
    {
        _needsRefresh = false;
        try
        {
            var matches = MatchHistoryManager.GetMatches() ?? new List<MatchHistoryItem>();
            foreach (var m in matches)
            {
                if (!string.IsNullOrEmpty(m.MapName) && m.MapName != "Bilinmeyen Harita")
                {
                    m.MapName = MapHelper.ResolveMapName(m.MapName);
                }
                else if (!string.IsNullOrEmpty(m.MapImage))
                {
                    var resolvedFromImg = MapHelper.ResolveMapNameFromImage(m.MapImage);
                    if (!string.IsNullOrEmpty(resolvedFromImg) && resolvedFromImg != "Bilinmeyen Harita")
                        m.MapName = resolvedFromImg;
                }

                if (string.IsNullOrEmpty(m.MapImage) && !string.IsNullOrEmpty(m.MapName))
                {
                    var img = MapHelper.ResolveMapImage(m.MapName);
                    if (img != null)
                        m.MapImage = img.ToString();
                }
            }

            _allMatches = matches;

            if (MatchCountText != null)
                MatchCountText.Text = matches.Count.ToString();

            var allPlayerCount = matches.SelectMany(m => m.Players ?? Enumerable.Empty<MatchHistoryPlayerItem>())
                                        .Select(p => p.Puuid)
                                        .Where(id => id != Guid.Empty)
                                        .Distinct()
                                        .Count();
            if (PlayerCountText != null)
                PlayerCountText.Text = allPlayerCount.ToString();

            // Win rate calculation
            var wins = _allMatches.Count(m => m.Result == "GALİBİYET" || (m.Result ?? "").IndexOf("GALİBİYET", StringComparison.OrdinalIgnoreCase) >= 0 || (m.Result ?? "").IndexOf("VICTORY", StringComparison.OrdinalIgnoreCase) >= 0);
            var losses = _allMatches.Count(m => m.Result == "MAĞLUBİYET" || (m.Result ?? "").IndexOf("MAĞLUBİYET", StringComparison.OrdinalIgnoreCase) >= 0 || (m.Result ?? "").IndexOf("DEFEAT", StringComparison.OrdinalIgnoreCase) >= 0);
            var totalFinished = wins + losses;
            if (WinRateText != null)
            {
                if (totalFinished > 0)
                {
                    var wr = (int)Math.Round((double)wins / totalFinished * 100);
                    WinRateText.Text = L10n.IsEnglish ? $"{wr}% ({wins}W - {losses}L)" : $"%{wr} ({wins}G - {losses}M)";
                }
                else
                {
                    WinRateText.Text = "-";
                }
            }

            ApplyCareerFilter();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "CareerControl.RefreshMatches failed");
        }
    }

    private void ApplyCareerFilter()
    {
        if (!_isControlLoaded || _allMatches == null || MatchListBox == null) return;

        var query = (CareerSearchBox?.Text ?? "").Trim().ToLowerInvariant();
        bool onlyWins = FilterWinRadio?.IsChecked == true;
        bool onlyLosses = FilterLossRadio?.IsChecked == true;

        if (ClearCareerSearchBtn != null)
        {
            ClearCareerSearchBtn.Visibility = string.IsNullOrWhiteSpace(query) ? Visibility.Collapsed : Visibility.Visible;
        }

        var filtered = _allMatches.Where(m =>
        {
            if (onlyWins && !(m.Result == "GALİBİYET" || (m.Result ?? "").IndexOf("GALİBİYET", StringComparison.OrdinalIgnoreCase) >= 0 || (m.Result ?? "").IndexOf("VICTORY", StringComparison.OrdinalIgnoreCase) >= 0))
                return false;
            if (onlyLosses && !(m.Result == "MAĞLUBİYET" || (m.Result ?? "").IndexOf("MAĞLUBİYET", StringComparison.OrdinalIgnoreCase) >= 0 || (m.Result ?? "").IndexOf("DEFEAT", StringComparison.OrdinalIgnoreCase) >= 0))
                return false;

            if (!string.IsNullOrEmpty(query))
            {
                var mapMatch = (m.MapName ?? "").ToLowerInvariant().Contains(query);
                var modeMatch = (m.GameMode ?? "").ToLowerInvariant().Contains(query);
                var agentMatch = (m.MyAgentName ?? "").ToLowerInvariant().Contains(query);
                var playerMatch = m.Players != null && m.Players.Any(p => (p.Username ?? "").ToLowerInvariant().Contains(query) || (p.AgentName ?? "").ToLowerInvariant().Contains(query));

                if (!mapMatch && !modeMatch && !agentMatch && !playerMatch)
                    return false;
            }

            return true;
        }).ToList();

        if (FilteredCountText != null)
        {
            FilteredCountText.Text = (filtered.Count != _allMatches.Count) ? $"({filtered.Count}/{_allMatches.Count})" : "";
        }

        MatchListBox.ItemsSource = null;
        MatchListBox.ItemsSource = filtered;

        if (filtered.Count == 0)
        {
            if (NoMatchesPrompt != null) NoMatchesPrompt.Visibility = Visibility.Visible;
            ClearDetails();
        }
        else
        {
            if (NoMatchesPrompt != null) NoMatchesPrompt.Visibility = Visibility.Collapsed;
            if (MatchListBox.SelectedItem == null || !filtered.Contains(MatchListBox.SelectedItem))
            {
                MatchListBox.SelectedIndex = 0;
            }
        }
    }

    private void CareerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isControlLoaded) return;
        ApplyCareerFilter();
    }

    private void ClearCareerSearch_Click(object sender, RoutedEventArgs e)
    {
        if (!_isControlLoaded) return;
        if (CareerSearchBox != null)
        {
            CareerSearchBox.Text = "";
        }
    }

    private void CareerFilter_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isControlLoaded) return;
        ApplyCareerFilter();
    }

    private void MatchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (MatchListBox.SelectedItem is MatchHistoryItem match)
            {
                DisplayMatchDetails(match);
            }
            else
            {
                ClearDetails();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "MatchListBox_SelectionChanged failed");
        }
    }

    private void DisplayMatchDetails(MatchHistoryItem match)
    {
        _selectedMatch = match;
        try
        {
            SelectedMapTitle.Text = match.MapName ?? "-";
            SelectedModeTitle.Text = match.GameMode ?? "-";
            SelectedDateTitle.Text = match.Date ?? "-";
            SelectedTotalValueText.Text = match.TotalInventoryCombinedText ?? "0 VP (~0 TL)";

            SelectedResultBadge.Text = match.Result ?? "OYNANDI";
            try
            {
                var colorStr = !string.IsNullOrEmpty(match.ResultColor) ? match.ResultColor : "#32e2b2";
                SelectedResultBadge.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr)
                );
            }
            catch
            {
                SelectedResultBadge.Foreground = System.Windows.Media.Brushes.White;
            }
            SelectedScoreText.Text = match.Score ?? "-";

            var allies = match.Allies?.ToList() ?? new List<MatchHistoryPlayerItem>();
            var enemies = match.Enemies?.ToList() ?? new List<MatchHistoryPlayerItem>();

            foreach (var p in allies.Concat(enemies))
            {
                var stats = EncounterTracker.GetEncounterStats(p.Puuid);
                p.EncounterCount = stats.Total;
                p.EncounterBadgeText = stats.Total > 1
                    ? (L10n.IsEnglish ? $"🔁 {stats.Total}x Met" : $"🔁 {stats.Total}x Denk Geldik")
                    : (stats.Total == 1 ? (L10n.IsEnglish ? "1x Met" : "1x Denk Geldik") : "");
                p.EncounterTooltip = stats.Total > 0
                    ? (L10n.IsEnglish
                        ? $"You encountered this player {stats.Total} time(s).\n({stats.Ally}x Friendly, {stats.Enemy}x Enemy)"
                        : $"Bu oyuncuyla toplam {stats.Total} kez karşılaştınız.\n({stats.Ally}x Dost Takım, {stats.Enemy}x Rakip Takım)")
                    : (L10n.IsEnglish ? "No other encounters with this player." : "Bu oyuncuyla başka karşılaşma kaydı bulunmuyor.");
                p.IsBlacklisted = BlacklistManager.IsBlacklisted(p.Puuid, p.Username);
                p.IsFemale = FemaleTagManager.IsFemale(p.Puuid, p.Username);
                p.IsMale = MaleTagManager.IsMale(p.Puuid, p.Username);
            }

            OurTeamItemsControl.ItemsSource = allies;
            var totalPrefix = L10n.IsEnglish ? "Total: " : "Toplam: ";
            OurTeamValueText.Text = $"{totalPrefix}{CurrencyHelper.FormatVpAndTl(match.AllyTotalVp)}";

            var hasEnemies = enemies != null && enemies.Count > 0;
            if (EnemyTeamHeaderBorder != null)
                EnemyTeamHeaderBorder.Visibility = hasEnemies ? Visibility.Visible : Visibility.Collapsed;
            if (EnemyTeamItemsControl != null)
                EnemyTeamItemsControl.Visibility = hasEnemies ? Visibility.Visible : Visibility.Collapsed;

            EnemyTeamItemsControl.ItemsSource = enemies;
            EnemyTeamValueText.Text = $"{totalPrefix}{CurrencyHelper.FormatVpAndTl(match.EnemyTotalVp)}";
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "DisplayMatchDetails failed");
        }
    }

    private void ClearDetails()
    {
        _selectedMatch = null;
        SelectedMapTitle.Text = "-";
        SelectedModeTitle.Text = "-";
        SelectedDateTitle.Text = "-";
        SelectedTotalValueText.Text = "0 VP (~0 TL)";
        SelectedResultBadge.Text = "-";
        SelectedScoreText.Text = "-";
        OurTeamItemsControl.ItemsSource = null;
        EnemyTeamItemsControl.ItemsSource = null;
        if (EnemyTeamHeaderBorder != null)
            EnemyTeamHeaderBorder.Visibility = Visibility.Visible;
        if (EnemyTeamItemsControl != null)
            EnemyTeamItemsControl.Visibility = Visibility.Visible;
        OurTeamValueText.Text = "Toplam: 0 VP";
        EnemyTeamValueText.Text = "Toplam: 0 VP";
    }

    private void DeleteCurrentMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMatch == null || string.IsNullOrEmpty(_selectedMatch.MatchId)) return;
        var map = _selectedMatch.MapName ?? "Seçili maç";
        var date = _selectedMatch.Date ?? "";
        var result = MessageBox.Show(
            $"{map} ({date}) maçını geçmişten silmek istediğinize emin misiniz?",
            "Maçı Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        if (result == MessageBoxResult.Yes)
        {
            MatchHistoryManager.DeleteMatch(_selectedMatch.MatchId);
            RefreshMatches();
        }
    }

    private void DeleteContextMatch_Click(object sender, RoutedEventArgs e)
    {
        var mi = sender as MenuItem;
        var match = mi?.Tag as MatchHistoryItem;
        if (match == null || string.IsNullOrEmpty(match.MatchId)) return;
        var map = match.MapName ?? "Seçili maç";
        var date = match.Date ?? "";
        var result = MessageBox.Show(
            $"{map} ({date}) maçını geçmişten silmek istediğinize emin misiniz?",
            "Maçı Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        if (result == MessageBoxResult.Yes)
        {
            MatchHistoryManager.DeleteMatch(match.MatchId);
            RefreshMatches();
        }
    }

    private void EditPlayerNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MatchHistoryPlayerItem player)
        {
            _editingPlayer = player;
            NoteModalPlayerName.Text = $"{player.Username} ({player.AgentName})";
            NoteInputBox.Text = player.Note ?? "";
            NoteEditModal.Visibility = Visibility.Visible;
            NoteInputBox.Focus();
            NoteInputBox.SelectAll();
        }
    }

    private void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        if (_editingPlayer != null)
        {
            var noteText = NoteInputBox.Text?.Trim() ?? "";
            MatchHistoryManager.UpdatePlayerNote(_editingPlayer.Puuid, noteText);
            _editingPlayer.Note = noteText;
        }

        NoteEditModal.Visibility = Visibility.Collapsed;
        _editingPlayer = null;
    }

    private void CancelNote_Click(object sender, RoutedEventArgs e)
    {
        NoteEditModal.Visibility = Visibility.Collapsed;
        _editingPlayer = null;
    }

    private void OpenInventory_Click(object sender, RoutedEventArgs e)
    {
        var fe = sender as FrameworkElement;
        var player = fe?.Tag as MatchHistoryPlayerItem ?? fe?.DataContext as MatchHistoryPlayerItem;
        if (player == null) return;

        OpenPlayerInventory(player);
        e.Handled = true;
    }

    private void Weapon_Click(object sender, MouseButtonEventArgs e)
    {
        var fe = sender as FrameworkElement;
        var player = fe?.DataContext as MatchHistoryPlayerItem;
        if (player == null)
        {
            var parent = VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent is FrameworkElement parentFe && parentFe.DataContext is MatchHistoryPlayerItem p)
                {
                    player = p;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        if (player != null)
        {
            OpenPlayerInventory(player);
            e.Handled = true;
        }
    }

    public void OpenPlayerInventory(MatchHistoryPlayerItem player)
    {
        if (player == null) return;
        var skinData = player.GetEffectiveSkinData();
        var displayName = $"{player.Username} ({player.AgentName})";
        var inv = new InventoryControl(skinData, displayName);
        InventoryModalHost.Content = inv;
        InventoryModalOverlay.Visibility = Visibility.Visible;
    }

    private void CloseInventoryModal(object sender, RoutedEventArgs e)
    {
        InventoryModalOverlay.Visibility = Visibility.Collapsed;
        InventoryModalHost.Content = null;
        if (e != null) e.Handled = true;
    }

    private void InventoryBackdrop_Click(object sender, MouseButtonEventArgs e)
    {
        CloseInventoryModal(this, null);
        e.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape)
        {
            if (InventoryModalOverlay != null && InventoryModalOverlay.Visibility == Visibility.Visible)
            {
                CloseInventoryModal(this, null);
                e.Handled = true;
                return;
            }
            if (NoteEditModal != null && NoteEditModal.Visibility == Visibility.Visible)
            {
                NoteEditModal.Visibility = Visibility.Collapsed;
                e.Handled = true;
                return;
            }
            Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }

    private void Backdrop_Click(object sender, MouseButtonEventArgs e)
    {
        if (InventoryModalOverlay != null && InventoryModalOverlay.Visibility == Visibility.Visible)
        {
            CloseInventoryModal(this, null);
            return;
        }
        if (NoteEditModal.Visibility == Visibility.Visible)
        {
            NoteEditModal.Visibility = Visibility.Collapsed;
            return;
        }
        if (e.OriginalSource == sender)
        {
            Visibility = Visibility.Collapsed;
        }
    }

    private void InnerBorder_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void CareerFemale_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = s?.DataContext as MatchHistoryPlayerItem;
        if (player != null)
        {
            player.IsFemale = !player.IsFemale;
            FemaleTagManager.SetFemale(player.Puuid, player.Username, player.IsFemale);
            if (player.IsFemale && player.IsMale)
            {
                player.IsMale = false;
            }
        }
        e.Handled = true;
    }

    private void CareerMale_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = s?.DataContext as MatchHistoryPlayerItem;
        if (player != null)
        {
            player.IsMale = !player.IsMale;
            MaleTagManager.SetMale(player.Puuid, player.Username, player.IsMale);
            if (player.IsMale && player.IsFemale)
            {
                player.IsFemale = false;
            }
        }
        e.Handled = true;
    }

    private void CareerBlacklist_Click(object sender, MouseButtonEventArgs e)
    {
        var s = sender as FrameworkElement;
        var player = s?.DataContext as MatchHistoryPlayerItem;
        if (player != null)
        {
            player.IsBlacklisted = !player.IsBlacklisted;
            BlacklistManager.SetBlacklisted(player.Puuid, player.Username, player.IsBlacklisted);
        }
        e.Handled = true;
    }
}

