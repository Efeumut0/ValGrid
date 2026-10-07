using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ValGrid.Objects;
using RestSharp;

namespace ValGrid.Helpers;

public class EncounterMatchRecord
{
    public string MatchId { get; set; } = "";
    public string Date { get; set; } = "";
    public string Map { get; set; } = "";
    public string AgentName { get; set; } = "";
    public string AgentIcon { get; set; } = "";
    public bool IsAlly { get; set; }
    public bool? Won { get; set; }
    public string Score { get; set; } = "";
}

// Persistent record for a single player, keyed by puuid.
public class EncounterRecord
{
    public string Name { get; set; } = "";
    public int Ally { get; set; }
    public int Enemy { get; set; }
    public string LastSeen { get; set; } = "";

    // Id of the last match this player was counted in. Persisted so reopening the
    // app during the same match does not count them again.
    public string LastMatch { get; set; } = "";

    // Custom user note for this player
    public string Note { get; set; } = "";

    // Up to 10 most recent matches with this player (last 5 shown in UI)
    public List<EncounterMatchRecord> Matches { get; set; } = new();
}

// Tracks how many times each player has appeared in your matches (ally vs enemy),
// persists it to disk, attaches small badge to repeat encounters, supports custom notes,
// and shows recent match history with agent icons (left for ally, right for enemy).
public static class EncounterTracker
{
    private static readonly object Lock = new();
    private static Dictionary<string, EncounterRecord> _data;
    private static readonly Dictionary<Guid, WeakReference<EncounterData>> _activeDisplays = new();

    public static event Action EncountersChanged;

    private static string FilePath =>
        (string.IsNullOrEmpty(Constants.LocalAppDataPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
            : Constants.LocalAppDataPath) + "\\encounters.json";

    private static Dictionary<string, EncounterRecord> Data
    {
        get
        {
            if (_data != null)
                return _data;
            try
            {
                if (File.Exists(FilePath))
                {
                    var rawText = File.ReadAllText(FilePath);
                    if (rawText.Contains("AppData/Local/NOWT") || rawText.Contains("AppData/Local/ValPulse") ||
                        rawText.Contains("AppData\\Local\\NOWT") || rawText.Contains("AppData\\Local\\ValPulse"))
                    {
                        rawText = rawText.Replace("AppData/Local/NOWT", "AppData/Local/ValGrid")
                                         .Replace("AppData/Local/ValPulse", "AppData/Local/ValGrid")
                                         .Replace("AppData\\Local\\NOWT", "AppData\\Local\\ValGrid")
                                         .Replace("AppData\\Local\\ValPulse", "AppData\\Local\\ValGrid");
                        try { File.WriteAllText(FilePath, rawText); } catch { }
                    }
                    _data = JsonSerializer.Deserialize<Dictionary<string, EncounterRecord>>(rawText) 
                            ?? new Dictionary<string, EncounterRecord>();
                }
                else
                    _data = new Dictionary<string, EncounterRecord>();
            }
            catch (Exception)
            {
                _data = new Dictionary<string, EncounterRecord>();
            }

            try
            {
                BackfillKnownNamesFromMatchHistory();
            }
            catch { }

            return _data;
        }
    }

    private static void Save(bool notify = true)
    {
        string json = null;
        lock (Lock)
        {
            try
            {
                json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception e)
            {
                Constants.Log?.Error("EncounterTracker serialize failed: {e}", e);
            }
        }

        if (json != null)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Constants.Log?.Error("EncounterTracker save failed: {e}", e);
            }
        }

