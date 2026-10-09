using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ValGrid.Helpers;

public class BlacklistEntry
{
    public string Puuid { get; set; } = "";
    public string Username { get; set; } = "";
    public string AddedAt { get; set; } = "";
}

public static class BlacklistManager
{
    private static readonly object Lock = new();
    private static List<BlacklistEntry> _entries;
    private static Dictionary<string, BlacklistEntry> _entriesByPuuid;
    private static Dictionary<string, BlacklistEntry> _entriesByUsername;

    public static event Action BlacklistChanged;

    private static string FilePath =>
        (string.IsNullOrEmpty(Constants.LocalAppDataPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
            : Constants.LocalAppDataPath) + "\\blacklist.json";

    public static string CleanPuuid(Guid puuid)
    {
        return (puuid == Guid.Empty) ? "" : puuid.ToString().ToLowerInvariant();
    }

    public static string CleanPuuid(string puuidStr)
    {
        if (string.IsNullOrWhiteSpace(puuidStr)) return "";
        if (Guid.TryParse(puuidStr, out var g) && g != Guid.Empty)
            return g.ToString().ToLowerInvariant();
        return puuidStr.Trim().ToLowerInvariant();
    }

    public static string CleanUsername(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw.Replace(" 🔗", "").Replace(" 👁", "").Trim();
        if (s == "----" ||
            s.Equals("oyuncu", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("bilinmeyen", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }
        return s;
    }

    private static void EnsureLoaded()
    {
        if (_entries != null) return;

        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath).Trim().Trim('\uFEFF', '\u200B');
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    try
                    {
                        _entries = JsonSerializer.Deserialize<List<BlacklistEntry>>(json, options) ?? new List<BlacklistEntry>();
                    }
                    catch
                    {
                        try
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, BlacklistEntry>>(json, options);
                            if (dict != null)
                                _entries = dict.Values.ToList();
                            else
                                _entries = new List<BlacklistEntry>();
                        }
                        catch
                        {
                            var single = JsonSerializer.Deserialize<BlacklistEntry>(json, options);
                            if (single != null && (!string.IsNullOrEmpty(single.Puuid) || !string.IsNullOrEmpty(single.Username)))
                                _entries = new List<BlacklistEntry> { single };
                            else
                                throw;
                        }
                    }
                }
                else
                {
                    _entries = new List<BlacklistEntry>();
                }
            }
            else
            {
                _entries = new List<BlacklistEntry>();
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("BlacklistManager load failed: {e}", e);
            try
            {
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 0)
                {
                    File.Copy(FilePath, FilePath + ".corrupt.bak", true);
                }
            }
            catch { }
            _entries = new List<BlacklistEntry>();
        }

        RebuildIndexes();
    }

    private static void RebuildIndexes()
    {
        _entriesByPuuid = new Dictionary<string, BlacklistEntry>(StringComparer.OrdinalIgnoreCase);
        _entriesByUsername = new Dictionary<string, BlacklistEntry>(StringComparer.OrdinalIgnoreCase);

        if (_entries == null) return;

        foreach (var entry in _entries)
        {
            var p = CleanPuuid(entry.Puuid);
            if (!string.IsNullOrEmpty(p) && !_entriesByPuuid.ContainsKey(p))
            {
                _entriesByPuuid[p] = entry;
            }

            var u = CleanUsername(entry.Username);
            if (!string.IsNullOrEmpty(u) && !_entriesByUsername.ContainsKey(u))
            {
                _entriesByUsername[u] = entry;
            }
        }
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json, new System.Text.UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Constants.Log?.Error("BlacklistManager save failed: {e}", e);
        }
    }

    public static bool IsBlacklisted(Guid puuid, string username = null)
    {
        return IsBlacklisted(CleanPuuid(puuid), username);
    }

    public static bool IsBlacklisted(string puuidStr, string username = null)
    {
        var pStr = CleanPuuid(puuidStr);
        var uStr = CleanUsername(username);

        lock (Lock)
        {
            EnsureLoaded();

            if (!string.IsNullOrEmpty(pStr) && _entriesByPuuid.ContainsKey(pStr))
                return true;

            if (!string.IsNullOrEmpty(uStr) && _entriesByUsername.ContainsKey(uStr))
                return true;

            return false;
        }
    }

    public static void SetBlacklisted(Guid puuid, string username, bool isBlacklisted)
    {
        SetBlacklisted(CleanPuuid(puuid), username, isBlacklisted);
    }

    public static void SetBlacklisted(string puuidStr, string username, bool isBlacklisted)
    {
        var pStr = CleanPuuid(puuidStr);
        var uStr = CleanUsername(username);

        if (string.IsNullOrEmpty(pStr) && string.IsNullOrEmpty(uStr))
            return;

        lock (Lock)
        {
            EnsureLoaded();

            if (isBlacklisted)
            {
                BlacklistEntry entry = null;
                if (!string.IsNullOrEmpty(pStr) && _entriesByPuuid.TryGetValue(pStr, out var existingP))
                {
                    entry = existingP;
                }
                else if (!string.IsNullOrEmpty(uStr) && _entriesByUsername.TryGetValue(uStr, out var existingU))
                {
                    entry = existingU;
                }

                if (entry == null)
                {
                    entry = new BlacklistEntry
                    {
                        Puuid = pStr,
                        Username = uStr,
                        AddedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    _entries.Add(entry);
                }
                else
                {
                    if (string.IsNullOrEmpty(entry.Puuid) && !string.IsNullOrEmpty(pStr))
                        entry.Puuid = pStr;
                    if (string.IsNullOrEmpty(entry.Username) && !string.IsNullOrEmpty(uStr))
                        entry.Username = uStr;
                }

                RebuildIndexes();
                Save();
            }
            else
            {
                var toRemove = _entries.Where(e =>
                    (!string.IsNullOrEmpty(pStr) && string.Equals(CleanPuuid(e.Puuid), pStr, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(uStr) && string.Equals(CleanUsername(e.Username), uStr, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (toRemove.Count > 0)
                {
                    foreach (var r in toRemove)
                    {
                        _entries.Remove(r);
                    }
                    RebuildIndexes();
                    Save();
                }
            }
        }

        try
        {
            BlacklistChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("BlacklistChanged event error: {e}", ex);
        }
    }

    public static void Clear()
    {
        lock (Lock)
        {
            EnsureLoaded();
            _entries.Clear();
            _entriesByPuuid.Clear();
            _entriesByUsername.Clear();
            Save();
        }

        try
        {
            BlacklistChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("BlacklistChanged event error: {e}", ex);
        }
    }

    public static int Count
    {
        get
        {
            lock (Lock)
            {
                EnsureLoaded();
                return _entries.Count;
            }
        }
    }

    public static List<BlacklistEntry> GetAll()
    {
        lock (Lock)
        {
            EnsureLoaded();
            return _entries.ToList();
        }
    }
}

