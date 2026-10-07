using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ValGrid.Helpers;

public class FemaleTagEntry
{
    public string Puuid { get; set; } = "";
    public string Username { get; set; } = "";
    public string AddedAt { get; set; } = "";
}

public static class FemaleTagManager
{
    private static readonly object Lock = new();
    private static List<FemaleTagEntry> _entries;
    private static Dictionary<string, FemaleTagEntry> _entriesByPuuid;
    private static Dictionary<string, FemaleTagEntry> _entriesByUsername;

    public static event Action FemaleTagsChanged;

    private static string FilePath =>
        (string.IsNullOrEmpty(Constants.LocalAppDataPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
            : Constants.LocalAppDataPath) + "\\femaletags.json";

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
                var json = File.ReadAllText(FilePath);
                _entries = JsonSerializer.Deserialize<List<FemaleTagEntry>>(json) ?? new List<FemaleTagEntry>();
            }
            else
            {
                _entries = new List<FemaleTagEntry>();
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("FemaleTagManager load failed: {e}", e);
            _entries = new List<FemaleTagEntry>();
        }

        RebuildIndexes();
    }

    private static void RebuildIndexes()
    {
        _entriesByPuuid = new Dictionary<string, FemaleTagEntry>(StringComparer.OrdinalIgnoreCase);
        _entriesByUsername = new Dictionary<string, FemaleTagEntry>(StringComparer.OrdinalIgnoreCase);

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
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Constants.Log?.Error("FemaleTagManager save failed: {e}", e);
        }
    }

    public static bool IsFemale(Guid puuid, string username = null)
    {
        return IsFemale(CleanPuuid(puuid), username);
    }

    public static bool IsFemale(string puuidStr, string username = null)
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

    public static void SetFemale(Guid puuid, string username, bool isFemale)
    {
        SetFemale(CleanPuuid(puuid), username, isFemale);
    }

    public static void SetFemale(string puuidStr, string username, bool isFemale)
    {
        var pStr = CleanPuuid(puuidStr);
        var uStr = CleanUsername(username);

        if (string.IsNullOrEmpty(pStr) && string.IsNullOrEmpty(uStr))
            return;

        lock (Lock)
        {
            EnsureLoaded();

            if (isFemale)
            {
                // Mutually exclusive: if setting to female, remove from male tags
                try
                {
                    if (MaleTagManager.IsMale(pStr, uStr))
                    {
                        MaleTagManager.SetMale(pStr, uStr, false);
                    }
                }
                catch { }

                FemaleTagEntry entry = null;
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
                    entry = new FemaleTagEntry
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
            FemaleTagsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("FemaleTagsChanged event error: {e}", ex);
        }
    }

    public static void AssociateUsername(Guid puuid, string username)
    {
        AssociateUsername(CleanPuuid(puuid), username);
    }

    public static void AssociateUsername(string puuidStr, string username)
    {
        var pStr = CleanPuuid(puuidStr);
        var uStr = CleanUsername(username);

        if (string.IsNullOrEmpty(pStr) || string.IsNullOrEmpty(uStr))
            return;

        lock (Lock)
        {
            EnsureLoaded();
            if (_entriesByPuuid != null && _entriesByPuuid.TryGetValue(pStr, out var entry))
            {
                if (string.IsNullOrEmpty(entry.Username) || !string.Equals(entry.Username, uStr, StringComparison.OrdinalIgnoreCase))
                {
                    entry.Username = uStr;
                    RebuildIndexes();
                    Save();
                }
            }
        }
    }

    public static bool ToggleFemale(Guid puuid, string username)
    {
        var current = IsFemale(puuid, username);
        SetFemale(puuid, username, !current);
        return !current;
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
            FemaleTagsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("FemaleTagsChanged event error: {e}", ex);
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

    public static List<FemaleTagEntry> GetAll()
    {
        lock (Lock)
        {
            EnsureLoaded();
            return _entries.ToList();
        }
    }
}

