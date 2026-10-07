using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public static class MatchHistoryManager
{
    public static event Action MatchHistoryUpdated;

    private static readonly object FileLock = new();
    private static List<MatchHistoryItem> _cachedMatches;

    public static string FilePath =>
        Path.Combine(
            string.IsNullOrEmpty(Constants.LocalAppDataPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                : Constants.LocalAppDataPath,
            "match_history.json"
        );

    public static List<MatchHistoryItem> GetMatches()
    {
        lock (FileLock)
        {
            if (_cachedMatches != null)
                return _cachedMatches;

            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    if (json.Contains("AppData/Local/NOWT") || json.Contains("AppData/Local/ValPulse") ||
                        json.Contains("AppData\\Local\\NOWT") || json.Contains("AppData\\Local\\ValPulse"))
                    {
                        json = json.Replace("AppData/Local/NOWT", "AppData/Local/ValGrid")
                                   .Replace("AppData/Local/ValPulse", "AppData/Local/ValGrid")
                                   .Replace("AppData\\Local\\NOWT", "AppData\\Local\\ValGrid")
                                   .Replace("AppData\\Local\\ValPulse", "AppData\\Local\\ValGrid");
                        try { File.WriteAllText(FilePath, json); } catch { }
                    }
                    var list = JsonSerializer.Deserialize<List<MatchHistoryItem>>(json);
                    if (list != null)
                    {
                        _cachedMatches = list;
                        if (CleanupBogusMatchesInternal(_cachedMatches))
                        {
                            Save();
                        }
                        return _cachedMatches;
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.GetMatches load failed: {e}", ex);
            }

            _cachedMatches = new List<MatchHistoryItem>();
            return _cachedMatches;
        }
    }

    public static bool CleanupBogusMatches()
    {
        lock (FileLock)
        {
            var matches = GetMatches();
            var changed = CleanupBogusMatchesInternal(matches);
            if (changed)
            {
                Save();
                MatchHistoryUpdated?.Invoke();
            }
            return changed;
        }
    }

    private static bool CleanupBogusMatchesInternal(List<MatchHistoryItem> list)
    {
        if (list == null || list.Count == 0) return false;

        var activeMatchId = LiveMatch.Matchid != Guid.Empty ? LiveMatch.Matchid.ToString() : null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var initialCount = list.Count;

        list.RemoveAll(m =>
        {
            if (m == null) return true;

            // 1. Poligon is not a real match
            if (string.Equals(m.MapName, "Poligon", StringComparison.OrdinalIgnoreCase))
                return true;

            var pCount = m.Players?.Count ?? 0;
            if (pCount == 0)
                return true;

            return false;
        });

        // 2. Ajan seçiminde kalıp dodgelanan veya eski tamamlanmamış maçları güncelle (asla silme)
        foreach (var m in list)
        {
            var isCurrentlyActive = !string.IsNullOrEmpty(activeMatchId) && string.Equals(m.MatchId, activeMatchId, StringComparison.OrdinalIgnoreCase);
            if (!isCurrentlyActive)
            {
                // Ajan seçiminde kalmış ve maç bozulmuşsa (dodge)
                if (m.Result == "AJAN SEÇİMİ" || m.Score == "Ajan Seçimi")
                {
                    m.Result = "BOZULDU";
                    m.Score = "Maç Başlamadı";
                    m.ResultColor = "#f59e0b"; // Kehribar (Amber)
                    m.ResultBadgeBg = "#362715";
                }
                // Canlı maç olarak kalmış ancak üzerinden 2 saat geçmiş ve tamamlanamamışsa
                else if ((m.Result == "CANLI MAÇ" || m.Score == "Oynanıyor") && m.Timestamp > 0 && (now - m.Timestamp > 2 * 3600 * 1000))
                {
                    m.Result = "YARIDA KALDI";
                    m.Score = "İptal";
                    m.ResultColor = "#8e9bb5";
                    m.ResultBadgeBg = "#1f2430";
                }
            }
        }

        // 3. Deduplicate matches with identical MatchId
        var distinct = list
            .GroupBy(m => m.MatchId)
            .Select(g => g.OrderByDescending(m => m.Players?.Count ?? 0).ThenByDescending(m => m.Timestamp).First())
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        list.Clear();
        list.AddRange(distinct);

        var changed = list.Count != initialCount;
        if (changed)
        {
            Constants.Log?.Information("MatchHistoryManager: Cleaned up {count} bogus/duplicate matches. Remaining: {rem}", initialCount - list.Count, list.Count);
        }
        return changed;
    }

    public static void DeleteMatch(string matchId)
    {
        if (string.IsNullOrEmpty(matchId)) return;
        lock (FileLock)
        {
            var matches = GetMatches();
            var count = matches.RemoveAll(m => m.MatchId == matchId);
            if (count > 0)
            {
                Save();
                Constants.Log?.Information("MatchHistoryManager: Deleted match {id}", matchId);
            }
        }
        MatchHistoryUpdated?.Invoke();
    }

    public static void Save()
    {
        string json = null;
        lock (FileLock)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                json = JsonSerializer.Serialize(_cachedMatches ?? new List<MatchHistoryItem>(), options);
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.Save serialize failed: {e}", ex);
            }
        }

        if (json != null)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.Save write failed: {e}", ex);
            }
        }
    }

    public static void RecordLiveMatch(LiveMatch liveMatch, List<Player> players, Guid ownPuuid)
    {
        if (liveMatch == null || players == null || players.Count == 0 || LiveMatch.Matchid == Guid.Empty)
            return;

        try
        {
            lock (FileLock)
            {
                var matches = GetMatches();
                var matchIdStr = LiveMatch.Matchid.ToString();
                var isPregame = LiveMatch.Stage == "pre";

                var ownPlayer = players.FirstOrDefault(p => p.PlayerUiData != null && p.PlayerUiData.Puuid == ownPuuid);
                var ownTeam = ownPlayer?.TeamId ?? "Blue";

                var item = matches.FirstOrDefault(m => m.MatchId == matchIdStr);
                var isNew = item == null;
                item ??= new MatchHistoryItem();

                item.MatchId = matchIdStr;
                item.Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                item.RelativeTime = "Az önce";
                item.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var resolvedMap = !string.IsNullOrEmpty(liveMatch.MatchInfo?.Map) ? MapHelper.ResolveMapName(liveMatch.MatchInfo.Map) : null;
                if (!string.IsNullOrEmpty(resolvedMap) && resolvedMap != "Bilinmeyen Harita")
                {
                    item.MapName = resolvedMap;
                }
                else if (string.IsNullOrEmpty(item.MapName))
                {
                    item.MapName = "Bilinmeyen Harita";
                }

                if (liveMatch.MatchInfo?.MapImage != null)
                {
                    item.MapImage = liveMatch.MatchInfo.MapImage.ToString();
                }
                else if (!string.IsNullOrEmpty(item.MapName) && item.MapName != "Bilinmeyen Harita")
                {
                    var fallbackImg = MapHelper.ResolveMapImage(item.MapName);
                    if (fallbackImg != null)
                        item.MapImage = fallbackImg.ToString();
                }
                item.GameMode = !string.IsNullOrEmpty(liveMatch.MatchInfo?.GameMode) ? liveMatch.MatchInfo.GameMode : "Standart";
                item.GameModeIcon = liveMatch.MatchInfo?.GameModeImage?.ToString() ?? "";

                item.TotalInventoryVp = liveMatch.MatchInfo?.LobbyTotalValueVp > 0
                    ? liveMatch.MatchInfo.LobbyTotalValueVp
                    : players.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
                item.TotalInventoryTl = CurrencyHelper.VpToTl(item.TotalInventoryVp);
                item.TotalInventoryCombinedText = CurrencyHelper.FormatVpAndTl(item.TotalInventoryVp);

                item.MyAgentName = ownPlayer?.IdentityData?.Name ?? (L10n.IsEnglish ? "Agent" : "Ajan");
                item.MyAgentIcon = EncounterTracker.ResolveAgentIcon(ownPlayer?.IdentityData?.Image?.ToString(), ownPlayer?.IdentityData?.Name);
                item.MyKda = "-";

                if (isPregame)
                {
                    if (item.Result != "GALİBİYET" && item.Result != "MAĞLUBİYET" && item.Result != "BERABERLİK" && item.Result != "BOZULDU")
                    {
                        item.Score = L10n.IsEnglish ? "Agent Select" : "Ajan Seçimi";
                        item.Result = L10n.IsEnglish ? "AGENT SELECT" : "AJAN SEÇİMİ";
                        item.ResultColor = "#38bdf8";
                        item.ResultBadgeBg = "#152838";
                    }
                }
                else
                {
                    if (item.Result != "GALİBİYET" && item.Result != "MAĞLUBİYET" && item.Result != "BERABERLİK")
                    {
                        item.Score = L10n.IsEnglish ? "Playing" : "Oynanıyor";
                        item.Result = L10n.IsEnglish ? "LIVE MATCH" : "CANLI MAÇ";
                        item.ResultColor = "#007ef9";
                        item.ResultBadgeBg = "#152538";
                    }
                }

                var playerItemList = new List<MatchHistoryPlayerItem>();
                foreach (var p in players)
                {
                    if (p == null) continue;

                    var rawUser = p.IgnData?.Username;
                    if (string.IsNullOrWhiteSpace(rawUser) || rawUser == "----")
                    {
                        var known = EncounterTracker.GetKnownPlayerName(p.PlayerUiData?.Puuid ?? Guid.Empty);
                        if (!string.IsNullOrEmpty(known))
                            rawUser = known;
                    }

                    var pItem = new MatchHistoryPlayerItem
                    {
                        Puuid = p.PlayerUiData?.Puuid ?? Guid.Empty,
                        Username = CleanUsername(rawUser),
                        AgentName = !string.IsNullOrWhiteSpace(p.IdentityData?.Name) ? p.IdentityData.Name : (L10n.IsEnglish ? "Unknown" : "Bilinmeyen"),
                        AgentIcon = EncounterTracker.ResolveAgentIcon(p.IdentityData?.Image?.ToString(), p.IdentityData?.Name),
                        AccountLevel = !string.IsNullOrEmpty(p.AccountLevel) ? p.AccountLevel : "-",
                        PeakRankName = p.RankData?.PeakRankName ?? "",
                        PeakRankIcon = p.RankData?.PeakRankImage?.ToString() ?? "",
                        PeakRankTooltip = !string.IsNullOrEmpty(p.RankData?.PeakRankTooltip) ? p.RankData.PeakRankTooltip : (!string.IsNullOrEmpty(p.RankData?.PeakRankName) ? $"Peak: {p.RankData.PeakRankName}" : ""),
                        RankProgress = p.MatchHistoryData?.RankProgress ?? 0,
                        RankName = (p.RankData?.RankNames != null && p.RankData.RankNames.Length > 0 && !string.IsNullOrEmpty(p.RankData.RankNames[0]))
                            ? p.RankData.RankNames[0]
                            : (p.RankData?.PeakRankName ?? (L10n.IsEnglish ? "Unrated" : "Derecesiz")),
                        RankIcon = (p.RankData?.RankImages != null && p.RankData.RankImages.Length > 0 && p.RankData.RankImages[0] != null)
                            ? p.RankData.RankImages[0].ToString()
                            : (p.RankData?.PeakRankImage?.ToString() ?? ""),
                        RankTier = 0,
                        TeamId = p.TeamId ?? "",
                        IsAlly = isPregame ? true : (p.TeamId == ownTeam),
                        TotalInventoryValueVp = p.SkinData?.TotalInventoryValueVp ?? 0
                    };

                    pItem.TeamName = pItem.IsAlly ? (L10n.IsEnglish ? "Our Team" : "Bizim Takım") : (L10n.IsEnglish ? "Enemy Team" : "Karşı Takım");
                    pItem.TeamColor = pItem.IsAlly ? "#5b92e5" : "#e55b5b";
                    pItem.TotalValueFormatted = CurrencyHelper.FormatVp(pItem.TotalInventoryValueVp);
                    pItem.TotalValueTlFormatted = CurrencyHelper.FormatTl(pItem.TotalInventoryValueVp);
                    pItem.Note = EncounterTracker.GetNote(pItem.Puuid);
                    pItem.IsBlacklisted = BlacklistManager.IsBlacklisted(pItem.Puuid, pItem.Username);
                    pItem.IsFemale = FemaleTagManager.IsFemale(pItem.Puuid, pItem.Username);
                    pItem.IsMale = MaleTagManager.IsMale(pItem.Puuid, pItem.Username);

                    var stats = EncounterTracker.GetEncounterStats(pItem.Puuid);
                    pItem.EncounterCount = stats.Total;
                    pItem.EncounterBadgeText = stats.Total > 1 ? (L10n.IsEnglish ? $"🔁 {stats.Total}x Met" : $"🔁 {stats.Total}x Denk Geldik") : (stats.Total == 1 ? (L10n.IsEnglish ? "1x Met" : "1x Denk Geldik") : "");
                    pItem.EncounterTooltip = stats.Total > 0
                        ? (L10n.IsEnglish ? $"Encountered this player {stats.Total} times.\n({stats.Ally}x Ally, {stats.Enemy}x Enemy)" : $"Bu oyuncuyla toplam {stats.Total} kez karşılaştınız.\n({stats.Ally}x Dost Takım, {stats.Enemy}x Rakip Takım)")
                        : (L10n.IsEnglish ? "No other encounter records with this player." : "Bu oyuncuyla başka karşılaşma kaydı bulunmuyor.");

                    // Save full player inventory, card, sprays, and weapons
                    if (p.SkinData != null)
                    {
                        var full = new PlayerFullSkinDataDto
                        {
                            CardImage = p.SkinData.CardImage?.ToString(),
                            LargeCardImage = p.SkinData.LargeCardImage?.ToString(),
                            CardName = p.SkinData.CardName,
                            Spray1Image = p.SkinData.Spray1Image?.ToString(),
                            Spray1Name = p.SkinData.Spray1Name,
                            Spray2Image = p.SkinData.Spray2Image?.ToString(),
                            Spray2Name = p.SkinData.Spray2Name,
                            Spray3Image = p.SkinData.Spray3Image?.ToString(),
                            Spray3Name = p.SkinData.Spray3Name,
                            Spray4Image = p.SkinData.Spray4Image?.ToString(),
                            Spray4Name = p.SkinData.Spray4Name,
                            TotalInventoryValueVp = p.SkinData.TotalInventoryValueVp
                        };

                        void AddWeapon(string key, Uri img, string name)
                        {
                            if (img == null && string.IsNullOrEmpty(name)) return;
                            ValSkinMeta meta = null;
                            p.SkinData.WeaponMetas?.TryGetValue(key, out meta);
                            WeaponBuddyInfo buddy = null;
                            p.SkinData.WeaponBuddies?.TryGetValue(key, out buddy);

                            full.WeaponSkins[key] = new PlayerSkinItemDto
                            {
                                WeaponKey = key,
                                SkinName = name ?? "",
                                SkinImage = img?.ToString() ?? "",
                                TierColor = meta?.TierColor ?? "#7f8c8d",
                                TierDevName = meta?.TierDevName ?? "Standard",
                                RarityLabel = meta?.RarityLabel ?? "",
                                VpCost = meta?.VpCost ?? 0,
                                IsRare = meta?.IsRare ?? false,
                                BuddyName = buddy?.Name,
                                BuddyImage = buddy?.Image?.ToString()
                            };
                        }

                        AddWeapon("Classic", p.SkinData.ClassicImage, p.SkinData.ClassicName);
                        AddWeapon("Shorty", p.SkinData.ShortyImage, p.SkinData.ShortyName);
                        AddWeapon("Frenzy", p.SkinData.FrenzyImage, p.SkinData.FrenzyName);
                        AddWeapon("Ghost", p.SkinData.GhostImage, p.SkinData.GhostName);
                        AddWeapon("Bandit", p.SkinData.BanditImage, p.SkinData.BanditName);
                        AddWeapon("Sheriff", p.SkinData.SheriffImage, p.SkinData.SheriffName);
                        AddWeapon("Stinger", p.SkinData.StingerImage, p.SkinData.StingerName);
                        AddWeapon("Spectre", p.SkinData.SpectreImage, p.SkinData.SpectreName);
                        AddWeapon("Bucky", p.SkinData.BuckyImage, p.SkinData.BuckyName);
                        AddWeapon("Judge", p.SkinData.JudgeImage, p.SkinData.JudgeName);
                        AddWeapon("Bulldog", p.SkinData.BulldogImage, p.SkinData.BulldogName);
                        AddWeapon("Guardian", p.SkinData.GuardianImage, p.SkinData.GuardianName);
                        AddWeapon("Warden", p.SkinData.WardenImage, p.SkinData.WardenName);
                        AddWeapon("Phantom", p.SkinData.PhantomImage, p.SkinData.PhantomName);
                        AddWeapon("Vandal", p.SkinData.VandalImage, p.SkinData.VandalName);
                        AddWeapon("Marshal", p.SkinData.MarshalImage, p.SkinData.MarshalName);
                        AddWeapon("Outlaw", p.SkinData.OutlawImage, p.SkinData.OutlawName);
                        AddWeapon("Operator", p.SkinData.OperatorImage, p.SkinData.OperatorName);
                        AddWeapon("Ares", p.SkinData.AresImage, p.SkinData.AresName);
                        AddWeapon("Odin", p.SkinData.OdinImage, p.SkinData.OdinName);
                        AddWeapon("Melee", p.SkinData.MeleeImage, p.SkinData.MeleeName);

                        pItem.FullSkinData = full;
                        pItem.CardImage = full.LargeCardImage ?? full.CardImage ?? "";
                        pItem.CardName = full.CardName ?? "";
                        pItem.Spray1Image = full.Spray1Image ?? "";
                        pItem.Spray1Name = full.Spray1Name ?? "";
                        pItem.Spray2Image = full.Spray2Image ?? "";
                        pItem.Spray2Name = full.Spray2Name ?? "";
                        pItem.Spray3Image = full.Spray3Image ?? "";
                        pItem.Spray3Name = full.Spray3Name ?? "";
                        pItem.Spray4Image = full.Spray4Image ?? "";
                        pItem.Spray4Name = full.Spray4Name ?? "";

                        // Also add 4 primary equipped slots for scoreboard row
                        if (p.SkinData.Slot1Image != null || !string.IsNullOrEmpty(p.SkinData.Slot1Name))
                        {
                            pItem.Weapons.Add(new MatchHistoryWeaponItem
                            {
                                WeaponName = !string.IsNullOrEmpty(p.SkinData.Slot1Tooltip) ? p.SkinData.Slot1Tooltip : p.SkinData.Slot1Name,
                                SkinName = p.SkinData.Slot1Name,
                                SkinImage = p.SkinData.Slot1Image?.ToString() ?? "",
                                TierColor = p.SkinData.Slot1BorderBrush,
                                IsRare = p.SkinData.Slot1IsRare
                            });
                        }
                        if (p.SkinData.Slot2Image != null || !string.IsNullOrEmpty(p.SkinData.Slot2Name))
                        {
                            pItem.Weapons.Add(new MatchHistoryWeaponItem
                            {
                                WeaponName = !string.IsNullOrEmpty(p.SkinData.Slot2Tooltip) ? p.SkinData.Slot2Tooltip : p.SkinData.Slot2Name,
                                SkinName = p.SkinData.Slot2Name,
                                SkinImage = p.SkinData.Slot2Image?.ToString() ?? "",
                                TierColor = p.SkinData.Slot2BorderBrush,
                                IsRare = p.SkinData.Slot2IsRare
                            });
                        }
                        if (p.SkinData.Slot3Image != null || !string.IsNullOrEmpty(p.SkinData.Slot3Name))
                        {
                            pItem.Weapons.Add(new MatchHistoryWeaponItem
                            {
                                WeaponName = !string.IsNullOrEmpty(p.SkinData.Slot3Tooltip) ? p.SkinData.Slot3Tooltip : p.SkinData.Slot3Name,
                                SkinName = p.SkinData.Slot3Name,
                                SkinImage = p.SkinData.Slot3Image?.ToString() ?? "",
                                TierColor = p.SkinData.Slot3BorderBrush,
                                IsRare = p.SkinData.Slot3IsRare
                            });
                        }
                        if (p.SkinData.Slot4Image != null || !string.IsNullOrEmpty(p.SkinData.Slot4Name))
                        {
                            pItem.Weapons.Add(new MatchHistoryWeaponItem
                            {
                                WeaponName = !string.IsNullOrEmpty(p.SkinData.Slot4Tooltip) ? p.SkinData.Slot4Tooltip : p.SkinData.Slot4Name,
                                SkinName = p.SkinData.Slot4Name,
                                SkinImage = p.SkinData.Slot4Image?.ToString() ?? "",
                                TierColor = p.SkinData.Slot4BorderBrush,
                                IsRare = p.SkinData.Slot4IsRare
                            });
                        }
                    }

                    pItem.SkinData = p.SkinData;

                    playerItemList.Add(pItem);
                }

                item.Players = new ObservableCollection<MatchHistoryPlayerItem>(playerItemList);

                if (isNew)
                {
                    matches.Insert(0, item);
                    if (matches.Count > 50)
                        matches.RemoveRange(50, matches.Count - 50);
                }

                Save();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("MatchHistoryManager.RecordLiveMatch failed: {e}", ex);
        }
    }

    public static void UpdatePlayerNote(Guid puuid, string newNote)
    {
        if (puuid == Guid.Empty) return;

        lock (FileLock)
        {
            try
            {
                // Update persistent encounter tracker
                EncounterTracker.SetNote(puuid, newNote);

                // Update note in all recorded matches
                var matches = GetMatches();
                var changed = false;

                foreach (var m in matches)
                {
                    if (m?.Players == null) continue;
                    foreach (var p in m.Players)
                    {
                        if (p.Puuid == puuid)
                        {
                            p.Note = newNote?.Trim() ?? "";
                            changed = true;
                        }
                    }
                }

                if (changed)
                    Save();
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.UpdatePlayerNote failed: {e}", ex);
            }
        }
    }

    public static void ClearAllNotes()
    {
        lock (FileLock)
        {
            try
            {
                var matches = GetMatches();
                var changed = false;
                foreach (var m in matches)
                {
                    if (m?.Players == null) continue;
                    foreach (var p in m.Players)
                    {
                        if (!string.IsNullOrEmpty(p.Note))
                        {
                            p.Note = "";
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    Save();
                    MatchHistoryUpdated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.ClearAllNotes failed: {e}", ex);
            }
        }
    }

    public static void ClearHistory()
    {
        lock (FileLock)
        {
            try
            {
                _cachedMatches = new List<MatchHistoryItem>();
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
                Save();
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("MatchHistoryManager.ClearHistory failed: {e}", ex);
            }
        }
    }

    private static bool _isUpdatingMatches = false;

    public static async Task UpdatePendingMatchesAsync()
    {
        if (_isUpdatingMatches) return;
        _isUpdatingMatches = true;

        try
        {
            if (string.IsNullOrEmpty(Constants.Region) || string.IsNullOrEmpty(Constants.AccessToken) || string.IsNullOrEmpty(Constants.EntitlementToken))
            {
                await Login.LocalLoginAsync().ConfigureAwait(false);
                await Login.LocalRegionAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(Constants.Region))
                    Constants.Region = "eu";
            }

            List<MatchHistoryItem> pending;
            lock (FileLock)
            {
                var matches = GetMatches();
                pending = matches.Where(m =>
                    m != null &&
                    !string.IsNullOrEmpty(m.MatchId) &&
                    (m.Result == "CANLI MAÇ" || m.Result == "AJAN SEÇİMİ" || m.Score == "Oynanıyor" || m.Score == "Ajan Seçimi" || string.IsNullOrEmpty(m.Score) || m.Score == "-" ||
                     (m.Players != null && m.Players.Any(p => p.Username == "Gizli Profil" || p.Username == "----" || string.IsNullOrEmpty(p.CardImage))))
                ).Take(15).ToList();
            }

            if (pending.Count == 0) return;

            var changed = false;
            foreach (var match in pending)
            {
                try
                {
                    var response = await Login.DoCachedRequestAsync(
                        Method.Get,
                        $"https://pd.{Constants.Region}.a.pvp.net/match-details/v1/matches/{match.MatchId}",
                        true,
                        true,
                        false
                    ).ConfigureAwait(false);

                    if (response == null) continue;
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Constants.Log?.Warning("UpdatePendingMatchesAsync: Rate limited (429), pausing checks.");
                        break;
                    }

                    if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            var isCurrentlyInThisMatch = (LiveMatch.Matchid != Guid.Empty && string.Equals(LiveMatch.Matchid.ToString(), match.MatchId, StringComparison.OrdinalIgnoreCase));

                            if (!isCurrentlyInThisMatch)
                            {
                                // Ajan seçiminde kalmış ve maç bozulmuşsa (dodge edilmiş / maç başlamamışsa)
                                if (match.Result == "AJAN SEÇİMİ" || match.Score == "Ajan Seçimi")
                                {
                                    match.Result = "BOZULDU";
                                    match.Score = "Maç Başlamadı";
                                    match.ResultColor = "#f59e0b"; // Kehribar (Amber)
                                    match.ResultBadgeBg = "#362715";
                                    changed = true;
                                }
                                // Canlı maç olarak kalmış ancak üzerinden 1 saat geçmiş ve tamamlanamamışsa
                                else if (match.Timestamp > 0 && (now - match.Timestamp > 60 * 60 * 1000) && match.Result == "CANLI MAÇ")
                                {
                                    match.Result = "YARIDA KALDI";
                                    match.Score = "İptal";
                                    match.ResultColor = "#8e9bb5";
                                    match.ResultBadgeBg = "#1f2430";
                                    changed = true;
                                }
                            }
                        }
                        continue;
                    }

                    await Task.Delay(120).ConfigureAwait(false);

                    using var doc = JsonDocument.Parse(response.Content);
                    var root = doc.RootElement;

                    // Match Info
                    if (root.TryGetProperty("matchInfo", out var matchInfo))
                    {
                        if (matchInfo.TryGetProperty("mapId", out var mapIdProp))
                        {
                            var rawMap = mapIdProp.GetString();
                            var resolvedName = MapHelper.ResolveMapName(rawMap);
                            if (!string.IsNullOrEmpty(resolvedName) && resolvedName != "Bilinmeyen Harita")
                                match.MapName = resolvedName;

                            var resolvedImg = MapHelper.ResolveMapImage(rawMap);
                            if (resolvedImg != null)
                                match.MapImage = resolvedImg.ToString();
                        }

                        if (matchInfo.TryGetProperty("gameStartMillis", out var gsmProp) && gsmProp.GetInt64() > 0)
                        {
                            try
                            {
                                var dt = DateTimeOffset.FromUnixTimeMilliseconds(gsmProp.GetInt64()).ToLocalTime().DateTime;
                                match.Date = dt.ToString("dd.MM.yyyy HH:mm");
                            }
                            catch { }
                        }
                    }

                    // Teams & Score
                    int blueScore = 0, redScore = 0;
                    string winningTeam = null;
                    if (root.TryGetProperty("teams", out var teams) && teams.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in teams.EnumerateArray())
                        {
                            var tid = t.TryGetProperty("teamId", out var tidProp) ? tidProp.GetString() : null;
                            var won = t.TryGetProperty("won", out var wonProp) && wonProp.GetBoolean();
                            var rounds = t.TryGetProperty("roundsWon", out var rwProp) ? rwProp.GetInt32() : 0;
                            if (tid == "Blue") blueScore = rounds;
                            else if (tid == "Red") redScore = rounds;
                            if (won) winningTeam = tid;
                        }
                    }

                    // Players list
                    string myTeamId = null;
                    var myPuuidStr = Constants.Ppuuid.ToString();
                    var riotPlayers = new List<JsonElement>();

                    if (root.TryGetProperty("players", out var playersElem) && playersElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in playersElem.EnumerateArray())
                        {
                            riotPlayers.Add(p);
                            var subject = p.TryGetProperty("subject", out var sProp) ? sProp.GetString() : null;
                            var tid = p.TryGetProperty("teamId", out var tProp) ? tProp.GetString() : null;
                            if (!string.IsNullOrEmpty(subject) && string.Equals(subject, myPuuidStr, StringComparison.OrdinalIgnoreCase))
                            {
                                myTeamId = tid;
                            }
                        }
                    }

                    // Fallback to identify our team if myTeamId is null
                    if (myTeamId == null && match.Players != null)
                    {
                        var allyPuuids = match.Players.Where(p => p.IsAlly).Select(p => p.Puuid.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        foreach (var rp in riotPlayers)
                        {
                            var s = rp.TryGetProperty("subject", out var sp) ? sp.GetString() : null;
                            var tid = rp.TryGetProperty("teamId", out var tp) ? tp.GetString() : null;
                            if (s != null && allyPuuids.Contains(s) && tid != null)
                            {
                                myTeamId = tid;
                                break;
                            }
                        }
                    }
                    myTeamId ??= "Blue";

                    // Calculate Result & Score
                    var isTie = (winningTeam == null) || (blueScore == redScore && blueScore > 0);
                    var didWeWin = !isTie && string.Equals(winningTeam, myTeamId, StringComparison.OrdinalIgnoreCase);

                    if (isTie)
                    {
                        match.Result = "BERABERLİK";
                        match.ResultColor = "#ffd700";
                        match.ResultBadgeBg = "#36321b";
                    }
                    else if (didWeWin)
                    {
                        match.Result = "GALİBİYET";
                        match.ResultColor = "#32e2b2";
                        match.ResultBadgeBg = "#183630";
                    }
                    else
                    {
                        match.Result = "MAĞLUBİYET";
                        match.ResultColor = "#e55b5b";
                        match.ResultBadgeBg = "#361b1b";
                    }

                    var ourScore = (myTeamId == "Blue") ? blueScore : redScore;
                    var enemyScore = (myTeamId == "Blue") ? redScore : blueScore;
                    match.Score = $"{ourScore} - {enemyScore}";

                    // Update player details & my KDA
                    if (riotPlayers.Count > 0)
                    {
                        var myRp = riotPlayers.FirstOrDefault(x =>
                            string.Equals(x.TryGetProperty("subject", out var sp) ? sp.GetString() : "", myPuuidStr, StringComparison.OrdinalIgnoreCase));

                        if (myRp.ValueKind != JsonValueKind.Undefined)
                        {
                            if (myRp.TryGetProperty("characterId", out var charProp) && Guid.TryParse(charProp.GetString(), out var myCharGuid))
                            {
                                var agentInfo = LiveMatch.GetAgentInfo(myCharGuid);
                                if (!string.IsNullOrEmpty(agentInfo.Name)) match.MyAgentName = agentInfo.Name;
                                if (agentInfo.Image != null) match.MyAgentIcon = agentInfo.Image.ToString();
                            }

                            if (myRp.TryGetProperty("stats", out var statsProp) && statsProp.ValueKind == JsonValueKind.Object)
                            {
                                var k = statsProp.TryGetProperty("kills", out var kp) ? kp.GetInt32() : 0;
                                var d = statsProp.TryGetProperty("deaths", out var dp) ? dp.GetInt32() : 0;
                                var a = statsProp.TryGetProperty("assists", out var ap) ? ap.GetInt32() : 0;
                                match.MyKda = $"{k}/{d}/{a}";
                            }
                        }

                        // Record all player names from this match into EncounterTracker
                        foreach (var rp in riotPlayers)
                        {
                            var sp = rp.TryGetProperty("subject", out var sProp) ? sProp.GetString() : (rp.TryGetProperty("puuid", out var pProp) ? pProp.GetString() : null);
                            var gn = rp.TryGetProperty("gameName", out var gnProp) ? gnProp.GetString() : null;
                            var tl = rp.TryGetProperty("tagLine", out var tlProp) ? tlProp.GetString() : null;
                            if (!string.IsNullOrEmpty(sp) && !string.IsNullOrEmpty(gn) && !string.IsNullOrEmpty(tl))
                            {
                                EncounterTracker.RecordKnownPlayerName(sp, $"{gn}#{tl}");
                            }
                        }

                        if (match.Players != null)
                        {
                            foreach (var pItem in match.Players)
                            {
                                var rp = riotPlayers.FirstOrDefault(x =>
                                    string.Equals(x.TryGetProperty("subject", out var sp) ? sp.GetString() : "", pItem.Puuid.ToString(), StringComparison.OrdinalIgnoreCase));

                                if (rp.ValueKind != JsonValueKind.Undefined)
                                {
                                    if (rp.TryGetProperty("gameName", out var gnProp) && rp.TryGetProperty("tagLine", out var tlProp))
                                    {
                                        var gn = gnProp.GetString();
                                        var tl = tlProp.GetString();
                                        if (!string.IsNullOrEmpty(gn) && !string.IsNullOrEmpty(tl))
                                        {
                                            pItem.Username = $"{gn}#{tl}";
                                        }
                                    }

                                    if (rp.TryGetProperty("characterId", out var charProp))
                                    {
                                        var charGuidStr = charProp.GetString();
                                        if (Guid.TryParse(charGuidStr, out var charGuid) && (pItem.AgentName == "Bilinmeyen" || string.IsNullOrEmpty(pItem.AgentName)))
                                        {
                                            var agentInfo = LiveMatch.GetAgentInfo(charGuid);
                                            if (!string.IsNullOrEmpty(agentInfo.Name))
                                                pItem.AgentName = agentInfo.Name;
                                            if (agentInfo.Image != null)
                                                pItem.AgentIcon = agentInfo.Image.ToString();
                                        }
                                    }

                                    if (rp.TryGetProperty("playerCard", out var cardProp))
                                    {
                                        var cardId = cardProp.GetString();
                                        if (!string.IsNullOrEmpty(cardId) && Guid.TryParse(cardId, out var parsedCardGuid))
                                        {
                                            pItem.CardImage = $"https://media.valorant-api.com/playercards/{cardId}/largeart.png";
                                            pItem.CardName = ResolveCardName(parsedCardGuid);
                                            if (pItem.FullSkinData != null)
                                            {
                                                pItem.FullSkinData.LargeCardImage = pItem.CardImage;
                                                pItem.FullSkinData.CardName = pItem.CardName;
                                            }
                                        }
                                    }
                                }
                            }

                            // Resolve any remaining missing or Gizli Profil names using NameService
                            var missingPlayers = match.Players.Where(p =>
                                p.Puuid != Guid.Empty &&
                                (p.Username == "Gizli Profil" || p.Username == "----" || string.IsNullOrWhiteSpace(p.Username) || !p.Username.Contains("#"))
                            ).ToList();

                            if (missingPlayers.Count > 0)
                            {
                                foreach (var mp in missingPlayers.ToList())
                                {
                                    var known = EncounterTracker.GetKnownPlayerName(mp.Puuid);
                                    if (!string.IsNullOrEmpty(known) && known.Contains("#"))
                                    {
                                        mp.Username = known;
                                        missingPlayers.Remove(mp);
                                        changed = true;
                                    }
                                }

                                if (missingPlayers.Count > 0)
                                {
                                    try
                                    {
                                        var puuidsToFetch = missingPlayers.Select(p => p.Puuid).ToArray();
                                        var resolved = await Login.GetNameServiceGetUsernamesAsync(puuidsToFetch).ConfigureAwait(false);
                                        if (resolved != null && resolved.Length == puuidsToFetch.Length)
                                        {
                                            for (int i = 0; i < missingPlayers.Count; i++)
                                            {
                                                var rName = resolved[i];
                                                if (!string.IsNullOrEmpty(rName) && rName.Contains("#") && rName != "----" && rName != "Gizli Profil")
                                                {
                                                    missingPlayers[i].Username = rName;
                                                    EncounterTracker.RecordKnownPlayerName(missingPlayers[i].Puuid, rName);
                                                    changed = true;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Constants.Log?.Warning("Error resolving missing names via NameService: {e}", ex.Message);
                                    }
                                }
                            }
                        }
                    }

                    changed = true;
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("Error updating match {id}: {e}", match.MatchId, ex.Message);
                }
            }

            if (changed)
            {
                Save();
                MatchHistoryUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("MatchHistoryManager.UpdatePendingMatchesAsync failed: {e}", ex);
        }
        finally
        {
            _isUpdatingMatches = false;
        }
    }

    private static string CleanUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return "Gizli Profil";
        var clean = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
        return clean == "----" ? "Gizli Profil" : clean;
    }

    private static Dictionary<Guid, string> _cardNameCache;
    public static string ResolveCardName(Guid cardId)
    {
        if (cardId == Guid.Empty) return "Oyuncu Kartı";
        try
        {
            if (_cardNameCache == null)
            {
                var path = Path.Combine(
                    string.IsNullOrEmpty(Constants.LocalAppDataPath)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                        : Constants.LocalAppDataPath,
                    "ValAPI", "cards.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    _cardNameCache = new Dictionary<Guid, string>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (Guid.TryParse(prop.Name, out var g))
                        {
                            var name = prop.Value.TryGetProperty("Name", out var np) ? np.GetString() : null;
                            if (!string.IsNullOrEmpty(name))
                                _cardNameCache[g] = name;
                        }
                    }
                }
            }
            if (_cardNameCache != null && _cardNameCache.TryGetValue(cardId, out var cardName))
                return cardName;
        }
        catch { }
        return "Oyuncu Kartı";
    }
}