        if (notify)
        {
            try
            {
                EncountersChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("EncountersChanged event error: {e}", ex);
            }
        }
    }

    public static Dictionary<string, EncounterRecord> GetAllRecords()
    {
        lock (Lock)
        {
            return new Dictionary<string, EncounterRecord>(Data);
        }
    }

    public static void RecordKnownPlayerName(Guid puuid, string realName, bool notify = true)
    {
        if (puuid == Guid.Empty)
            return;
        RecordKnownPlayerName(puuid.ToString(), realName, notify);
    }

    public static void RecordKnownPlayerName(string puuidStr, string realName, bool notify = true)
    {
        if (string.IsNullOrWhiteSpace(puuidStr) || string.IsNullOrWhiteSpace(realName))
            return;

        realName = CleanName(realName);
        if (string.IsNullOrWhiteSpace(realName) || !realName.Contains("#"))
            return;

        try
        {
            var changed = false;
            lock (Lock)
            {
                var key = puuidStr.Trim();
                if (Data.TryGetValue(key, out var rec))
                {
                    if (string.IsNullOrWhiteSpace(rec.Name) || rec.Name == "----" || rec.Name == "Gizli Profil" || rec.Name != realName)
                    {
                        rec.Name = realName;
                        changed = true;
                    }
                }
                else
                {
                    rec = new EncounterRecord
                    {
                        Name = realName,
                        LastSeen = DateTime.UtcNow.ToString("o")
                    };
                    Data[key] = rec;
                    changed = true;
                }
            }

            if (changed)
            {
                Save(notify);
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("RecordKnownPlayerName error: {e}", ex);
        }
    }

    public static void RecordKnownPlayerNames(Dictionary<string, string> pairs, bool notify = false)
    {
        if (pairs == null || pairs.Count == 0)
            return;

        try
        {
            var changed = false;
            lock (Lock)
            {
                foreach (var kvp in pairs)
                {
                    var puuidStr = kvp.Key;
                    var realName = CleanName(kvp.Value);
                    if (string.IsNullOrWhiteSpace(puuidStr) || string.IsNullOrWhiteSpace(realName) || !realName.Contains("#"))
                        continue;

                    var key = puuidStr.Trim();
                    if (Data.TryGetValue(key, out var rec))
                    {
                        if (string.IsNullOrWhiteSpace(rec.Name) || rec.Name == "----" || rec.Name == "Gizli Profil" || rec.Name != realName)
                        {
                            rec.Name = realName;
                            changed = true;
                        }
                    }
                    else
                    {
                        rec = new EncounterRecord
                        {
                            Name = realName,
                            LastSeen = DateTime.UtcNow.ToString("o")
                        };
                        Data[key] = rec;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Save(notify);
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("RecordKnownPlayerNames batch error: {e}", ex);
        }
    }

    public static string GetKnownPlayerName(Guid puuid)
    {
        if (puuid == Guid.Empty)
            return null;
        return GetKnownPlayerName(puuid.ToString());
    }

    public static string GetKnownPlayerName(string puuidStr)
    {
        if (string.IsNullOrWhiteSpace(puuidStr))
            return null;

        try
        {
            lock (Lock)
            {
                var key = puuidStr.Trim();
                if (Data.TryGetValue(key, out var rec) && !string.IsNullOrWhiteSpace(rec.Name))
                {
                    var clean = CleanName(rec.Name);
                    if (!string.IsNullOrEmpty(clean) && clean.Contains("#") && clean != "----" && clean != "Gizli Profil")
                    {
                        return clean;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("GetKnownPlayerName error: {e}", ex);
        }

        return null;
    }

    private static bool _hasBackfilled = false;
    public static void BackfillKnownNamesFromMatchHistory()
    {
        if (_hasBackfilled) return;
        _hasBackfilled = true;

        try
        {
            var matches = MatchHistoryManager.GetMatches();
            if (matches == null || matches.Count == 0) return;

            var changed = false;
            lock (Lock)
            {
                var dict = _data ?? new Dictionary<string, EncounterRecord>();
                foreach (var match in matches)
                {
                    if (match?.Players == null) continue;
                    foreach (var p in match.Players)
                    {
                        if (p.Puuid == Guid.Empty || string.IsNullOrWhiteSpace(p.Username))
                            continue;

                        var clean = CleanName(p.Username);
                        if (string.IsNullOrWhiteSpace(clean) || !clean.Contains("#") || clean == "Gizli Profil" || clean == "----")
                            continue;

                        var key = p.Puuid.ToString();
                        if (dict.TryGetValue(key, out var rec))
                        {
                            if (string.IsNullOrWhiteSpace(rec.Name) || rec.Name == "----" || rec.Name == "Gizli Profil")
                            {
                                rec.Name = clean;
                                changed = true;
                            }
                        }
                        else
                        {
                            dict[key] = new EncounterRecord
                            {
                                Name = clean,
                                LastSeen = DateTime.UtcNow.ToString("o")
                            };
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                Save();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("BackfillKnownNamesFromMatchHistory failed: {e}", ex);
        }
    }

    public static void SetNote(Guid puuid, string note)
    {
        if (puuid == Guid.Empty)
            return;
        SetNote(puuid.ToString(), note);
    }

    public static void SetNote(string puuidStr, string note, string fallbackName = null)
    {
        if (string.IsNullOrWhiteSpace(puuidStr))
            return;

        try
        {
            lock (Lock)
            {
                var key = puuidStr.Trim();
                if (!Data.TryGetValue(key, out var rec))
                {
                    rec = new EncounterRecord();
                    if (!string.IsNullOrWhiteSpace(fallbackName))
                        rec.Name = CleanName(fallbackName);
                    rec.LastSeen = DateTime.UtcNow.ToString("o");
                    Data[key] = rec;
                }
                else if (string.IsNullOrWhiteSpace(rec.Name) && !string.IsNullOrWhiteSpace(fallbackName))
                {
                    rec.Name = CleanName(fallbackName);
                }
                rec.Note = (note ?? "").Trim();
            }
            Save();
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker SetNote failed: {e}", e);
        }
    }

    public static void RemoveRecord(string puuidStr)
    {
        if (string.IsNullOrWhiteSpace(puuidStr))
            return;

        try
        {
            bool removed;
            lock (Lock)
            {
                var key = puuidStr.Trim();
                removed = Data.Remove(key);
            }
            if (removed)
            {
                Save();
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker RemoveRecord failed: {e}", e);
        }
    }

    public static void ClearMatchHistory(string puuidStr)
    {
        if (string.IsNullOrWhiteSpace(puuidStr))
            return;

        try
        {
            lock (Lock)
            {
                var key = puuidStr.Trim();
                if (Data.TryGetValue(key, out var rec))
                {
                    rec.Matches?.Clear();
                    rec.Ally = 0;
                    rec.Enemy = 0;
                    rec.LastMatch = "";
                }
            }
            Save();
            EncountersChanged?.Invoke();
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker ClearMatchHistory failed: {e}", e);
        }
    }

    public static int EncountersCount
    {
        get
        {
            lock (Lock)
            {
                return Data.Values.Count(r => (r.Ally + r.Enemy) > 0 || (r.Matches != null && r.Matches.Count > 0));
            }
        }
    }

    public static int NotesCount
    {
        get
        {
            lock (Lock)
            {
                return Data.Values.Count(r => !string.IsNullOrWhiteSpace(r.Note));
            }
        }
    }

    public static void ClearEncounters()
    {
        try
        {
            lock (Lock)
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in Data)
                {
                    var rec = kvp.Value;
                    rec.Ally = 0;
                    rec.Enemy = 0;
                    if (rec.Matches != null) rec.Matches.Clear();
                    rec.LastMatch = "";
                    if (string.IsNullOrWhiteSpace(rec.Note))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in keysToRemove)
                {
                    Data.Remove(k);
                }
            }
            Save();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("EncounterTracker ClearEncounters failed: {e}", ex);
        }
    }

    public static void ClearNotes()
    {
        try
        {
            lock (Lock)
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in Data)
                {
                    var rec = kvp.Value;
                    rec.Note = "";
                    if (rec.Ally == 0 && rec.Enemy == 0 && (rec.Matches == null || rec.Matches.Count == 0))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in keysToRemove)
                {
                    Data.Remove(k);
                }
            }
            Save();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("EncounterTracker ClearNotes failed: {e}", ex);
        }
    }

    public static string GetNote(Guid puuid)
    {
        if (puuid == Guid.Empty)
            return "";

        try
        {
            lock (Lock)
            {
                var key = puuid.ToString();
                if (Data.TryGetValue(key, out var rec))
                    return rec.Note ?? "";
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker GetNote failed: {e}", e);
        }
        return "";
    }

    public static (int Total, int Ally, int Enemy, string Summary) GetEncounterStats(Guid puuid)
    {
        if (puuid == Guid.Empty)
            return (0, 0, 0, "");

        try
        {
            lock (Lock)
            {
                var key = puuid.ToString();
                if (Data.TryGetValue(key, out var rec))
                {
                    var total = rec.Ally + rec.Enemy;
                    var summary = total > 1 ? $"{total}x Denk Geldik" : (total == 1 ? "1x Denk Geldik" : "");
                    return (total, rec.Ally, rec.Enemy, summary);
                }
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker GetEncounterStats failed: {e}", e);
        }
        return (0, 0, 0, "");
    }

    // Strip the tracker-link suffix and streamer icon, and treat the streamer-mode placeholder as "no name".
    private static string CleanName(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "";
        var name = username.Replace(" 🔗", "").Replace(" 👁", "").Trim();
        return (name == "----" || name == "Gizli Profil") ? "" : name;
    }

    // Call once per produced player list. Attaches prior/updated encounter info to
    // each player and increments counters a single time per unique match id.
    public static void Process(List<Player> players, Guid matchId, Guid ownPuuid, bool countable, string mapName = "")
    {
        if (players == null || players.Count == 0)
            return;

        mapName = MapHelper.ResolveMapName(mapName);

        try
        {
            var changed = false;
            lock (Lock)
            {
                // Figure out which team is "ours" so we can split ally vs enemy.
                string ownTeam = null;
                foreach (var p in players)
                {
                    if (p?.PlayerUiData != null && p.PlayerUiData.Puuid == ownPuuid)
                    {
                        ownTeam = p.TeamId;
                        break;
                    }
                }

                var matchKey = matchId == Guid.Empty ? "" : matchId.ToString();

                foreach (var p in players)
                {
                    if (p?.PlayerUiData == null)
                        continue;

                    var puuid = p.PlayerUiData.Puuid;
                    if (puuid == Guid.Empty || puuid == ownPuuid)
                        continue;

                    var key = puuid.ToString();
                    Data.TryGetValue(key, out var rec);

                    var countThis =
                        countable && matchKey != "" && (rec == null || rec.LastMatch != matchKey);

                    if (countThis)
                    {
                        rec ??= new EncounterRecord();
                        var isAlly = ownTeam == null || p.TeamId == ownTeam;
                        if (isAlly)
                            rec.Ally++;
                        else
                            rec.Enemy++;

                        var clean = CleanName(p.IgnData?.Username);
                        if (!string.IsNullOrEmpty(clean))
                            rec.Name = clean;

                        rec.LastMatch = matchKey;
                        rec.LastSeen = DateTime.UtcNow.ToString("o");

                        // Record or update this match in the player's recent matches list
                        var matchEntry = rec.Matches.Find(m => m.MatchId == matchKey);
                        if (matchEntry == null)
                        {
                            matchEntry = new EncounterMatchRecord
                            {
                                MatchId = matchKey,
                                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                                Map = mapName,
                                AgentName = p.IdentityData?.Name ?? "Bilinmeyen",
                                AgentIcon = p.IdentityData?.Image?.ToString() ?? "",
                                IsAlly = isAlly,
                                Won = null,
                                Score = ""
                            };
                            rec.Matches.Insert(0, matchEntry);
                            if (rec.Matches.Count > 10)
                                rec.Matches.RemoveRange(10, rec.Matches.Count - 10);
                        }
                        else
                        {
                            matchEntry.AgentName = p.IdentityData?.Name ?? matchEntry.AgentName;
                            matchEntry.AgentIcon = p.IdentityData?.Image?.ToString() ?? matchEntry.AgentIcon;
                            matchEntry.IsAlly = isAlly;
                            if (!string.IsNullOrEmpty(mapName))
                                matchEntry.Map = mapName;
                        }

                        Data[key] = rec;
                        changed = true;
                    }

                    p.EncounterData = BuildDisplay(rec, puuid);
                }
            }

            if (changed)
            {
                Save();
                _ = Task.Run(UpdatePendingMatchResultsAsync);
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("EncounterTracker process failed: {e}", e);
        }
    }

    public static EncounterData BuildDisplay(EncounterRecord rec, Guid puuid)
    {
        var ally = rec?.Ally ?? 0;
        var enemy = rec?.Enemy ?? 0;
        var total = ally + enemy;
        var note = rec?.Note ?? "";
        var hasNote = !string.IsNullOrWhiteSpace(note);

        var bg = total >= 2 ? "#f0b232" : (hasNote ? "#32e2b2" : "#4c5b6e");
        var textColor = bg == "#4c5b6e" ? "#FFFFFF" : "#161926";

        var display = new EncounterData
        {
            Puuid = puuid,
            Visible = Visibility.Visible,
            Summary = "↻ " + total + (hasNote ? " 📝" : ""),
            Background = bg,
            TextColor = textColor,
            Note = note,
            HasNote = hasNote
        };

        var tooltip = "";
        if (hasNote)
        {
            tooltip += L10n.IsEnglish
                ? $"📝 Note: {note}\n------------------------------\n"
                : $"📝 Not: {note}\n------------------------------\n";
        }
        tooltip += L10n.IsEnglish
            ? $"Encountered {total} times  (🟢 Ally: {ally}  •  🔴 Enemy: {enemy})"
            : $"{total} kez karşılaştınız  (🟢 Dost: {ally}  •  🔴 Düşman: {enemy})";
        if (!string.IsNullOrEmpty(rec?.Name))
        {
            tooltip += L10n.IsEnglish
                ? $"\nLast known name: {rec.Name}"
                : $"\nSon bilinen isim: {rec.Name}";
        }
        tooltip += L10n.IsEnglish
            ? "\n(Click to add / edit note)"
            : "\n(Not bırakmak / düzenlemek için tıklayın)";
        display.Tooltip = tooltip;

        // Build recent matches for hover popup (up to 5 matches)
        var recentList = new ObservableCollection<EncounterMatch>();
        if (rec?.Matches != null)
        {
            var count = 0;
            foreach (var m in rec.Matches)
            {
                if (count >= 5) break;

                Uri iconUri = null;
                if (!string.IsNullOrEmpty(m.AgentIcon))
                {
                    try
                    {
                        var resolved = ResolveAgentIcon(m.AgentIcon, m.AgentName);
                        if (!string.IsNullOrEmpty(resolved) && Uri.TryCreate(resolved, UriKind.Absolute, out var parsed))
                            iconUri = parsed;
                    }
                    catch { }
                }

                var resolvedMapName = MapHelper.ResolveMapName(m.Map);
                var resolvedMapImg = MapHelper.ResolveMapImage(m.Map);

                recentList.Add(new EncounterMatch
                {
                    MatchId = m.MatchId,
                    Date = m.Date,
                    RelativeTime = CalculateRelativeTime(m.Date),
                    Map = resolvedMapName,
                    MapImage = resolvedMapImg,
                    AgentName = m.AgentName,
                    AgentIcon = iconUri,
                    IsAlly = m.IsAlly,
                    Won = m.Won,
                    Score = m.Score
                });
                count++;
            }
        }
        display.RecentMatches = recentList;
        display.HasMatches = recentList.Count > 0;

        lock (Lock)
        {
            _activeDisplays[puuid] = new WeakReference<EncounterData>(display);
        }

        return display;
    }

    private static bool _isUpdatingPending = false;

    public static async Task UpdatePendingMatchResultsAsync()
    {
        if (_isUpdatingPending) return;
        _isUpdatingPending = true;

        try
        {
            if (string.IsNullOrEmpty(Constants.Region) || string.IsNullOrEmpty(Constants.AccessToken) || string.IsNullOrEmpty(Constants.EntitlementToken))
                return;

            List<(string Puuid, string MatchId, bool IsAlly)> pending = new();
            lock (Lock)
            {
                foreach (var kvp in Data)
                {
                    if (kvp.Value?.Matches == null) continue;
                    foreach (var m in kvp.Value.Matches)
                    {
                        if ((m.Won == null || string.IsNullOrWhiteSpace(kvp.Value.Name) || kvp.Value.Name == "----" || kvp.Value.Name == "Gizli Profil") && !string.IsNullOrEmpty(m.MatchId))
                        {
                            pending.Add((kvp.Key, m.MatchId, m.IsAlly));
                        }
                    }
                }
            }

            if (pending.Count == 0) return;

            var matchGroups = pending.GroupBy(x => x.MatchId).Take(15).ToList();
            var changed = false;

            foreach (var group in matchGroups)
            {
                var matchId = group.Key;
                try
                {
                    // Pass addRiotAuth = true so Riot Auth headers (Entitlements + Bearer token) are included!
                    // Pass bypassCache = true so we always check fresh results for pending matches.
                    var response = await Login.DoCachedRequestAsync(
                        Method.Get,
                        $"https://pd.{Constants.Region}.a.pvp.net/match-details/v1/matches/{matchId}",
                        true,
                        true,
                        false // do not spam error log if match is still in progress (404)
                    ).ConfigureAwait(false);

                    if (response == null) continue;
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Constants.Log?.Warning("UpdatePendingMatchResultsAsync: Rate limited (429), pausing checks.");
                        break;
                    }

                    if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var isCurrentlyInThisMatch = (LiveMatch.Matchid != Guid.Empty && string.Equals(LiveMatch.Matchid.ToString(), matchId, StringComparison.OrdinalIgnoreCase));
                            if (!isCurrentlyInThisMatch)
                            {
                                lock (Lock)
                                {
                                    foreach (var item in group)
                                    {
                                        if (Data.TryGetValue(item.Puuid, out var r) && r?.Matches != null)
                                        {
                                            var mRec = r.Matches.Find(m => m.MatchId == matchId);
                                            if (mRec != null && mRec.Won == null && string.IsNullOrEmpty(mRec.Score))
                                            {
                                                mRec.Score = "Bozuldu";
                                                changed = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        continue;
                    }

                    await Task.Delay(120).ConfigureAwait(false);

                    using var doc = JsonDocument.Parse(response.Content);
                    var root = doc.RootElement;
                    string winningTeam = null;

                    string mapName = null;
                    string matchDate = null;
                    if (root.TryGetProperty("matchInfo", out var matchInfo))
                    {
                        if (matchInfo.TryGetProperty("mapId", out var mapIdProp))
                        {
                            var rawMap = mapIdProp.GetString();
                            mapName = MapHelper.ResolveMapName(rawMap);
                        }
                        if (matchInfo.TryGetProperty("gameStartMillis", out var gsmProp) && gsmProp.GetInt64() > 0)
                        {
                            try
                            {
                                var matchDateTime = DateTimeOffset.FromUnixTimeMilliseconds(gsmProp.GetInt64()).ToLocalTime().DateTime;
                                matchDate = matchDateTime.ToString("dd.MM.yyyy HH:mm");
                            }
                            catch { }
                        }
                    }

                    int blueScore = 0, redScore = 0;
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

                    // Extract our team (Constants.Ppuuid) from match players list
                    string myTeamId = null;
                    var myPuuidStr = Constants.Ppuuid.ToString();
                    Dictionary<string, string> playerTeamMap = new(StringComparer.OrdinalIgnoreCase);

                    if (root.TryGetProperty("players", out var players) && players.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in players.EnumerateArray())
                        {
                            var subject = p.TryGetProperty("subject", out var sProp) ? sProp.GetString() : (p.TryGetProperty("puuid", out var pProp) ? pProp.GetString() : null);
                            var tid = p.TryGetProperty("teamId", out var tProp) ? tProp.GetString() : null;
                            if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(tid))
                            {
                                playerTeamMap[subject] = tid;
                                if (string.Equals(subject, myPuuidStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    myTeamId = tid;
                                }
                            }

                            // Capture and record real Riot ID for all players in the match
                            var gn = p.TryGetProperty("gameName", out var gnProp) ? gnProp.GetString() : null;
                            var tl = p.TryGetProperty("tagLine", out var tlProp) ? tlProp.GetString() : null;
                            if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(gn) && !string.IsNullOrEmpty(tl))
                            {
                                var realName = $"{gn}#{tl}";
                                lock (Lock)
                                {
                                    if (Data.TryGetValue(subject, out var prec))
                                    {
                                        if (string.IsNullOrWhiteSpace(prec.Name) || prec.Name == "----" || prec.Name == "Gizli Profil" || prec.Name != realName)
                                        {
                                            prec.Name = realName;
                                            changed = true;
                                        }
                                    }
                                    else
                                    {
                                        Data[subject] = new EncounterRecord
                                        {
                                            Name = realName,
                                            LastSeen = DateTime.UtcNow.ToString("o")
                                        };
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }

                    if (winningTeam != null)
                    {
                        lock (Lock)
                        {
                            foreach (var item in group)
                            {
                                if (Data.TryGetValue(item.Puuid, out var rec) && rec.Matches != null)
                                {
                                    var m = rec.Matches.Find(x => x.MatchId == matchId);
                                    if (m != null)
                                    {
                                        string ourTeam = myTeamId;
                                        if (ourTeam == null && playerTeamMap.TryGetValue(item.Puuid, out var pTeam))
                                        {
                                            ourTeam = item.IsAlly ? pTeam : (pTeam == "Blue" ? "Red" : "Blue");
                                        }

                                        bool weWon;
                                        int myRounds, enemyRounds;
                                        if (ourTeam != null)
                                        {
                                            weWon = string.Equals(winningTeam, ourTeam, StringComparison.OrdinalIgnoreCase);
                                            myRounds = ourTeam == "Blue" ? blueScore : redScore;
                                            enemyRounds = ourTeam == "Blue" ? redScore : blueScore;
                                        }
                                        else
                                        {
                                            weWon = (item.IsAlly && winningTeam == "Blue") || (!item.IsAlly && winningTeam != "Blue");
                                            myRounds = weWon ? Math.Max(blueScore, redScore) : Math.Min(blueScore, redScore);
                                            enemyRounds = weWon ? Math.Min(blueScore, redScore) : Math.Max(blueScore, redScore);
                                        }

                                        string formattedScore = (blueScore > 0 || redScore > 0) ? $"{myRounds}-{enemyRounds}" : "";

                                        m.Won = weWon;
                                        if (!string.IsNullOrEmpty(formattedScore)) m.Score = formattedScore;
                                        if (!string.IsNullOrEmpty(mapName) && string.IsNullOrEmpty(m.Map)) m.Map = mapName;
                                        if (!string.IsNullOrEmpty(matchDate)) m.Date = matchDate;
                                        changed = true;

                                        // Real-time update to any active UI displays
                                        if (Guid.TryParse(item.Puuid, out var playerGuid) &&
                                            _activeDisplays.TryGetValue(playerGuid, out var weakDisplay) &&
                                            weakDisplay.TryGetTarget(out var liveDisplay))
                                        {
                                            var resolvedMap = mapName;
                                            var resolvedDate = matchDate;
                                            Application.Current?.Dispatcher?.BeginInvoke(() =>
                                            {
                                                var liveMatch = liveDisplay.RecentMatches?.FirstOrDefault(x => x.MatchId == matchId);
                                                if (liveMatch != null)
                                                {
                                                    liveMatch.Won = weWon;
                                                    if (!string.IsNullOrEmpty(formattedScore)) liveMatch.Score = formattedScore;
                                                    if (!string.IsNullOrEmpty(resolvedMap) && string.IsNullOrEmpty(liveMatch.Map)) liveMatch.Map = resolvedMap;
                                                    if (liveMatch.MapImage == null && !string.IsNullOrEmpty(liveMatch.Map)) liveMatch.MapImage = MapHelper.ResolveMapImage(liveMatch.Map);
                                                    if (!string.IsNullOrEmpty(resolvedDate)) liveMatch.Date = resolvedDate;
                                                    liveMatch.RelativeTime = CalculateRelativeTime(liveMatch.Date);
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Match details not ready yet (404) or network error
                }
            }

            if (changed)
            {
                Save();
            }

            // Also update any pending matches in Career match history
            _ = MatchHistoryManager.UpdatePendingMatchesAsync();
        }
        catch (Exception e)
        {
            Constants.Log?.Error("UpdatePendingMatchResultsAsync failed: {e}", e);
        }
        finally
        {
            _isUpdatingPending = false;
        }
    }

    public static string ResolveAgentIcon(string rawIconPath, string agentName = null)
    {
        if (string.IsNullOrWhiteSpace(rawIconPath) && string.IsNullOrWhiteSpace(agentName))
            return "";

        // 1. If it's a web URL (http/https), return it directly
        if (!string.IsNullOrWhiteSpace(rawIconPath) && 
            (rawIconPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
             rawIconPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return rawIconPath;
        }

        // 2. Extract filename from local path or file URI
        string fileName = "";
        if (!string.IsNullOrWhiteSpace(rawIconPath))
        {
            try
            {
                if (Uri.TryCreate(rawIconPath, UriKind.Absolute, out var uri) && uri.IsFile)
                {
                    var localPath = uri.LocalPath;
                    if (File.Exists(localPath)) return uri.AbsoluteUri;
                    fileName = Path.GetFileName(localPath);
                }
                else
                {
                    if (File.Exists(rawIconPath)) return new Uri(rawIconPath).AbsoluteUri;
                    fileName = Path.GetFileName(rawIconPath);
                }
            }
            catch
            {
                fileName = Path.GetFileName(rawIconPath.Replace('/', '\\'));
            }
        }

        // 3. Check in current ValGrid agentsimg folder
        var localAppData = string.IsNullOrEmpty(Constants.LocalAppDataPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
            : Constants.LocalAppDataPath;
        var valGridDir = Path.Combine(localAppData, "ValAPI", "agentsimg");

        if (!string.IsNullOrEmpty(fileName))
        {
            var localValGridPath = Path.Combine(valGridDir, fileName);
            if (File.Exists(localValGridPath))
            {
                return new Uri(localValGridPath).AbsoluteUri;
            }

            // If filename without ext is a Guid (e.g. add6443a-41bd-e414-f6ad-e58d267f4e95.png)
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            if (Guid.TryParse(nameWithoutExt, out var agentGuid))
            {
                return $"https://media.valorant-api.com/agents/{agentGuid}/displayicon.png";
            }
        }

        return "";
    }

    public static string CalculateRelativeTime(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return "";

        DateTime dt;
        string[] formats = { "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "o" };
        if (!DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
        {
            if (!DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out dt))
            {
                if (!DateTime.TryParse(dateStr, out dt))
                    return "";
            }
        }

        var localDt = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        var diff = DateTime.Now - localDt;
        if (diff < TimeSpan.Zero) diff = TimeSpan.Zero;

        var days = (int)diff.TotalDays;
        var hours = diff.Hours;
        var minutes = diff.Minutes;

        bool isEn = L10n.IsEnglish;

        if (days > 0)
        {
            if (hours > 0 && minutes > 0)
                return isEn ? $"{days}d {hours}h {minutes}m ago" : $"{days} gün {hours} saat {minutes} dk önce";
            if (hours > 0)
                return isEn ? $"{days}d {hours}h ago" : $"{days} gün {hours} saat önce";
            if (minutes > 0)
                return isEn ? $"{days}d {minutes}m ago" : $"{days} gün {minutes} dk önce";
            return isEn ? (days == 1 ? "1 day ago" : $"{days} days ago") : $"{days} gün önce";
        }

        if (hours > 0)
        {
            if (minutes > 0)
                return isEn ? $"{hours}h {minutes}m ago" : $"{hours} saat {minutes} dk önce";
            return isEn ? (hours == 1 ? "1 hour ago" : $"{hours} hours ago") : $"{hours} saat önce";
        }

        if (minutes > 0)
        {
            return isEn ? $"{minutes}m ago" : $"{minutes} dk önce";
        }

        return isEn ? "Just now" : "Az önce";
    }

    public static void RefreshRelativeTimes(IEnumerable<EncounterMatch> matches)
    {
        if (matches == null) return;
        foreach (var m in matches)
        {
            m.RelativeTime = CalculateRelativeTime(m.Date);
        }
    }
}

