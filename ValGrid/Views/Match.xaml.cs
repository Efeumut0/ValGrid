using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ValGrid.Helpers;
using ValGrid.Objects;
using ValGrid.ViewModels;

namespace ValGrid.Views;

/// <summary>
///     Interaction logic for Match.xaml
/// </summary>
public partial class Match : UserControl
{
    public Match()
    {
        InitializeComponent();
        DataContextChanged += DataContextChangedHandler;
        Loaded += (s, e) =>
        {
            var win = Window.GetWindow(this);
            if (win != null)
            {
                win.PreviewKeyDown += (ws, we) =>
                {
                    if (we.Key == System.Windows.Input.Key.Escape)
                    {
                        if (PlayerListOverlay != null && PlayerListOverlay.Visibility == Visibility.Visible)
                        {
                            PlayerListOverlay.Visibility = Visibility.Collapsed;
                            we.Handled = true;
                            return;
                        }

                        if (CareerOverlay != null && CareerOverlay.Visibility == Visibility.Visible)
                        {
                            CareerOverlay.Visibility = Visibility.Collapsed;
                            we.Handled = true;
                            return;
                        }
                    }
                };
            }

            ApplyLocalization();

            if (DataContext is MatchViewModel vm)
            {
                UpdateMatchLeaderboard(vm);
            }
        };
    }

