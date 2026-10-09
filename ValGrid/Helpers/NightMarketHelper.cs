using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public class SavedNightMarketContainer
{
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public string AccountPuuid { get; set; } = "";
    public List<DailyStoreOffer> Offers { get; set; } = new();
}

public static class NightMarketHelper
{
    private static readonly string CacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ValGrid");
    private static readonly string RevealedPath = Path.Combine(CacheDir, "nightmarket_revealed.json");
    private static readonly string SavedPath = Path.Combine(CacheDir, "nightmarket_saved.json");
    private static readonly object FileLock = new();
    private static HashSet<string> _revealedSet;

    public static HashSet<string> GetRevealedSet()
    {
        lock (FileLock)
        {
            if (_revealedSet != null) return _revealedSet;

            try
            {
                if (File.Exists(RevealedPath))
                {
                    var json = File.ReadAllText(RevealedPath).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true
                        };
                        var list = JsonSerializer.Deserialize<List<string>>(json, options);
                        _revealedSet = new HashSet<string>(list ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        return _revealedSet;
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Warning("Error loading nightmarket_revealed: {e}", ex.Message);
            }

            _revealedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return _revealedSet;
        }
    }

    public static bool IsOfferRevealed(Guid offerId)
    {
        if (offerId == Guid.Empty) return true;
        return GetRevealedSet().Contains(offerId.ToString());
    }

    public static void SetOfferRevealed(Guid offerId)
    {
        if (offerId == Guid.Empty) return;
        lock (FileLock)
        {
            var set = GetRevealedSet();
            if (set.Add(offerId.ToString()))
            {
                SaveRevealedSet(set);
            }
        }
    }

    public static void RevealAll(IEnumerable<DailyStoreOffer> offers)
    {
        if (offers == null) return;
        lock (FileLock)
        {
            var set = GetRevealedSet();
            bool changed = false;
            foreach (var off in offers)
            {
                off.IsRevealed = true;
                if (off.OfferId != Guid.Empty && set.Add(off.OfferId.ToString()))
                {
                    changed = true;
                }
            }
            if (changed)
            {
                SaveRevealedSet(set);
            }
        }
    }

    public static void ResetAll(IEnumerable<DailyStoreOffer> offers)
    {
        if (offers == null) return;
        lock (FileLock)
        {
            var set = GetRevealedSet();
            set.Clear();
            foreach (var off in offers)
            {
                off.IsRevealed = false;
            }
            SaveRevealedSet(set);
        }
    }

    private static void SaveRevealedSet(HashSet<string> set)
    {
        try
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);
            var list = new List<string>(set);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RevealedPath, json, new System.Text.UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Error saving nightmarket_revealed: {e}", ex.Message);
        }
    }

    public static void SaveNightMarketOffers(List<DailyStoreOffer> offers, string puuid)
    {
        if (offers == null || offers.Count == 0) return;
        lock (FileLock)
        {
            try
            {
                if (!Directory.Exists(CacheDir))
                    Directory.CreateDirectory(CacheDir);

                var container = new SavedNightMarketContainer
                {
                    SavedAt = DateTime.UtcNow,
                    AccountPuuid = puuid ?? "",
                    Offers = offers
                };
                var json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavedPath, json, new System.Text.UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Constants.Log?.Warning("Error saving nightmarket_saved: {e}", ex.Message);
            }
        }
    }

    public static List<DailyStoreOffer> LoadSavedNightMarket()
    {
        lock (FileLock)
        {
            try
            {
                if (File.Exists(SavedPath))
                {
                    var json = File.ReadAllText(SavedPath).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true
                        };
                        var container = JsonSerializer.Deserialize<SavedNightMarketContainer>(json, options);
                        if (container?.Offers != null && container.Offers.Count > 0)
                        {
                            var revealedSet = GetRevealedSet();
                            foreach (var off in container.Offers)
                            {
                                off.IsNightMarket = true;
                                if (off.OfferId != Guid.Empty)
                                {
                                    off.IsRevealed = revealedSet.Contains(off.OfferId.ToString());
                                }
                            }
                            return container.Offers;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Warning("Error loading nightmarket_saved: {e}", ex.Message);
            }
            return null;
        }
    }

    public static List<DailyStoreOffer> GetSavedNightMarket()
    {
        return LoadSavedNightMarket();
    }
}

