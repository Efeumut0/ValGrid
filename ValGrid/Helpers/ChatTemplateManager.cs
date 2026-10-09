using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ValGrid.Objects;
using ValGrid.ViewModels;

namespace ValGrid.Helpers;

public enum ChatTemplateType
{
    MatchStart,
    MatchEnd,
    Leaderboard
}

public class ChatTemplatesData
{
    public string? MatchStartTr { get; set; }
    public string? MatchEndTr { get; set; }
    public string? LeaderboardTr { get; set; }

    public string? MatchStartEn { get; set; }
    public string? MatchEndEn { get; set; }
    public string? LeaderboardEn { get; set; }
}

/// <summary>
/// Manages customizable chat templates for Match Start, Match End, and Leaderboard.
/// Supports dynamic placeholders like {totalVp}, {totalTl}, {richest}, {richest1}, {richest2}, {blacklist}, {leaderboard}.
/// Persists customized templates in %LocalAppData%\ValGrid\chat_templates.json with TR/EN language fallback.
/// </summary>
public static class ChatTemplateManager
{
    private static readonly object Lock = new();
    private static ChatTemplatesData _data = new();
    private static bool _loaded;

    public const string DefaultMatchStartTr = "Bu maçın toplam envanter değeri (SADECE TAKILI OLANLAR) {totalVp}vp ve {totalTl}{richest}, herkese iyi oyunlar dilerim ♥";
    public const string DefaultMatchEndTr = "Bu maçın toplam envanter değeri (SADECE TAKILI OLANLAR) {totalVp}vp ve {totalTl}{richest}, İyi günler diler ve Ez Yazanların anasını sikiyim ♥{blacklist}";
    public const string DefaultLeaderboardTr = "Sırasıyla liderlik: {leaderboard}";

    public const string DefaultMatchStartEn = "Total inventory value for this match (EQUIPPED ONLY) {totalVp}vp and {totalTl}{richest}, have a great game everyone ♥";
    public const string DefaultMatchEndEn = "Total inventory value for this match (EQUIPPED ONLY) {totalVp}vp and {totalTl}{richest}, have a nice day and fuck whoever types Ez ♥{blacklist}";
    public const string DefaultLeaderboardEn = "Inventory leaderboard: {leaderboard}";