    private void ApplyLocalization()
    {
        try
        {
            if (GoHome != null) GoHome.ToolTip = L10n.Get("HomeTitle");
            if (MatchSettingsBtn != null) MatchSettingsBtn.ToolTip = L10n.Get("NavSettingsTooltip");
            if (MatchRefreshingInLabel != null) MatchRefreshingInLabel.Text = L10n.Get("RefreshingIn");
            if (MatchValueLabelText != null) MatchValueLabelText.Text = L10n.Get("MatchValueLabel");
            if (MatchEquippedOnlyText != null) MatchEquippedOnlyText.Text = L10n.Get("MatchEquippedOnly");
            if (MatchLeaderboardTitleText != null) MatchLeaderboardTitleText.Text = L10n.Get("MatchLeaderboardBadge");
            if (MatchBlacklistAlertTitleText != null) MatchBlacklistAlertTitleText.Text = L10n.Get("MatchBlacklistBadge");
            if (MatchPlayerListText != null) MatchPlayerListText.Text = L10n.Get("ListButton");
            if (MatchPlayerListBtn != null) MatchPlayerListBtn.ToolTip = L10n.Get("ListToolTip");
            if (MatchCareerText != null) MatchCareerText.Text = L10n.Get("CareerButton");
            if (MatchCareerBtn != null) MatchCareerBtn.ToolTip = L10n.Get("CareerToolTip");
            if (CopyLeaderboardBtn != null) CopyLeaderboardBtn.ToolTip = L10n.Get("MatchLeaderboardTooltip");
            if (BlacklistAlertBadge != null) BlacklistAlertBadge.ToolTip = L10n.Get("MatchBlacklistTooltip");
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "ApplyLocalization failed in Match");
        }
    }

    private void OpenPlayerList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (PlayerListOverlay != null)
            {
                PlayerListOverlay.RefreshDirectory();
                PlayerListOverlay.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenPlayerList_Click failed in Match");
        }
    }

    private void OpenCareer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CareerOverlay != null)
            {
                CareerOverlay.RefreshMatches();
                CareerOverlay.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "OpenCareer_Click failed in Match");
        }
    }

    private void DataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
    {
        var viewModel = e.NewValue as MatchViewModel;

        if (viewModel == null)
            return;

        viewModel.GoHomeEvent += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (GoHome.Command.CanExecute(null))
                    GoHome.Command.Execute(null);
            });
        };

        viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(MatchViewModel.LeftPlayerList) ||
                args.PropertyName == nameof(MatchViewModel.RightPlayerList) ||
                args.PropertyName == nameof(MatchViewModel.Match))
            {
                Dispatcher.Invoke(() => UpdateMatchLeaderboard(viewModel));
            }
        };

        UpdateMatchLeaderboard(viewModel);
    }

    private bool _isCopyingMatch = false;
    private bool _isCopyingServer = false;

    private async void CopyMatchSummary_Click(object sender, RoutedEventArgs e)
    {
        if (_isCopyingMatch) return;
        _isCopyingMatch = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm == null || vm.Match == null) return;

            var text = BuildChatSummary(vm);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyMatchIcon != null && CopyMatchSummaryBtn != null)
            {
                var allPlayers = new List<Player>();
                if (vm.LeftPlayerList != null) allPlayers.AddRange(vm.LeftPlayerList);
                if (vm.RightPlayerList != null) allPlayers.AddRange(vm.RightPlayerList);
                var count = allPlayers.Count(p => p.IsBlacklisted);

                CopyMatchIcon.Text = success ? "✓" : "❌";
                CopyMatchSummaryBtn.ToolTip = success
                    ? (count > 0 ? $"Maç Sonu Mesajı Kopyalandı! ({count} oyuncu eklendi)" : "Maç Sonu Mesajı Kopyalandı!")
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyMatchIcon.Text = "📋";
                CopyMatchSummaryBtn.ToolTip = "Sol Tık: Maç Sonu Mesajını Kopyala\nSağ Tık: Maç Başlangıç Mesajını Kopyala";
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Error("CopyMatchSummary_Click failed: {e}", ex);
        }
        finally
        {
            _isCopyingMatch = false;
        }
    }

    private async void CopyMatchSummary_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isCopyingMatch) return;
        _isCopyingMatch = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm == null || vm.Match == null) return;

            var text = BuildMatchStartSummary(vm);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyMatchIcon != null && CopyMatchSummaryBtn != null)
            {
                CopyMatchIcon.Text = success ? "✓" : "❌";
                CopyMatchSummaryBtn.ToolTip = success
                    ? "Maç Başlangıç Mesajı Kopyalandı!"
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyMatchIcon.Text = "📋";
                CopyMatchSummaryBtn.ToolTip = "Sol Tık: Maç Sonu Mesajını Kopyala\nSağ Tık: Maç Başlangıç Mesajını Kopyala";
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Constants.Log.Error("CopyMatchSummary_RightClick failed: {e}", ex);
        }
        finally
        {
            _isCopyingMatch = false;
        }
    }

    private async void CopyServerInfo_Click(object sender, RoutedEventArgs e)
    {
        if (_isCopyingServer) return;
        _isCopyingServer = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm?.Match == null) return;

            var text = BuildServerInfo(vm.Match, singleLine: false);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyServerIcon != null && CopyServerInfoBtn != null)
            {
                CopyServerIcon.Text = success ? "✓" : "❌";
                CopyServerInfoBtn.ToolTip = success
                    ? "Sunucu Bilgileri (Alt Alta) Kopyalandı!"
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyServerIcon.Text = "📋";
                CopyServerInfoBtn.ToolTip = "Sol Tık: Alt Alta Kopyala\nSağ Tık: Tek Satırda Kopyala";
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Warning("CopyServerInfo_Click failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingServer = false;
        }
    }

    private async void CopyServerInfo_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isCopyingServer) return;
        _isCopyingServer = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm?.Match == null) return;

            var text = BuildServerInfo(vm.Match, singleLine: true);
            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyServerIcon != null && CopyServerInfoBtn != null)
            {
                CopyServerIcon.Text = success ? "✓" : "❌";
                CopyServerInfoBtn.ToolTip = success
                    ? "Sunucu Bilgileri (Tek Satır) Kopyalandı!"
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyServerIcon.Text = "📋";
                CopyServerInfoBtn.ToolTip = "Sol Tık: Alt Alta Kopyala\nSağ Tık: Tek Satırda Kopyala";
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Constants.Log.Warning("CopyServerInfo_RightClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingServer = false;
        }
    }

    private bool _isCopyingLeaderboard = false;

    private async void CopyLeaderboard_Click(object sender, RoutedEventArgs e)
    {
        if (_isCopyingLeaderboard) return;
        _isCopyingLeaderboard = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm == null) return;

            UpdateMatchLeaderboard(vm);
            var text = vm.Match?.LeaderboardChatText;
            if (string.IsNullOrWhiteSpace(text)) return;

            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyLeaderboardIcon != null && CopyLeaderboardBtn != null)
            {
                CopyLeaderboardIcon.Text = success ? "✓" : "❌";
                CopyLeaderboardBtn.ToolTip = success
                    ? "Liderlik Metni Kopyalandı!"
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyLeaderboardIcon.Text = "📋";
                CopyLeaderboardBtn.ToolTip = "Sol Tık: Liderlik Metnini Kopyala (Tek Satır)\nSağ Tık: Liderlik Metnini Kopyala (Alt Alta Liste)";
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "CopyLeaderboard_Click failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingLeaderboard = false;
        }
    }

    private async void CopyLeaderboard_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isCopyingLeaderboard) return;
        _isCopyingLeaderboard = true;

        try
        {
            var vm = DataContext as MatchViewModel;
            if (vm == null) return;

            UpdateMatchLeaderboard(vm);
            var text = vm.Match?.LeaderboardMultiLineText;
            if (string.IsNullOrWhiteSpace(text)) return;

            var success = await ClipboardHelper.SetTextAsync(text);

            if (CopyLeaderboardIcon != null && CopyLeaderboardBtn != null)
            {
                CopyLeaderboardIcon.Text = success ? "✓" : "❌";
                CopyLeaderboardBtn.ToolTip = success
                    ? "Liderlik Listesi (Alt Alta) Kopyalandı!"
                    : "Kopyalama başarısız (Pano meşgul)";

                await Task.Delay(1800);
                CopyLeaderboardIcon.Text = "📋";
                CopyLeaderboardBtn.ToolTip = "Sol Tık: Liderlik Metnini Kopyala (Tek Satır)\nSağ Tık: Liderlik Metnini Kopyala (Alt Alta Liste)";
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "CopyLeaderboard_RightClick failed: {e}", ex.Message);
        }
        finally
        {
            _isCopyingLeaderboard = false;
        }
    }

    public static string FormatPlayerLeaderboardIdentity(Player player, string myTeamId = null, MatchViewModel vm = null)
    {
        if (player == null) return "Oyuncu(0VP)";

        var username = player.IgnData?.Username;
        var cleanUser = (!string.IsNullOrWhiteSpace(username) && username != "----")
            ? username.Replace(" 🔗", "").Replace(" 👁", "").Trim()
            : "";

        var vp = player.SkinData?.TotalInventoryValueVp ?? 0;

        if (!string.IsNullOrWhiteSpace(cleanUser))
        {
            return $"{cleanUser}({vp}VP)";
        }

        // Kullanıcı adı gizliyse Bizim / Sizin + Ajan Adı
        bool isOurTeam = false;
        if (!string.IsNullOrEmpty(myTeamId) && !string.IsNullOrEmpty(player.TeamId))
        {
            isOurTeam = (player.TeamId == myTeamId);
        }
        else if (vm != null)
        {
            if (vm.LeftPlayerList != null && vm.LeftPlayerList.Contains(player))
                isOurTeam = true;
            else if (vm.RightPlayerList != null && vm.RightPlayerList.Contains(player))
                isOurTeam = false;
        }
        else if (player.PlayerUiData != null && player.PlayerUiData.Puuid == Constants.Ppuuid)
        {
            isOurTeam = true;
        }

        var teamPrefix = isOurTeam ? "Bizim" : "Sizin";
        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name
            : "Oyuncu";

        return $"{teamPrefix} {agentName}({vp}VP)";
    }

    public static void UpdateMatchLeaderboard(MatchViewModel vm)
    {
        if (vm == null || vm.Match == null) return;

        var allPlayers = new List<Player>();
        if (vm.LeftPlayerList != null) allPlayers.AddRange(vm.LeftPlayerList);
        if (vm.RightPlayerList != null) allPlayers.AddRange(vm.RightPlayerList);

        if (allPlayers.Count == 0)
        {
            vm.Match.LeaderboardVisibility = Visibility.Collapsed;
            vm.Match.BlacklistAlertVisibility = Visibility.Collapsed;
            return;
        }

        vm.Match.RecalculateTeamRatios();

        // Check for blacklisted players in the lobby
        foreach (var p in allPlayers)
        {
            p.RefreshBlacklistStatus();
        }

        var blacklistedPlayers = allPlayers
            .Where(p => p.IsBlacklisted ||
                        (p.PlayerUiData != null && BlacklistManager.IsBlacklisted(p.PlayerUiData.Puuid, p.IgnData?.Username)) ||
                        (!string.IsNullOrWhiteSpace(p.IgnData?.Username) && BlacklistManager.IsBlacklisted("", p.IgnData.Username)))
            .ToList();

        if (blacklistedPlayers.Count > 0)
        {
            vm.Match.HasBlacklistedPlayerInMatch = true;
            vm.Match.BlacklistAlertVisibility = Visibility.Visible;
            vm.Match.BlacklistAlertText = blacklistedPlayers.Count == 1
                ? "Lobide 1 kara listedeki oyuncu tespit edildi!"
                : $"Lobide {blacklistedPlayers.Count} kara listedeki oyuncu tespit edildi!";

            var alertLines = new List<string>
            {
                $"⛔ DİKKAT: Lobide {blacklistedPlayers.Count} Engelli Oyuncu Bulundu!",
                "────────────────────────────────────────"
            };

            foreach (var bp in blacklistedPlayers)
            {
                var uname = (!string.IsNullOrWhiteSpace(bp.IgnData?.Username) && bp.IgnData.Username != "----")
                    ? bp.IgnData.Username.Replace(" 🔗", "").Replace(" 👁", "").Trim()
                    : bp.IdentityData?.Name ?? "Bilinmeyen Oyuncu";
                var note = bp.EncounterData?.Note;
                var reason = !string.IsNullOrWhiteSpace(note) ? $" (Not: {note})" : "";
                alertLines.Add($"• {uname}{reason}");
            }

            alertLines.Add("────────────────────────────────────────");
            alertLines.Add("Bu oyuncuları daha önce kara listeye eklediniz.");
            vm.Match.BlacklistAlertTooltip = string.Join(Environment.NewLine, alertLines);
        }
        else
        {
            vm.Match.HasBlacklistedPlayerInMatch = false;
            vm.Match.BlacklistAlertVisibility = Visibility.Collapsed;
            vm.Match.BlacklistAlertText = "";
            vm.Match.BlacklistAlertTooltip = "";
        }

        var myPlayer = allPlayers.FirstOrDefault(p => p.PlayerUiData != null && p.PlayerUiData.Puuid == Constants.Ppuuid);
        var myTeamId = myPlayer?.TeamId;

        // Sort all players descending by inventory value
        var sortedPlayers = allPlayers
            .OrderByDescending(p => p.SkinData?.TotalInventoryValueVp ?? 0)
            .ToList();

        // Check if anyone has skins
        var hasAnySkins = sortedPlayers.Any(p => (p.SkinData?.TotalInventoryValueVp ?? 0) > 0);
        if (!hasAnySkins)
        {
            vm.Match.LeaderboardVisibility = Visibility.Visible;
            vm.Match.LeaderboardTopSummary = "Özel skin bulunmuyor";
            vm.Match.LeaderboardPreviewText = "Tüm oyuncular standart silah kullanıyor";
            vm.Match.LeaderboardTooltip = "👑 Sırasıyla Liderlik:\nÖzel skinli oyuncu bulunmuyor";
            vm.Match.LeaderboardChatText = "Sırasıyla liderlik: Özel skin bulunmuyor";
            vm.Match.LeaderboardMultiLineText = "Sırasıyla liderlik:\nÖzel skin bulunmuyor";
            return;
        }

        vm.Match.LeaderboardVisibility = Visibility.Visible;

        // Kullanıcının tercihi: İlk 7 kişiyi listele ve kopyala
        var topCount = Math.Min(7, sortedPlayers.Count);
        var topPlayers = sortedPlayers.Take(topCount).ToList();

        var formattedEntries = new List<string>();
        var tooltipLines = new List<string>
        {
            $"👑 Sırasıyla En Değerli Envanterler (İlk {topCount}):",
            "────────────────────────────────────────"
        };

        for (int i = 0; i < topPlayers.Count; i++)
        {
            var p = topPlayers[i];
            var entry = FormatPlayerLeaderboardIdentity(p, myTeamId, vm);
            formattedEntries.Add(entry);

            var vp = p.SkinData?.TotalInventoryValueVp ?? 0;
            var tlText = CurrencyHelper.FormatTl(vp);
            tooltipLines.Add($"{i + 1}. {entry} (~{tlText})");
        }

        tooltipLines.Add("────────────────────────────────────────");
        tooltipLines.Add("💡 Sol Tık: Tek satır kopyala (Chat)");
        tooltipLines.Add("💡 Sağ Tık: Alt alta liste kopyala");

        vm.Match.LeaderboardTopSummary = formattedEntries[0];

        if (formattedEntries.Count > 1)
        {
            vm.Match.LeaderboardPreviewText = string.Join("   |   ", formattedEntries.Skip(1));
        }
        else
        {
            vm.Match.LeaderboardPreviewText = "Başka özel skinli oyuncu bulunmuyor";
        }

        vm.Match.LeaderboardTooltip = string.Join(Environment.NewLine, tooltipLines);
        vm.Match.LeaderboardChatText = ChatTemplateManager.FormatLeaderboard(formattedEntries, multiLine: false);
        vm.Match.LeaderboardMultiLineText = ChatTemplateManager.FormatLeaderboard(formattedEntries, multiLine: true);
    }

    public static string BuildServerInfo(MatchDetails m, bool singleLine = false)
    {
        if (m == null) return "";
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(m.ServerRegion))
            parts.Add($"Sunucu Bölgesi: {m.ServerRegion}");
        if (!string.IsNullOrWhiteSpace(m.ServerPodId))
            parts.Add($"Sunucu Pod: {m.ServerPodId}");
        if (!string.IsNullOrWhiteSpace(m.ServerIpPort))
            parts.Add($"IP & Port: {m.ServerIpPort}");

        var pingText = m.ServerPing;
        if ((string.IsNullOrWhiteSpace(pingText) || pingText.StartsWith("--")) && !string.IsNullOrWhiteSpace(m.ServerPodId))
        {
            var fallbackPing = ServerHelper.GetPingForPod(m.ServerPodId);
            if (fallbackPing > 0)
            {
                pingText = $"{fallbackPing} ms";
                m.ServerPing = pingText;
                m.ServerPingColor = ServerHelper.GetPingColor(fallbackPing);
            }
        }

        if (!string.IsNullOrWhiteSpace(pingText))
            parts.Add($"Gecikme (Ping): {pingText}");

        return singleLine ? string.Join(" | ", parts) : string.Join(Environment.NewLine, parts);
    }

    public static string FormatPlayerWealthIdentity(Player player, string myTeamId, MatchViewModel vm = null, bool capitalizeTeam = false)
    {
        if (player == null) return "oyuncu";

        var username = player.IgnData?.Username;
        if (!string.IsNullOrWhiteSpace(username) && username != "----")
        {
            var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                return clean;
        }

        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name
            : "oyuncu";

        bool isOurTeam = false;
        if (!string.IsNullOrEmpty(myTeamId) && !string.IsNullOrEmpty(player.TeamId))
        {
            isOurTeam = (player.TeamId == myTeamId);
        }
        else if (vm != null)
        {
            if (vm.LeftPlayerList != null && vm.LeftPlayerList.Contains(player))
                isOurTeam = true;
            else if (vm.RightPlayerList != null && vm.RightPlayerList.Contains(player))
                isOurTeam = false;
        }
        else if (player.PlayerUiData != null && player.PlayerUiData.Puuid == Constants.Ppuuid)
        {
            isOurTeam = true;
        }

        if (capitalizeTeam)
        {
            var teamPrefix = isOurTeam ? "Bizim Takım" : "Sizin Takım";
            return $"{teamPrefix} {agentName}";
        }
        else
        {
            var teamPrefix = isOurTeam ? "bizim" : "sizin";
            return $"{teamPrefix} {agentName.ToLowerInvariant()}";
        }
    }

    private static bool GetMatchSummaryData(
        MatchViewModel vm,
        out int totalVp,
        out string totalTl,
        out string richestEndChatText,
        out string richestStartChatText)
    {
        totalVp = 0;
        totalTl = "";
        richestEndChatText = "";
        richestStartChatText = "";

        if (vm == null || vm.Match == null) return false;

        var allPlayers = new List<Player>();
        if (vm.LeftPlayerList != null) allPlayers.AddRange(vm.LeftPlayerList);
        if (vm.RightPlayerList != null) allPlayers.AddRange(vm.RightPlayerList);

        var myPlayer = allPlayers.FirstOrDefault(p => p.PlayerUiData != null && p.PlayerUiData.Puuid == Constants.Ppuuid);
        var myTeamId = myPlayer?.TeamId;

        totalVp = vm.Match.LobbyTotalValueVp > 0
            ? vm.Match.LobbyTotalValueVp
            : allPlayers.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
        totalTl = CurrencyHelper.FormatTlCompact(totalVp);

        var rankedPlayers = allPlayers
            .Where(p => (p.SkinData?.TotalInventoryValueVp ?? 0) > 0)
            .OrderByDescending(p => p.SkinData?.TotalInventoryValueVp ?? 0)
            .ToList();

        if (rankedPlayers.Count >= 2)
        {
            var p1 = rankedPlayers[0];
            var p2 = rankedPlayers[1];

            var who1 = FormatPlayerWealthIdentity(p1, myTeamId, vm, capitalizeTeam: false);
            var vp1Text = CurrencyHelper.FormatVp(p1.SkinData.TotalInventoryValueVp);

            var who2 = FormatPlayerWealthIdentity(p2, myTeamId, vm, capitalizeTeam: false);
            var vp2Text = CurrencyHelper.FormatVp(p2.SkinData.TotalInventoryValueVp);

            richestEndChatText = $"En zenginler ise 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text}) idi";
            richestStartChatText = $"En zenginler ise 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text})";
        }
        else if (rankedPlayers.Count == 1)
        {
            var p1 = rankedPlayers[0];
            var who1 = FormatPlayerWealthIdentity(p1, myTeamId, vm, capitalizeTeam: false);
            var vp1Text = CurrencyHelper.FormatVp(p1.SkinData.TotalInventoryValueVp);

            richestEndChatText = $"En zenginse 1. {who1} ({vp1Text}) idi";
            richestStartChatText = $"En zenginse 1. {who1} ({vp1Text})";
        }

        return true;
    }

    public static string BuildChatSummary(MatchViewModel vm)
    {
        return ChatTemplateManager.FormatMatchEnd(vm);
    }

    public static string FormatCalloutName(Player player, string myTeamId, MatchViewModel vm = null)
    {
        if (player == null) return "oyuncu";

        var username = player.IgnData?.Username;
        if (!string.IsNullOrWhiteSpace(username) && username != "----")
        {
            var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                return clean;
        }

        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name
            : "oyuncu";

        bool isOurTeam = false;
        if (!string.IsNullOrEmpty(myTeamId) && !string.IsNullOrEmpty(player.TeamId))
        {
            isOurTeam = (player.TeamId == myTeamId);
        }
        else if (vm != null)
        {
            if (vm.LeftPlayerList != null && vm.LeftPlayerList.Contains(player))
                isOurTeam = true;
            else if (vm.RightPlayerList != null && vm.RightPlayerList.Contains(player))
                isOurTeam = false;
        }
        else if (player.PlayerUiData != null && player.PlayerUiData.Puuid == Constants.Ppuuid)
        {
            isOurTeam = true;
        }

        var teamPrefix = isOurTeam ? "bizim" : "sizin";
        return $"{teamPrefix} {agentName}";
    }

    public static string BuildMatchStartSummary(MatchViewModel vm)
    {
        return ChatTemplateManager.FormatMatchStart(vm);
    }

    public static string BuildMatchSummary(MatchViewModel vm)
    {
        if (vm == null || vm.Match == null) return "";

        var allPlayers = new List<Player>();
        if (vm.LeftPlayerList != null) allPlayers.AddRange(vm.LeftPlayerList);
        if (vm.RightPlayerList != null) allPlayers.AddRange(vm.RightPlayerList);

        var myPlayer = allPlayers.FirstOrDefault(p => p.PlayerUiData != null && p.PlayerUiData.Puuid == Constants.Ppuuid);
        var myTeamId = myPlayer?.TeamId;

        var line1 = $"Maç Değeri: {vm.Match.LobbyTotalValueVpText} (~{vm.Match.LobbyTotalValueTlText})";

        var rankedPlayers = allPlayers
            .Where(p => (p.SkinData?.TotalInventoryValueVp ?? 0) > 0)
            .OrderByDescending(p => p.SkinData?.TotalInventoryValueVp ?? 0)
            .Take(3)
            .ToList();

        if (rankedPlayers.Count == 0)
            return line1;

        var lines = new List<string> { line1, "En Değerli Envanterler:" };
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            var p = rankedPlayers[i];
            var name = FormatPlayerWealthIdentity(p, myTeamId, vm, capitalizeTeam: true);
            var vpFormatted = p.SkinData?.TotalValueFormatted ?? CurrencyHelper.FormatVp(0);
            var tlFormatted = p.SkinData?.TotalValueTlFormatted ?? CurrencyHelper.FormatTl(0);
            lines.Add($"{i + 1}. {name} - {vpFormatted} (~{tlFormatted})");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

