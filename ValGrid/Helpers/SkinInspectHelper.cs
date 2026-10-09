using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public static class SkinInspectHelper
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static JsonElement? _cachedSkinsArray;
    private static readonly object CacheLock = new();

    public static async Task<SkinInspectDetail> GetSkinInspectDetailAsync(DailyStoreOffer offer)
    {
        var detail = new SkinInspectDetail
        {
            SkinUuid = offer.ItemId,
            Name = offer.Name,
            WeaponType = offer.WeaponType,
            VpCost = offer.VpCost,
            TierDevName = offer.TierDevName,
            TierDisplayName = offer.TierDisplayName,
            TierColor = offer.TierColor,
            TierIconUrl = offer.TierIconUrl,
            DisplayIcon = offer.Image?.ToString() ?? ""
        };

        try
        {
            var skinsArray = await EnsureSkinsDataAsync().ConfigureAwait(false);
            if (skinsArray.HasValue && skinsArray.Value.ValueKind == JsonValueKind.Array)
            {
                var offerIdStr = offer.ItemId.ToString();
                var cleanOfferName = CleanSkinName(offer.Name);

                JsonElement matchedSkin = default;
                bool found = false;

                // 1. Try match by UUID (skin uuid, level uuid, or chroma uuid)
                foreach (var s in skinsArray.Value.EnumerateArray())
                {
                    if (s.TryGetProperty("uuid", out var suuid) &&
                        suuid.GetString()?.Equals(offerIdStr, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        matchedSkin = s;
                        found = true;
                        break;
                    }

                    if (s.TryGetProperty("levels", out var lvls) && lvls.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var l in lvls.EnumerateArray())
                        {
                            if (l.TryGetProperty("uuid", out var luuid) &&
                                luuid.GetString()?.Equals(offerIdStr, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                matchedSkin = s;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;

                    if (s.TryGetProperty("chromas", out var chrs) && chrs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in chrs.EnumerateArray())
                        {
                            if (c.TryGetProperty("uuid", out var cuuid) &&
                                cuuid.GetString()?.Equals(offerIdStr, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                matchedSkin = s;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;
                }

                // 2. Try match by clean skin name if UUID didn't match directly
                if (!found && !string.IsNullOrEmpty(cleanOfferName))
                {
                    foreach (var s in skinsArray.Value.EnumerateArray())
                    {
                        if (s.TryGetProperty("displayName", out var dName))
                        {
                            var sName = CleanSkinName(dName.GetString());
                            if (sName.Equals(cleanOfferName, StringComparison.OrdinalIgnoreCase) ||
                                cleanOfferName.Contains(sName, StringComparison.OrdinalIgnoreCase) ||
                                sName.Contains(cleanOfferName, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedSkin = s;
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (found)
                {
                    PopulateSkinDetail(detail, matchedSkin);
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("GetSkinInspectDetailAsync exception: {e}", ex.Message);
        }

        // Fallbacks if levels or chromas were empty
        if (detail.Levels.Count == 0)
        {
            detail.Levels.Add(new SkinLevelModel
            {
                Uuid = offer.ItemId,
                DisplayName = offer.Name,
                LevelNumber = 1,
                LevelTypeName = L10n.GetLevelTypeName(1, "", false),
                DisplayIcon = offer.Image?.ToString(),
                IsSelected = true
            });
        }
        else
        {
            // Select the highest level or level 1 by default
            var lastLevel = detail.Levels.LastOrDefault(l => l.HasVideo) ?? detail.Levels.LastOrDefault();
            if (lastLevel != null)
                lastLevel.IsSelected = true;
            else if (detail.Levels.Count > 0)
                detail.Levels[0].IsSelected = true;
        }

        if (detail.Chromas.Count == 0)
        {
            detail.Chromas.Add(new SkinChromaModel
            {
                Uuid = offer.ItemId,
                DisplayName = offer.Name,
                CleanStyleName = L10n.GetDefaultVariantName(),
                DisplayIcon = offer.Image?.ToString(),
                FullRenderUrl = offer.Image?.ToString(),
                IsSelected = true
            });
        }
        else
        {
            detail.Chromas[0].IsSelected = true;
        }

        return detail;
    }

    private static void PopulateSkinDetail(SkinInspectDetail detail, JsonElement skin)
    {
        if (skin.TryGetProperty("displayName", out var dn) && !string.IsNullOrEmpty(dn.GetString()))
            detail.Name = dn.GetString();

        if (skin.TryGetProperty("wallpaper", out var wp))
            detail.WallpaperUrl = wp.GetString();

        // Parse Levels
        if (skin.TryGetProperty("levels", out var lvls) && lvls.ValueKind == JsonValueKind.Array)
        {
            int num = 1;
            foreach (var l in lvls.EnumerateArray())
            {
                var lvlModel = new SkinLevelModel
                {
                    LevelNumber = num,
                    DisplayName = l.TryGetProperty("displayName", out var ld) ? ld.GetString() ?? "" : "",
                    DisplayIcon = l.TryGetProperty("displayIcon", out var li) ? li.GetString() : null,
                    VideoUrl = l.TryGetProperty("streamedVideo", out var lv) ? lv.GetString() : null
                };

                if (l.TryGetProperty("uuid", out var lu) && Guid.TryParse(lu.GetString(), out var lGuid))
                    lvlModel.Uuid = lGuid;

                string itemType = l.TryGetProperty("levelItem", out var lItem) ? lItem.GetString() : null;
                lvlModel.LevelItem = itemType ?? "";

                lvlModel.LevelTypeName = L10n.GetLevelTypeName(num, itemType, lvlModel.HasVideo);

                detail.Levels.Add(lvlModel);
                num++;
            }
        }

        // Get default finisher video if available
        var fallbackFinisherVideo = detail.Levels.LastOrDefault(l => l.HasVideo)?.VideoUrl;

        // Parse Chromas (Variants)
        if (skin.TryGetProperty("chromas", out var chrs) && chrs.ValueKind == JsonValueKind.Array)
        {
            int cIndex = 0;
            foreach (var c in chrs.EnumerateArray())
            {
                var cModel = new SkinChromaModel
                {
                    DisplayName = c.TryGetProperty("displayName", out var cd) ? cd.GetString() ?? "" : "",
                    DisplayIcon = c.TryGetProperty("displayIcon", out var ci) ? ci.GetString() : null,
                    FullRenderUrl = c.TryGetProperty("fullRender", out var cf) ? cf.GetString() : null,
                    SwatchUrl = c.TryGetProperty("swatch", out var cs) ? cs.GetString() : null,
                    VideoUrl = c.TryGetProperty("streamedVideo", out var cv) ? cv.GetString() : null
                };

                if (c.TryGetProperty("uuid", out var cu) && Guid.TryParse(cu.GetString(), out var cGuid))
                    cModel.Uuid = cGuid;

                // Style 0 default chroma video fallback
                if (string.IsNullOrEmpty(cModel.VideoUrl) && fallbackFinisherVideo != null)
                {
                    cModel.VideoUrl = fallbackFinisherVideo;
                }

                cModel.CleanStyleName = ExtractCleanStyleName(cModel.DisplayName, cIndex);

                detail.Chromas.Add(cModel);
                cIndex++;
            }
        }
    }

    private static string ExtractCleanStyleName(string displayName, int index)
    {
        if (index == 0) return L10n.GetDefaultVariantName();
        if (string.IsNullOrWhiteSpace(displayName)) return L10n.IsEnglish ? $"Variant {index + 1}" : $"Varyant {index + 1}";

        if (displayName.Contains('\n'))
        {
            var parts = displayName.Split('\n');
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                var s = parts[1].Trim().Trim('(', ')');
                if (s.StartsWith("Stil", StringComparison.OrdinalIgnoreCase) ||
                    s.StartsWith("Style", StringComparison.OrdinalIgnoreCase) ||
                    s.StartsWith("Variant", StringComparison.OrdinalIgnoreCase))
                {
                    var dashIdx = s.IndexOf('-');
                    if (dashIdx >= 0 && dashIdx + 1 < s.Length)
                        return s[(dashIdx + 1)..].Trim();
                }
                return s;
            }
        }

        var openParen = displayName.IndexOf('(');
        var closeParen = displayName.IndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var sub = displayName.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            var dashIdx = sub.IndexOf('-');
            if (dashIdx >= 0 && dashIdx + 1 < sub.Length)
                return sub[(dashIdx + 1)..].Trim();
            return sub;
        }

        return L10n.IsEnglish ? $"Variant {index + 1}" : $"Varyant {index + 1}";
    }

    private static string CleanSkinName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        // Remove "1. Seviye", "2. Seviye", etc.
        var cleaned = name;
        if (cleaned.Contains(". Seviye "))
        {
            var idx = cleaned.IndexOf(". Seviye ");
            if (idx >= 0 && idx + 9 < cleaned.Length)
                cleaned = cleaned[(idx + 9)..].Trim();
        }
        if (cleaned.Contains('\n'))
            cleaned = cleaned.Split('\n')[0].Trim();
        return cleaned.Trim();
    }

    private static string _cachedLanguage;

    public static async Task<JsonElement?> EnsureSkinsDataAsync()
    {
        var activeLang = L10n.ValApiLanguage;
        lock (CacheLock)
        {
            if (_cachedSkinsArray.HasValue && _cachedLanguage == activeLang)
                return _cachedSkinsArray.Value;
        }

        try
        {
            var localPath = Path.Combine(Constants.LocalAppDataPath ?? "", "ValAPI", $"allskins_{activeLang}.json");

            // If cached file exists and is less than 3 days old, use it
            if (File.Exists(localPath))
            {
                var fi = new FileInfo(localPath);
                if ((DateTime.UtcNow - fi.LastWriteTimeUtc).TotalDays < 3)
                {
                    var text = (await File.ReadAllTextAsync(localPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    using var doc = JsonDocument.Parse(text);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        lock (CacheLock)
                        {
                            _cachedLanguage = activeLang;
                            _cachedSkinsArray = data.Clone();
                            return _cachedSkinsArray.Value;
                        }
                    }
                }
            }

            // Otherwise, fetch from valorant-api.com
            var response = await HttpClient.GetStringAsync($"https://valorant-api.com/v1/weapons/skins?language={activeLang}").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var dir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    await File.WriteAllTextAsync(localPath, response).ConfigureAwait(false);
                }
                catch { }

                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    lock (CacheLock)
                    {
                        _cachedLanguage = activeLang;
                        _cachedSkinsArray = data.Clone();
                        return _cachedSkinsArray.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("EnsureSkinsDataAsync failed: {e}", ex.Message);
        }

        return null;
    }
}