    private static string GetConfigFilePath()
    {
        var localDir = string.IsNullOrEmpty(Constants.LocalAppDataPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValGrid")
            : Constants.LocalAppDataPath;

        if (!Directory.Exists(localDir))
        {
            Directory.CreateDirectory(localDir);
        }

        return Path.Combine(localDir, "chat_templates.json");
    }

    private static void EnsureLoaded()
    {
        lock (Lock)
        {
            if (_loaded) return;
            try
            {
                var filePath = GetConfigFilePath();
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };
                        _data = JsonSerializer.Deserialize<ChatTemplatesData>(json, options) ?? new ChatTemplatesData();
                    }
                    else
                    {
                        _data = new ChatTemplatesData();
                    }
                }
                else
                {
                    _data = new ChatTemplatesData();
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Error(ex, "Failed to load chat_templates.json, using defaults.");
                try
                {
                    var filePath = GetConfigFilePath();
                    if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                    {
                        File.Copy(filePath, filePath + ".corrupt.bak", true);
                    }
                }
                catch { }
                _data = new ChatTemplatesData();
            }
            finally
            {
                _loaded = true;
            }
        }
    }

    public static string GetTemplate(ChatTemplateType type, bool isEnglish)
    {
        EnsureLoaded();
        lock (Lock)
        {
            if (isEnglish)
            {
                return type switch
                {
                    ChatTemplateType.MatchStart => !string.IsNullOrWhiteSpace(_data.MatchStartEn) ? _data.MatchStartEn : DefaultMatchStartEn,
                    ChatTemplateType.MatchEnd => !string.IsNullOrWhiteSpace(_data.MatchEndEn) ? _data.MatchEndEn : DefaultMatchEndEn,
                    ChatTemplateType.Leaderboard => !string.IsNullOrWhiteSpace(_data.LeaderboardEn) ? _data.LeaderboardEn : DefaultLeaderboardEn,
                    _ => ""
                };
            }
            else
            {
                return type switch
                {
                    ChatTemplateType.MatchStart => !string.IsNullOrWhiteSpace(_data.MatchStartTr) ? _data.MatchStartTr : DefaultMatchStartTr,
                    ChatTemplateType.MatchEnd => !string.IsNullOrWhiteSpace(_data.MatchEndTr) ? _data.MatchEndTr : DefaultMatchEndTr,
                    ChatTemplateType.Leaderboard => !string.IsNullOrWhiteSpace(_data.LeaderboardTr) ? _data.LeaderboardTr : DefaultLeaderboardTr,
                    _ => ""
                };
            }
        }
    }

    public static void SaveTemplates(string matchStart, string matchEnd, string leaderboard, bool isEnglish)
    {
        EnsureLoaded();
        lock (Lock)
        {
            if (isEnglish)
            {
                _data.MatchStartEn = string.IsNullOrWhiteSpace(matchStart) || matchStart == DefaultMatchStartEn ? null : matchStart.Trim();
                _data.MatchEndEn = string.IsNullOrWhiteSpace(matchEnd) || matchEnd == DefaultMatchEndEn ? null : matchEnd.Trim();
                _data.LeaderboardEn = string.IsNullOrWhiteSpace(leaderboard) || leaderboard == DefaultLeaderboardEn ? null : leaderboard.Trim();
            }
            else
            {
                _data.MatchStartTr = string.IsNullOrWhiteSpace(matchStart) || matchStart == DefaultMatchStartTr ? null : matchStart.Trim();
                _data.MatchEndTr = string.IsNullOrWhiteSpace(matchEnd) || matchEnd == DefaultMatchEndTr ? null : matchEnd.Trim();
                _data.LeaderboardTr = string.IsNullOrWhiteSpace(leaderboard) || leaderboard == DefaultLeaderboardTr ? null : leaderboard.Trim();
            }

            try
            {
                var filePath = GetConfigFilePath();
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json, new System.Text.UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Constants.Log?.Error(ex, "Failed to save chat_templates.json");
            }
        }
    }

    public static void ResetToDefaults(bool isEnglish)
    {
        EnsureLoaded();
        lock (Lock)
        {
            if (isEnglish)
            {
                _data.MatchStartEn = null;
                _data.MatchEndEn = null;
                _data.LeaderboardEn = null;
            }
            else
            {
                _data.MatchStartTr = null;
                _data.MatchEndTr = null;
                _data.LeaderboardTr = null;
            }

            try
            {
                var filePath = GetConfigFilePath();
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json, new System.Text.UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Constants.Log?.Error(ex, "Failed to reset chat_templates.json");
            }
        }
    }

    public static string FormatMatchStart(MatchViewModel vm)
    {
        if (vm?.Match == null) return "";
        var isEn = L10n.IsEnglish;
        var template = GetTemplate(ChatTemplateType.MatchStart, isEn);

        var context = ExtractMatchContext(vm, isEn, isMatchEnd: false);
        return ApplyContextToTemplate(template, context, sanitizeSingleLine: true);
    }

    public static string FormatMatchEnd(MatchViewModel vm)
    {
        if (vm?.Match == null) return "";
        var isEn = L10n.IsEnglish;
        var template = GetTemplate(ChatTemplateType.MatchEnd, isEn);

        var context = ExtractMatchContext(vm, isEn, isMatchEnd: true);
        return ApplyContextToTemplate(template, context, sanitizeSingleLine: true);
    }

    public static string FormatLeaderboard(List<string> formattedEntries, bool multiLine = false)
    {
        if (formattedEntries == null || formattedEntries.Count == 0) return "";
        var isEn = L10n.IsEnglish;
        var template = GetTemplate(ChatTemplateType.Leaderboard, isEn);

        var singleLineText = string.Join(", ", formattedEntries);
        var multiLineText = string.Join(Environment.NewLine, formattedEntries.Select((e, idx) => $"{idx + 1}. {e}"));

        var result = ReplacePlaceholder(template, "leaderboard", singleLineText);
        result = ReplacePlaceholder(result, "leaderboard_multiline", multiLineText);

        if (multiLine)
        {
            return result.Trim();
        }

        return Controls.InventoryControl.SanitizeChatMessage(result);
    }

    public static string GetSamplePreview(string template, ChatTemplateType type, bool isEnglish)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            template = GetTemplate(type, isEnglish);
        }

        var context = new MatchChatContext
        {
            TotalVp = 48500,
            TotalTl = isEnglish ? "~$170" : "~5.820 TL",
            Richest1 = isEnglish ? "Our Jett (12500 VP)" : "Bizim Jett (12500 VP)",
            Richest2 = isEnglish ? "Enemy Reyna (8700 VP)" : "Sizin Reyna (8700 VP)",
            Richest = isEnglish
                ? (type == ChatTemplateType.MatchEnd
                    ? ", Richest players were 1. Our Jett (12500 VP), 2. Enemy Reyna (8700 VP)"
                    : ", Richest players: 1. Our Jett (12500 VP), 2. Enemy Reyna (8700 VP)")
                : (type == ChatTemplateType.MatchEnd
                    ? ", En zenginler ise 1. Bizim Jett (12500 VP), 2. Sizin Reyna (8700 VP) idi"
                    : ", En zenginler ise 1. Bizim Jett (12500 VP), 2. Sizin Reyna (8700 VP)"),
            BlacklistCallout = isEnglish
                ? ", Today's motherfuckers: Ahmet, Mehmet"
                : ", Bu günün orospu çocukları da Ahmet, Mehmet",
            LeaderboardSingleLine = isEnglish
                ? "Reyna (12500 VP), Jett (8700 VP), Fade (4350 VP)"
                : "Reyna (12500 VP), Jett (8700 VP), Fade (4350 VP)",
            LeaderboardMultiLine = isEnglish
                ? "1. Reyna (12500 VP)\n2. Jett (8700 VP)\n3. Fade (4350 VP)"
                : "1. Reyna (12500 VP)\n2. Jett (8700 VP)\n3. Fade (4350 VP)"
        };

        return ApplyContextToTemplate(template, context, sanitizeSingleLine: (type != ChatTemplateType.Leaderboard));
    }

    private class MatchChatContext
    {
        public int TotalVp { get; set; }
        public string TotalTl { get; set; } = "";
        public string Richest { get; set; } = "";
        public string Richest1 { get; set; } = "";
        public string Richest2 { get; set; } = "";
        public string BlacklistCallout { get; set; } = "";
        public string LeaderboardSingleLine { get; set; } = "";
        public string LeaderboardMultiLine { get; set; } = "";
    }

    private static MatchChatContext ExtractMatchContext(MatchViewModel vm, bool isEnglish, bool isMatchEnd)
    {
        var context = new MatchChatContext();
        if (vm == null || vm.Match == null) return context;

        var allPlayers = new List<Player>();
        if (vm.LeftPlayerList != null) allPlayers.AddRange(vm.LeftPlayerList);
        if (vm.RightPlayerList != null) allPlayers.AddRange(vm.RightPlayerList);

        var myPlayer = allPlayers.FirstOrDefault(p => p.PlayerUiData != null && p.PlayerUiData.Puuid == Constants.Ppuuid);
        var myTeamId = myPlayer?.TeamId;

        context.TotalVp = vm.Match.LobbyTotalValueVp > 0
            ? vm.Match.LobbyTotalValueVp
            : allPlayers.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
        context.TotalTl = CurrencyHelper.FormatTlCompact(context.TotalVp);

        var rankedPlayers = allPlayers
            .Where(p => (p.SkinData?.TotalInventoryValueVp ?? 0) > 0)
            .OrderByDescending(p => p.SkinData?.TotalInventoryValueVp ?? 0)
            .ToList();

        if (rankedPlayers.Count >= 2)
        {
            var p1 = rankedPlayers[0];
            var p2 = rankedPlayers[1];

            var who1 = FormatPlayerIdentity(p1, myTeamId, vm, isEnglish);
            var vp1Text = CurrencyHelper.FormatVp(p1.SkinData.TotalInventoryValueVp);

            var who2 = FormatPlayerIdentity(p2, myTeamId, vm, isEnglish);
            var vp2Text = CurrencyHelper.FormatVp(p2.SkinData.TotalInventoryValueVp);

            context.Richest1 = $"{who1} ({vp1Text})";
            context.Richest2 = $"{who2} ({vp2Text})";

            if (isEnglish)
            {
                context.Richest = isMatchEnd
                    ? $", Richest players were 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text})"
                    : $", Richest players: 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text})";
            }
            else
            {
                context.Richest = isMatchEnd
                    ? $", En zenginler ise 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text}) idi"
                    : $", En zenginler ise 1. {who1} ({vp1Text}), 2. {who2} ({vp2Text})";
            }
        }
        else if (rankedPlayers.Count == 1)
        {
            var p1 = rankedPlayers[0];
            var who1 = FormatPlayerIdentity(p1, myTeamId, vm, isEnglish);
            var vp1Text = CurrencyHelper.FormatVp(p1.SkinData.TotalInventoryValueVp);

            context.Richest1 = $"{who1} ({vp1Text})";

            if (isEnglish)
            {
                context.Richest = isMatchEnd
                    ? $", Richest player was 1. {who1} ({vp1Text})"
                    : $", Richest player: 1. {who1} ({vp1Text})";
            }
            else
            {
                context.Richest = isMatchEnd
                    ? $", En zenginse 1. {who1} ({vp1Text}) idi"
                    : $", En zenginse 1. {who1} ({vp1Text})";
            }
        }

        // Blacklist callout
        var blacklisted = allPlayers
            .Where(p => p.IsBlacklisted)
            .OrderBy(p => p.BlacklistOrder)
            .ToList();

        if (blacklisted.Count > 0)
        {
            var names = blacklisted.Select(p => FormatCalloutName(p, myTeamId, vm, isEnglish)).ToList();
            var prefix = isEnglish ? ",Today's motherfuckers: " : ",Bu günün orospu çocukları da ";
            context.BlacklistCallout = $"{prefix}{string.Join(", ", names)}";
        }

        // Leaderboard
        var entries = rankedPlayers.Take(7).Select(p =>
        {
            var name = FormatPlayerIdentity(p, myTeamId, vm, isEnglish);
            var vpText = CurrencyHelper.FormatVp(p.SkinData.TotalInventoryValueVp);
            return $"{name} ({vpText})";
        }).ToList();

        context.LeaderboardSingleLine = string.Join(", ", entries);
        context.LeaderboardMultiLine = string.Join(Environment.NewLine, entries.Select((e, idx) => $"{idx + 1}. {e}"));

        return context;
    }

    private static string ApplyContextToTemplate(string template, MatchChatContext ctx, bool sanitizeSingleLine)
    {
        if (string.IsNullOrWhiteSpace(template)) return "";

        var text = template;
        text = ReplacePlaceholder(text, "totalVp", ctx.TotalVp.ToString());
        text = ReplacePlaceholder(text, "totalTl", ctx.TotalTl);
        text = ReplacePlaceholder(text, "richest", ctx.Richest);
        text = ReplacePlaceholder(text, "richest1", ctx.Richest1);
        text = ReplacePlaceholder(text, "richest2", ctx.Richest2);
        text = ReplacePlaceholder(text, "blacklist", ctx.BlacklistCallout);
        text = ReplacePlaceholder(text, "leaderboard", ctx.LeaderboardSingleLine);
        text = ReplacePlaceholder(text, "leaderboard_multiline", ctx.LeaderboardMultiLine);

        if (sanitizeSingleLine)
        {
            return Controls.InventoryControl.SanitizeChatMessage(text);
        }

        return text.Trim();
    }

    private static string ReplacePlaceholder(string template, string placeholder, string value)
    {
        if (string.IsNullOrEmpty(template)) return "";
        return Regex.Replace(template, "\\{" + Regex.Escape(placeholder) + "\\}", value ?? "", RegexOptions.IgnoreCase);
    }

    private static string FormatPlayerIdentity(Player player, string? myTeamId, MatchViewModel? vm, bool isEnglish)
    {
        if (player == null) return isEnglish ? "player" : "oyuncu";

        var username = player.IgnData?.Username;
        if (!string.IsNullOrWhiteSpace(username) && username != "----")
        {
            var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                return clean;
        }

        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name
            : (isEnglish ? "player" : "oyuncu");

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

        if (isEnglish)
        {
            var teamPrefix = isOurTeam ? "Our" : "Enemy";
            return $"{teamPrefix} {agentName}";
        }
        else
        {
            var teamPrefix = isOurTeam ? "bizim" : "sizin";
            return $"{teamPrefix} {agentName.ToLowerInvariant()}";
        }
    }

    private static string FormatCalloutName(Player player, string? myTeamId, MatchViewModel? vm, bool isEnglish)
    {
        if (player == null) return isEnglish ? "player" : "oyuncu";

        var username = player.IgnData?.Username;
        if (!string.IsNullOrWhiteSpace(username) && username != "----")
        {
            var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
            if (!string.IsNullOrWhiteSpace(clean))
                return clean;
        }

        var agentName = !string.IsNullOrWhiteSpace(player.IdentityData?.Name)
            ? player.IdentityData.Name
            : (isEnglish ? "player" : "oyuncu");

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

        if (isEnglish)
        {
            var teamPrefix = isOurTeam ? "Our" : "Enemy";
            return $"{teamPrefix} {agentName}";
        }
        else
        {
            var teamPrefix = isOurTeam ? "bizim" : "sizin";
            return $"{teamPrefix} {agentName}";
        }
    }
}
