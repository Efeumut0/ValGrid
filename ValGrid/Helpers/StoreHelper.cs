using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public static class StoreHelper
{
    public const string VpCurrencyUuid = "85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741";
    public const string RpCurrencyUuid = "e59aa87c-4cbf-517a-5983-6e81511be9b7";
    public const string KcCurrencyUuid = "85ca954a-41f2-ce94-9b45-8ca3dd39a00d";

    public const string VpIconUrl = "https://media.valorant-api.com/currencies/85ad13f7-3d1b-5128-9eb2-7cd8ee0b5741/largeicon.png";
    public const string RpIconUrl = "https://media.valorant-api.com/currencies/e59aa87c-4cbf-517a-5983-6e81511be9b7/largeicon.png";
    public const string KcIconUrl = "https://media.valorant-api.com/currencies/85ca954a-41f2-ce94-9b45-8ca3dd39a00d/largeicon.png";

    private static Dictionary<Guid, ValNameImage> _skinCache;
    private static Dictionary<Guid, ValSkinMeta> _metaCache;
    private static Dictionary<Guid, ValNameImage> _bundleCache;
    private static Dictionary<Guid, ValNameImage> _cardsCache;
    private static Dictionary<Guid, ValNameImage> _spraysCache;
    private static Dictionary<Guid, ValNameImage> _buddiesCache;
    private static List<DailyStoreOffer> _allWeaponsCatalogCache;
    private static readonly object CacheLock = new();
    private static readonly System.Net.Http.HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };

    private static string _cacheLoadedLanguage;

    public static async Task EnsureCachesLoadedAsync()
    {
        var activeLang = L10n.ValApiLanguage;
        lock (CacheLock)
        {
            if (_skinCache != null && _metaCache != null && _bundleCache != null &&
                _cardsCache != null && _spraysCache != null && _buddiesCache != null &&
                _cacheLoadedLanguage == activeLang)
                return;
        }

        try
        {
            var basePath = Constants.LocalAppDataPath + "\\ValAPI";
            var chromasPath = Path.Combine(basePath, $"skinchromas_{activeLang}.json");
            if (!File.Exists(chromasPath)) chromasPath = Path.Combine(basePath, "skinchromas.json");

            var metaPath = Path.Combine(basePath, $"skinmeta_{activeLang}.json");
            if (!File.Exists(metaPath)) metaPath = Path.Combine(basePath, "skinmeta.json");

            var bundlesPath = Path.Combine(basePath, $"bundles_{activeLang}.json");
            if (!File.Exists(bundlesPath)) bundlesPath = Path.Combine(basePath, "bundles.json");

            var cardsPath = Path.Combine(basePath, $"cards_{activeLang}.json");
            if (!File.Exists(cardsPath)) cardsPath = Path.Combine(basePath, "cards.json");

            var spraysPath = Path.Combine(basePath, $"sprays_{activeLang}.json");
            if (!File.Exists(spraysPath)) spraysPath = Path.Combine(basePath, "sprays.json");

            var buddiesPath = Path.Combine(basePath, $"buddies_{activeLang}.json");
            if (!File.Exists(buddiesPath)) buddiesPath = Path.Combine(basePath, "buddies.json");

            Dictionary<Guid, ValNameImage> skins = null;
            if (File.Exists(chromasPath))
            {
                var json = await File.ReadAllTextAsync(chromasPath).ConfigureAwait(false);
                skins = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
            }

            Dictionary<Guid, ValSkinMeta> metas = null;
            if (File.Exists(metaPath))
            {
                var json = await File.ReadAllTextAsync(metaPath).ConfigureAwait(false);
                metas = JsonSerializer.Deserialize<Dictionary<Guid, ValSkinMeta>>(json);
            }

            Dictionary<Guid, ValNameImage> bundles = null;
            if (File.Exists(bundlesPath))
            {
                var json = await File.ReadAllTextAsync(bundlesPath).ConfigureAwait(false);
                bundles = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
            }

            Dictionary<Guid, ValNameImage> cards = null;
            if (File.Exists(cardsPath))
            {
                var json = await File.ReadAllTextAsync(cardsPath).ConfigureAwait(false);
                cards = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
            }

            Dictionary<Guid, ValNameImage> sprays = null;
            if (File.Exists(spraysPath))
            {
                var json = await File.ReadAllTextAsync(spraysPath).ConfigureAwait(false);
                sprays = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
            }

            Dictionary<Guid, ValNameImage> buddies = null;
            if (File.Exists(buddiesPath))
            {
                var json = await File.ReadAllTextAsync(buddiesPath).ConfigureAwait(false);
                buddies = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
            }

            lock (CacheLock)
            {
                _cacheLoadedLanguage = activeLang;
                _skinCache = skins ?? new Dictionary<Guid, ValNameImage>();
                _metaCache = metas ?? new Dictionary<Guid, ValSkinMeta>();
                _bundleCache = bundles ?? new Dictionary<Guid, ValNameImage>();
                _cardsCache = cards ?? new Dictionary<Guid, ValNameImage>();
                _spraysCache = sprays ?? new Dictionary<Guid, ValNameImage>();
                _buddiesCache = buddies ?? new Dictionary<Guid, ValNameImage>();
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("StoreHelper.EnsureCachesLoadedAsync failed: {e}", ex);
        }
    }

    public static async Task<DailyStoreData> GetDailyStoreAsync()
    {
        var data = new DailyStoreData();

        if (string.IsNullOrEmpty(Constants.AccessToken) || string.IsNullOrEmpty(Constants.EntitlementToken))
        {
            Constants.Log?.Warning("GetDailyStoreAsync: Tokens are missing.");
            return data;
        }

        if (string.IsNullOrEmpty(Constants.Region))
            Constants.Region = "eu";

        if (string.IsNullOrEmpty(Constants.Version))
        {
            try
            {
                var versionFile = Path.Combine(Constants.LocalAppDataPath ?? "", "ValAPI", "version.json");
                if (File.Exists(versionFile))
                {
                    var lines = File.ReadAllLines(versionFile);
                    if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                        Constants.Version = lines[0].Trim();
                }
            }
            catch { }
        }

        await EnsureCachesLoadedAsync().ConfigureAwait(false);

        var client = new RestClient($"https://pd.{Constants.Region}.a.pvp.net");

        // 1. Fetch Storefront (V3 POST is standard, with fallback to V3 GET / V2 POST / V2 GET)
        try
        {
            RestResponse response = null;

            // Attempt 1: POST /store/v3/storefront/{puuid} (Current Valorant client standard)
            try
            {
                var v3PostReq = new RestRequest($"/store/v3/storefront/{Constants.Ppuuid}", Method.Post);
                Login.AddAuthToRequest(v3PostReq);
                v3PostReq.AddStringBody("{}", DataFormat.Json);
                var v3PostResp = await client.ExecuteAsync(v3PostReq).ConfigureAwait(false);
                if (v3PostResp.IsSuccessful && !string.IsNullOrEmpty(v3PostResp.Content))
                {
                    response = v3PostResp;
                }
                else
                {
                    Constants.Log?.Warning("Storefront V3 POST returned {status}: {err}", v3PostResp.StatusCode, v3PostResp.ErrorMessage);
                }
            }
            catch (Exception exV3)
            {
                Constants.Log?.Warning("Storefront V3 POST exception: {e}", exV3.Message);
            }

            // Attempt 2: GET /store/v3/storefront/{puuid}
            if (response == null)
            {
                try
                {
                    var v3GetReq = new RestRequest($"/store/v3/storefront/{Constants.Ppuuid}", Method.Get);
                    Login.AddAuthToRequest(v3GetReq);
                    var v3GetResp = await client.ExecuteGetAsync(v3GetReq).ConfigureAwait(false);
                    if (v3GetResp.IsSuccessful && !string.IsNullOrEmpty(v3GetResp.Content))
                    {
                        response = v3GetResp;
                    }
                    else
                    {
                        Constants.Log?.Warning("Storefront V3 GET returned {status}: {err}", v3GetResp.StatusCode, v3GetResp.ErrorMessage);
                    }
                }
                catch { }
            }

            // Attempt 3: POST /store/v2/storefront/{puuid}
            if (response == null)
            {
                try
                {
                    var v2PostReq = new RestRequest($"/store/v2/storefront/{Constants.Ppuuid}", Method.Post);
                    Login.AddAuthToRequest(v2PostReq);
                    v2PostReq.AddStringBody("{}", DataFormat.Json);
                    var v2PostResp = await client.ExecuteAsync(v2PostReq).ConfigureAwait(false);
                    if (v2PostResp.IsSuccessful && !string.IsNullOrEmpty(v2PostResp.Content))
                    {
                        response = v2PostResp;
                    }
                }
                catch { }
            }

            // Attempt 4: GET /store/v2/storefront/{puuid}
            if (response == null)
            {
                try
                {
                    var v2GetReq = new RestRequest($"/store/v2/storefront/{Constants.Ppuuid}", Method.Get);
                    Login.AddAuthToRequest(v2GetReq);
                    var v2GetResp = await client.ExecuteGetAsync(v2GetReq).ConfigureAwait(false);
                    if (v2GetResp.IsSuccessful && !string.IsNullOrEmpty(v2GetResp.Content))
                    {
                        response = v2GetResp;
                    }
                }
                catch { }
            }

            if (response != null && response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                Constants.Log?.Information("Storefront fetched successfully. Content length: {len}", response.Content.Length);
                using var doc = JsonDocument.Parse(response.Content);
                var root = doc.RootElement;

                // 1. Featured Bundles (FeaturedBundle)
                JsonElement featuredBundleProp = default;
                bool foundFeatured = false;
                if (root.TryGetProperty("FeaturedBundle", out featuredBundleProp))
                {
                    foundFeatured = true;
                }
                else
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals("FeaturedBundle", StringComparison.OrdinalIgnoreCase))
                        {
                            featuredBundleProp = prop.Value;
                            foundFeatured = true;
                            break;
                        }
                    }
                }

                if (foundFeatured)
                {
                    var bundleListElements = new List<JsonElement>();
                    if (featuredBundleProp.TryGetProperty("Bundles", out var multiBundles) &&
                        multiBundles.ValueKind == JsonValueKind.Array &&
                        multiBundles.GetArrayLength() > 0)
                    {
                        foreach (var b in multiBundles.EnumerateArray())
                        {
                            bundleListElements.Add(b);
                        }
                    }
                    else if (featuredBundleProp.TryGetProperty("Bundle", out var singleBundle))
                    {
                        bundleListElements.Add(singleBundle);
                    }

                    var seenBundles = new HashSet<Guid>();
                    foreach (var bEl in bundleListElements)
                    {
                        var bundleOffer = ParseBundleElement(bEl);
                        if (bundleOffer != null && seenBundles.Add(bundleOffer.DataAssetId))
                        {
                            data.FeaturedBundles.Add(bundleOffer);
                        }
                    }
                }

                // Daily Offers (SkinsPanelLayout - case-insensitive)
                JsonElement panel = default;
                bool foundPanel = false;
                if (root.TryGetProperty("SkinsPanelLayout", out panel))
                {
                    foundPanel = true;
                }
                else
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals("SkinsPanelLayout", StringComparison.OrdinalIgnoreCase))
                        {
                            panel = prop.Value;
                            foundPanel = true;
                            break;
                        }
                    }
                }

                if (foundPanel)
                {
                    if (panel.TryGetProperty("SingleItemOffersRemainingDurationInSeconds", out var remSec))
                    {
                        var seconds = remSec.GetInt32();
                        data.RemainingDurationSeconds = seconds;
                        data.RemainingTimeFormatted = FormatCountdown(seconds);
                    }

                    // Map of OfferID to VP cost
                    var costs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    if (panel.TryGetProperty("SingleItemStoreOffers", out var storeOffers))
                    {
                        foreach (var offer in storeOffers.EnumerateArray())
                        {
                            var offerId = offer.TryGetProperty("OfferID", out var oid) ? oid.GetString() : null;
                            var cost = 0;
                            if (offer.TryGetProperty("Cost", out var costObj))
                            {
                                if (costObj.TryGetProperty(VpCurrencyUuid, out var vpVal))
                                    cost = vpVal.GetInt32();
                                else
                                {
                                    foreach (var prop in costObj.EnumerateObject())
                                    {
                                        cost = prop.Value.GetInt32();
                                        break;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(offerId))
                            {
                                costs[offerId] = cost;
                            }
                        }
                    }

                    if (panel.TryGetProperty("SingleItemOffers", out var itemOffers))
                    {
                        foreach (var item in itemOffers.EnumerateArray())
                        {
                            string idStr = null;
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                idStr = item.GetString();
                            }
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                if (item.TryGetProperty("OfferID", out var oid) || item.TryGetProperty("offerId", out oid))
                                    idStr = oid.GetString();
                                else if (item.TryGetProperty("Rewards", out var rew) && rew.ValueKind == JsonValueKind.Array && rew.GetArrayLength() > 0)
                                {
                                    var firstRew = rew[0];
                                    if (firstRew.TryGetProperty("ItemID", out var iid))
                                        idStr = iid.GetString();
                                }
                            }

                            if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var itemGuid))
                            {
                                var cost = costs.TryGetValue(idStr, out var c) ? c : 0;
                                var offerObj = ResolveSkinOffer(itemGuid, cost);
                                data.DailyOffers.Add(offerObj);
                            }
                        }
                    }
                }
                else
                {
                    Constants.Log?.Warning("SkinsPanelLayout not found in storefront JSON response.");
                }

                // Night Market (BonusStore)
                if (root.TryGetProperty("BonusStore", out var bonusStore) &&
                    bonusStore.TryGetProperty("BonusStoreOffers", out var bonusOffers))
                {
                    foreach (var bo in bonusOffers.EnumerateArray())
                    {
                        if (bo.TryGetProperty("Offer", out var offerProp))
                        {
                            var offerIdStr = offerProp.TryGetProperty("OfferID", out var bOid) ? bOid.GetString() : null;
                            var origCost = 0;
                            if (offerProp.TryGetProperty("Cost", out var bCostObj))
                            {
                                if (bCostObj.TryGetProperty(VpCurrencyUuid, out var bVp))
                                    origCost = bVp.GetInt32();
                                else
                                {
                                    foreach (var prop in bCostObj.EnumerateObject())
                                    {
                                        origCost = prop.Value.GetInt32();
                                        break;
                                    }
                                }
                            }

                            var discountedCost = 0;
                            if (bo.TryGetProperty("DiscountCosts", out var discCostObj))
                            {
                                if (discCostObj.TryGetProperty(VpCurrencyUuid, out var dVp))
                                    discountedCost = dVp.GetInt32();
                                else
                                {
                                    foreach (var prop in discCostObj.EnumerateObject())
                                    {
                                        discountedCost = prop.Value.GetInt32();
                                        break;
                                    }
                                }
                            }

                            var discountPercent = 0;
                            if (bo.TryGetProperty("DiscountPercent", out var dp))
                                discountPercent = dp.GetInt32();

                            if (Guid.TryParse(offerIdStr, out var offerGuid))
                            {
                                var nmOffer = ResolveSkinOffer(offerGuid, discountedCost);
                                nmOffer.OfferId = offerGuid;
                                nmOffer.IsNightMarket = true;
                                nmOffer.OriginalVpCost = origCost;
                                nmOffer.DiscountPercentage = discountPercent;

                                bool isSeenInGame = bo.TryGetProperty("IsSeen", out var isSeenProp) && isSeenProp.GetBoolean();
                                nmOffer.IsRevealed = isSeenInGame || NightMarketHelper.IsOfferRevealed(offerGuid);

                                data.NightMarketOffers.Add(nmOffer);
                            }
                        }
                    }
                }

                // If Night Market offers were found, save them to history
                if (data.NightMarketOffers.Count > 0)
                {
                    NightMarketHelper.SaveNightMarketOffers(data.NightMarketOffers, Constants.Ppuuid.ToString());
                }
            }
            else
            {
                Constants.Log?.Warning("Storefront request failed on all attempts: {status} {err}",
                    response?.StatusCode, response?.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("Error fetching storefront: {e}", ex);
        }


        // Fallback Featured Bundles if none returned from live storefront
        if (data.FeaturedBundles.Count == 0)
        {
            data.FeaturedBundles = GetDefaultFeaturedBundles();
        }

        // 2. Fetch Wallet (VP & Radianite)
        try
        {
            var walletRequest = new RestRequest($"/store/v1/wallet/{Constants.Ppuuid}");
            Login.AddAuthToRequest(walletRequest);

            var walletResp = await client.ExecuteGetAsync(walletRequest).ConfigureAwait(false);
            if (walletResp.IsSuccessful && !string.IsNullOrEmpty(walletResp.Content))
            {
                using var wdoc = JsonDocument.Parse(walletResp.Content);
                if (wdoc.RootElement.TryGetProperty("Balances", out var balances))
                {
                    if (balances.TryGetProperty(VpCurrencyUuid, out var vp))
                        data.VpBalance = vp.GetInt32();
                    if (balances.TryGetProperty(RpCurrencyUuid, out var rp))
                        data.RadianiteBalance = rp.GetInt32();
                    if (balances.TryGetProperty(KcCurrencyUuid, out var kc))
                        data.KcBalance = kc.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Error fetching wallet: {e}", ex.Message);
        }

        return data;
    }

    private static DailyStoreOffer ResolveSkinOffer(Guid itemId, int vpCost)
    {
        var offer = new DailyStoreOffer
        {
            ItemId = itemId,
            VpCost = vpCost
        };

        ValNameImage skinInfo = null;
        lock (CacheLock)
        {
            _skinCache?.TryGetValue(itemId, out skinInfo);
        }

        if (skinInfo != null)
        {
            offer.Name = skinInfo.Name;
            offer.Image = skinInfo.Image;
        }

        // Detect weapon type early
        offer.WeaponType = DetectWeaponType(offer.Name);
        bool isMelee = IsMeleeWeapon(offer.Name, offer.WeaponType);
        if (isMelee) offer.WeaponType = L10n.IsEnglish ? "MELEE" : "YAKIN DÖVÜŞ";

        ValSkinMeta meta = null;
        lock (CacheLock)
        {
            _metaCache?.TryGetValue(itemId, out meta);
        }

        if (meta != null)
        {
            if (offer.VpCost <= 0 && meta.VpCost > 0)
                offer.VpCost = meta.VpCost;

            offer.TierDevName = meta.TierDevName ?? "Standard";
            offer.TierColor = meta.TierColor ?? "#7f8c8d";
            offer.IsRare = meta.IsRare;
            offer.RarityLabel = meta.RarityLabel ?? "";
            if (meta.IsMelee)
            {
                isMelee = true;
                offer.WeaponType = L10n.IsEnglish ? "MELEE" : "YAKIN DÖVÜŞ";
            }
        }

        if (offer.TierDevName is "Standard" or null)
        {
            var inferred = InferTierFromKeywords(offer.Name);
            if (!string.IsNullOrEmpty(inferred))
                offer.TierDevName = inferred;
        }

        // Apply authoritative tier colors, display name and CDN icon
        ApplyTierDetails(offer);

        // Fallback / Validation: Ensure cost is accurate official Valorant price matrix
        if (offer.VpCost <= 0)
        {
            offer.VpCost = GetAccurateSkinPrice(offer.Name, offer.TierDevName, isMelee);
        }

        return offer;
    }

    public static void ApplyTierDetails(DailyStoreOffer offer)
    {
        var dev = offer.TierDevName?.ToLowerInvariant() ?? "";
        offer.TierDisplayName = L10n.GetTierDisplayName(dev);
        switch (dev)
        {
            case "ultra":
                offer.TierColor = "#FAD663";
                offer.TierIconUrl = "https://media.valorant-api.com/contenttiers/411e4a55-4e59-7757-41f0-86a53f101bb5/displayicon.png";
                break;
            case "exclusive":
                offer.TierColor = "#F5955B";
                offer.TierIconUrl = "https://media.valorant-api.com/contenttiers/e046854e-406c-37f4-6607-19a9ba8426fc/displayicon.png";
                break;
            case "premium":
                offer.TierColor = "#D1548D";
                offer.TierIconUrl = "https://media.valorant-api.com/contenttiers/60bca009-4182-7998-dee7-b8a2558dc369/displayicon.png";
                break;
            case "deluxe":
                offer.TierColor = "#009587";
                offer.TierIconUrl = "https://media.valorant-api.com/contenttiers/0cebb8be-46d7-c12a-d306-e9907bfc5a25/displayicon.png";
                break;
            case "select":
                offer.TierColor = "#5A9FE2";
                offer.TierIconUrl = "https://media.valorant-api.com/contenttiers/12683d76-48d7-84a3-4e09-6985794f0445/displayicon.png";
                break;
            default:
                break;
        }
    }

    public static string FormatCountdown(int totalSeconds)
    {
        return L10n.FormatCountdown(totalSeconds);
    }

    public static bool IsMeleeWeapon(string skinName, string weaponType = null)
    {
        if (!string.IsNullOrEmpty(weaponType) && 
            (weaponType.Equals("YAKIN DÖVÜŞ", StringComparison.OrdinalIgnoreCase) ||
             weaponType.Equals("MELEE", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (string.IsNullOrWhiteSpace(skinName)) return false;
        var s = skinName.ToLowerInvariant();
        return s.Contains("bıçak") || s.Contains("bicak") || s.Contains("melee") || s.Contains("karambit") ||
               s.Contains("hançer") || s.Contains("hancer") || s.Contains("kılıç") || s.Contains("kilic") ||
               s.Contains("balta") || s.Contains("yelpaze") || s.Contains("kelebek") || s.Contains("kunai") ||
               s.Contains("katana") || s.Contains("fan") || s.Contains("blade") || s.Contains("sword") ||
               s.Contains("dagger") || s.Contains("axe") || s.Contains("scythe") || s.Contains("tırpan") ||
               s.Contains("hammer") || s.Contains("çekiş") || s.Contains("baton") || s.Contains("misericórdia") ||
               s.Contains("onimaru") || s.Contains("power-fist");
    }

    public static int GetAccurateSkinPrice(string skinName, string tierDevName, bool isMelee)
    {
        if (string.IsNullOrWhiteSpace(skinName)) skinName = "";
        var lower = skinName.ToLowerInvariant();

        // 1. Special Event / Limited Edition Sets (Fixed official Riot prices)
        if (lower.Contains("champions"))
        {
            return isMelee ? 5350 : 2675;
        }
        if (lower.Contains("vct lock//in") || lower.Contains("lock//in") || lower.Contains("misericórdia"))
        {
            return 5440;
        }
        if (lower.Contains("ignite") || lower.Contains("ateş püsküren"))
        {
            return 4710;
        }
        if (lower.Contains("arcane"))
        {
            if (isMelee) return 4350;
            if (lower.Contains("sheriff")) return 2377;
            return 2175;
        }
        if (lower.Contains("spectrum") || lower.Contains("waveform") || lower.Contains("spektrum"))
        {
            return isMelee ? 5350 : 2675;
        }
        if (lower.Contains("kuronami"))
        {
            return isMelee ? 5350 : 2375;
        }
        if (lower.Contains("radiant entertainment") || lower.Contains("power-fist") || lower.Contains("arcade"))
        {
            return isMelee ? 5950 : 2975;
        }
        if (lower.Contains("onimaru"))
        {
            return 5350;
        }

        // 2. Standard Tiers (Official Valorant Price Matrix)
        var tier = tierDevName?.ToLowerInvariant() ?? "";
        return tier switch
        {
            "select" => isMelee ? 1750 : 875,
            "deluxe" => isMelee ? 2550 : 1275,
            "premium" => isMelee ? 3550 : 1775,
            "exclusive" => isMelee ? 4350 : 2175,
            "ultra" => isMelee ? 4950 : 2475,
            _ => isMelee ? 3550 : 1775
        };
    }

    private static string DetectWeaponType(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName))
            return L10n.IsEnglish ? "WEAPON" : "SİLAH";

        var weapons = new[]
        {
            "Vandal", "Phantom", "Operator", "Sheriff", "Spectre",
            "Ghost", "Classic", "Frenzy", "Shorty", "Stinger",
            "Bulldog", "Guardian", "Marshal", "Outlaw", "Ares",
            "Odin", "Bucky", "Judge", "Bıçak", "Karambit", "Hançer",
            "Kılıç", "Balta", "Melee", "Yelpaze", "Kelebek", "Kunai",
            "Katana", "Fan", "Blade", "Sword", "Dagger", "Axe", "Scythe",
            "Tırpan", "Hammer", "Baton", "Misericórdia"
        };

        foreach (var w in weapons)
        {
            if (skinName.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (w is "Bıçak" or "Karambit" or "Hançer" or "Kılıç" or "Balta" or "Melee" or
                         "Yelpaze" or "Kelebek" or "Kunai" or "Katana" or "Fan" or "Blade" or
                         "Sword" or "Dagger" or "Axe" or "Scythe" or "Tırpan" or "Hammer" or "Baton" or "Misericórdia")
                    return L10n.IsEnglish ? "MELEE" : "YAKIN DÖVÜŞ";
                return w.ToUpperInvariant();
            }
        }

        return L10n.IsEnglish ? "WEAPON" : "SİLAH";
    }

    private static string InferTierFromKeywords(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName)) return null;
        var s = skinName.ToLowerInvariant();

        if (s.Contains("ejder") || s.Contains("elderflame") || s.Contains("protokol") || s.Contains("protocol") ||
            s.Contains("evori") || s.Contains("arcade") || s.Contains("ışıklar şehri"))
            return "ultra";

        if (s.Contains("kuronami") || s.Contains("mistik gül") || s.Contains("mystbloom") || s.Contains("champions") ||
            s.Contains("araxys") || s.Contains("delipop") || s.Contains("glitchpop") || s.Contains("spektrum") ||
            s.Contains("spectrum") || s.Contains("chronovoid") || s.Contains("zamanın ötesi") || s.Contains("singularity") ||
            s.Contains("tekillik") || s.Contains("imperium") || s.Contains("kaosun başlangıcı") || s.Contains("prelude to chaos"))
            return "exclusive";

        if (s.Contains("yağmacı") || s.Contains("reaver") || s.Contains("asil") || s.Contains("prime") ||
            s.Contains("iyon") || s.Contains("ion") || s.Contains("oni") || s.Contains("gaia") ||
            s.Contains("büyüpunk") || s.Contains("magepunk") || s.Contains("neptün") || s.Contains("neptune") ||
            s.Contains("sovereign") || s.Contains("hükümdar") || s.Contains("yabancı") || s.Contains("xenohunter") ||
            s.Contains("kriyostaz") || s.Contains("cryostasis"))
            return "premium";

        if (s.Contains("yüksek irtifa") || s.Contains("altitude") || s.Contains("sakura") || s.Contains("çorak") ||
            s.Contains("wasteland") || s.Contains("aristokrat") || s.Contains("aristocrat") || s.Contains("minima") ||
            s.Contains("silvanus"))
            return "deluxe";

        if (s.Contains("çarpma") || s.Contains("smite") || s.Contains("piyade") || s.Contains("infantry") ||
            s.Contains("hülya") || s.Contains("reverie") || s.Contains("lüks") || s.Contains("luxe") ||
            s.Contains("galleria") || s.Contains("dışbükey") || s.Contains("convex") || s.Contains("rush") || s.Contains("hücum"))
            return "select";

        return null;
    }

    public static string GetBundleCoverVideo(Guid bundleGuid, string bundleName)
    {
        if (string.IsNullOrEmpty(bundleName)) return null;

        // In Valorant, only specific event sets (e.g. Champions or Arcane) have an official animated cover/banner
        bool isChampions = bundleName.Contains("Champions", StringComparison.OrdinalIgnoreCase);
        bool isArcane = bundleName.Contains("Arcane", StringComparison.OrdinalIgnoreCase);

        if (!isChampions && !isArcane)
        {
            // Standard sets (RGX, Kuronami, Reaver, Prime, etc.) do NOT have animated covers
            return null;
        }

        try
        {
            var valLive = RiotClientHelper.GetValorantLivePath();
            if (!string.IsNullOrEmpty(valLive))
            {
                var menuDir = Path.Combine(valLive, "ShooterGame", "Content", "Movies", "Menu");
                if (Directory.Exists(menuDir))
                {
                    var files = Directory.GetFiles(menuDir, "*Homescreen.mp4");
                    if (files.Length > 0)
                    {
                        var best = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                        if (File.Exists(best)) return best;
                    }
                }
            }
        }
        catch { }

        var defaultPath = @"C:\Riot Games\VALORANT\live\ShooterGame\Content\Movies\Menu\13_06_Homescreen.mp4";
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }

    private static FeaturedBundleOffer ParseBundleElement(JsonElement b)
    {
        try
        {
            string dataAssetIdStr = null;
            if (b.TryGetProperty("DataAssetID", out var daProp) ||
                b.TryGetProperty("dataAssetId", out daProp) ||
                b.TryGetProperty("DataAssetId", out daProp))
            {
                dataAssetIdStr = daProp.GetString();
            }

            string idStr = null;
            if (b.TryGetProperty("ID", out var idProp) ||
                b.TryGetProperty("id", out idProp))
            {
                idStr = idProp.GetString();
            }

            // Prioritize DataAssetID because it matches bundles.json and Valorant API CDN
            string chosenIdStr = !string.IsNullOrEmpty(dataAssetIdStr) ? dataAssetIdStr : idStr;
            if (string.IsNullOrEmpty(chosenIdStr) || !Guid.TryParse(chosenIdStr, out var bundleGuid))
                return null;

            var bundleOffer = new FeaturedBundleOffer
            {
                BundleId = bundleGuid,
                DataAssetId = bundleGuid
            };

            if (b.TryGetProperty("DurationRemainingInSeconds", out var dr))
            {
                var sec = dr.GetInt32();
                bundleOffer.RemainingDurationSeconds = sec;
                bundleOffer.RemainingTimeFormatted = FormatCountdown(sec);
            }

            // Costs
            if (b.TryGetProperty("TotalDiscountedCost", out var discCostObj))
            {
                if (discCostObj.TryGetProperty(VpCurrencyUuid, out var vpVal))
                    bundleOffer.VpCost = vpVal.GetInt32();
                else
                {
                    foreach (var prop in discCostObj.EnumerateObject())
                    {
                        bundleOffer.VpCost = prop.Value.GetInt32();
                        break;
                    }
                }
            }

            if (b.TryGetProperty("TotalBaseCost", out var baseCostObj))
            {
                if (baseCostObj.TryGetProperty(VpCurrencyUuid, out var bVp))
                    bundleOffer.BaseVpCost = bVp.GetInt32();
                else
                {
                    foreach (var prop in baseCostObj.EnumerateObject())
                    {
                        bundleOffer.BaseVpCost = prop.Value.GetInt32();
                        break;
                    }
                }
            }

            if (b.TryGetProperty("TotalDiscountPercent", out var tdp))
                bundleOffer.DiscountPercent = (int)Math.Round(tdp.GetDouble());
            else if (bundleOffer.BaseVpCost > bundleOffer.VpCost && bundleOffer.BaseVpCost > 0)
            {
                bundleOffer.DiscountPercent = (int)Math.Round((1.0 - (double)bundleOffer.VpCost / bundleOffer.BaseVpCost) * 100.0);
            }

            // Bundle Items (Skins, Cards, Buddies, Sprays)
            if (b.TryGetProperty("Items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in itemsArray.EnumerateArray())
                {
                    string itemIdStr = null;
                    if (it.TryGetProperty("Item", out var innerItem))
                    {
                        if (innerItem.TryGetProperty("ItemID", out var iid))
                            itemIdStr = iid.GetString();
                    }
                    else if (it.TryGetProperty("ItemID", out var iid2))
                    {
                        itemIdStr = iid2.GetString();
                    }

                    var basePrice = 0;
                    if (it.TryGetProperty("BasePrice", out var bp))
                        basePrice = bp.GetInt32();

                    var discountedPrice = 0;
                    if (it.TryGetProperty("DiscountedPrice", out var dp))
                        discountedPrice = dp.GetInt32();

                    var discountPct = 0;
                    if (it.TryGetProperty("DiscountPercent", out var dpct))
                        discountPct = (int)Math.Round(dpct.GetDouble() * 100);

                    // For weapon skins and items, the standalone purchase price is ALWAYS BasePrice!
                    var standaloneCost = basePrice > 0 ? basePrice : discountedPrice;

                    if (!string.IsNullOrEmpty(itemIdStr) && Guid.TryParse(itemIdStr, out var itemGuid))
                    {
                        var itemOffer = ResolveBundleItemOffer(itemGuid, standaloneCost);
                        if (itemOffer != null && !bundleOffer.Items.Any(x => x.ItemId == itemGuid))
                        {
                            if (discountedPrice > 0 && discountedPrice < itemOffer.VpCost)
                            {
                                itemOffer.BundleDiscountedCost = discountedPrice;
                                itemOffer.DiscountPercentage = discountPct > 0 ? discountPct :
                                    (int)Math.Round((1.0 - (double)discountedPrice / itemOffer.VpCost) * 100);
                            }
                            bundleOffer.Items.Add(itemOffer);
                        }
                    }
                }
            }

            if (b.TryGetProperty("ItemOffers", out var itemOffersArray) && itemOffersArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var io in itemOffersArray.EnumerateArray())
                {
                    var baseCost = 0;
                    var discountedCost = 0;
                    var discountPct = 0;

                    if (io.TryGetProperty("Offer", out var offerProp))
                    {
                        if (offerProp.TryGetProperty("Cost", out var costObj))
                        {
                            if (costObj.TryGetProperty(VpCurrencyUuid, out var vVal))
                                baseCost = vVal.GetInt32();
                            else
                            {
                                foreach (var p in costObj.EnumerateObject()) { baseCost = p.Value.GetInt32(); break; }
                            }
                        }
                    }

                    if (io.TryGetProperty("DiscountedCost", out var ioDiscCostObj))
                    {
                        if (ioDiscCostObj.TryGetProperty(VpCurrencyUuid, out var dvVal))
                            discountedCost = dvVal.GetInt32();
                        else
                        {
                            foreach (var p in ioDiscCostObj.EnumerateObject()) { discountedCost = p.Value.GetInt32(); break; }
                        }
                    }

                    if (io.TryGetProperty("DiscountPercent", out var dpct2))
                        discountPct = (int)Math.Round(dpct2.GetDouble() * 100);

                    var standaloneCost = baseCost > 0 ? baseCost : discountedCost;

                    if (io.TryGetProperty("Offer", out var offerObj) &&
                        offerObj.TryGetProperty("Rewards", out var rewArray) && rewArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var rew in rewArray.EnumerateArray())
                        {
                            if (rew.TryGetProperty("ItemID", out var rewItemId) && Guid.TryParse(rewItemId.GetString(), out var rewGuid))
                            {
                                var existing = bundleOffer.Items.FirstOrDefault(x => x.ItemId == rewGuid);
                                if (existing != null)
                                {
                                    if (existing.VpCost <= 0 && standaloneCost > 0)
                                        existing.VpCost = standaloneCost;

                                    if (discountedCost > 0 && discountedCost < existing.VpCost)
                                    {
                                        existing.BundleDiscountedCost = discountedCost;
                                        existing.DiscountPercentage = discountPct > 0 ? discountPct :
                                            (int)Math.Round((1.0 - (double)discountedCost / existing.VpCost) * 100);
                                    }
                                }
                                else
                                {
                                    var itemOffer = ResolveBundleItemOffer(rewGuid, standaloneCost);
                                    if (itemOffer != null)
                                    {
                                        if (discountedCost > 0 && discountedCost < itemOffer.VpCost)
                                        {
                                            itemOffer.BundleDiscountedCost = discountedCost;
                                            itemOffer.DiscountPercentage = discountPct > 0 ? discountPct :
                                                (int)Math.Round((1.0 - (double)discountedCost / itemOffer.VpCost) * 100);
                                        }
                                        bundleOffer.Items.Add(itemOffer);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Match bundle info from _bundleCache
            ValNameImage bundleInfo = null;
            lock (CacheLock)
            {
                _bundleCache?.TryGetValue(bundleGuid, out bundleInfo);
            }

            if (bundleInfo != null)
            {
                bundleOffer.Name = bundleInfo.Name;
                bundleOffer.DisplayIcon = bundleInfo.Image;
                bundleOffer.DisplayIcon2 = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/displayicon2.png");
                bundleOffer.VerticalPromoImage = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/verticalpromoimage.png");
            }
            else
            {
                var online = FetchBundleDetailsOnline(bundleGuid);
                if (online != null)
                {
                    bundleOffer.Name = online.Name;
                    bundleOffer.DisplayIcon = online.Image;
                    bundleOffer.DisplayIcon2 = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/displayicon2.png");
                    bundleOffer.VerticalPromoImage = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/verticalpromoimage.png");
                }
                else
                {
                    bundleOffer.Name = "ÖNE ÇIKAN KOLEKSİYON";
                    bundleOffer.DisplayIcon = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/displayicon.png");
                    bundleOffer.DisplayIcon2 = new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/displayicon2.png");
                }
            }

            // Set animated cover video ONLY for sets that have an official animated cover in Valorant (e.g. Champions)
            bundleOffer.VideoUrl = GetBundleCoverVideo(bundleGuid, bundleOffer.Name);

            return bundleOffer;
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Error parsing bundle element: {e}", ex.Message);
            return null;
        }
    }

    public static DailyStoreOffer ResolveBundleItemOffer(Guid itemId, int vpCost)
    {
        // 1. Weapon Skin
        var skinOffer = ResolveSkinOffer(itemId, vpCost);
        if (!string.IsNullOrEmpty(skinOffer.Name) && skinOffer.Name != "Bilinmeyen Skin")
        {
            var isMelee = IsMeleeWeapon(skinOffer.Name, skinOffer.WeaponType);
            var accuratePrice = GetAccurateSkinPrice(skinOffer.Name, skinOffer.TierDevName, isMelee);
            if (accuratePrice > 0)
            {
                if (skinOffer.VpCost > 0 && skinOffer.VpCost < accuratePrice)
                {
                    if (skinOffer.BundleDiscountedCost <= 0)
                        skinOffer.BundleDiscountedCost = skinOffer.VpCost;
                    skinOffer.VpCost = accuratePrice;
                    skinOffer.DiscountPercentage = (int)Math.Round((1.0 - (double)skinOffer.BundleDiscountedCost / skinOffer.VpCost) * 100);
                }
                else if (skinOffer.VpCost <= 0)
                {
                    skinOffer.VpCost = accuratePrice;
                }
            }
            return skinOffer;
        }

        // 2. Player Card
        ValNameImage cardInfo = null;
        lock (CacheLock)
        {
            _cardsCache?.TryGetValue(itemId, out cardInfo);
        }
        if (cardInfo != null)
        {
            var cardOffer = new DailyStoreOffer
            {
                ItemId = itemId,
                Name = cardInfo.Name,
                Image = new Uri($"https://media.valorant-api.com/playercards/{itemId}/largeart.png"),
                CardLargeArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/largeart.png"),
                CardWideArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/wideart.png"),
                CardSmallArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/smallart.png"),
                WeaponType = "OYUNCU KARTI",
                TierDevName = "Select",
                TierDisplayName = "OYUNCU KARTI",
                TierColor = "#a855f7",
                VpCost = vpCost > 0 ? vpCost : 375
            };
            ApplyTierDetails(cardOffer);
            cardOffer.TierColor = "#a855f7";
            return cardOffer;
        }

        // 3. Spray
        ValNameImage sprayInfo = null;
        lock (CacheLock)
        {
            _spraysCache?.TryGetValue(itemId, out sprayInfo);
        }
        if (sprayInfo != null)
        {
            var sprayOffer = new DailyStoreOffer
            {
                ItemId = itemId,
                Name = sprayInfo.Name,
                Image = sprayInfo.Image,
                WeaponType = "SPREY",
                TierDevName = "Select",
                TierDisplayName = "SPREY",
                TierColor = "#06b6d4",
                VpCost = vpCost > 0 ? vpCost : 325
            };
            ApplyTierDetails(sprayOffer);
            sprayOffer.TierColor = "#06b6d4";
            return sprayOffer;
        }

        // 4. Gun Buddy
        ValNameImage buddyInfo = null;
        lock (CacheLock)
        {
            _buddiesCache?.TryGetValue(itemId, out buddyInfo);
        }
        if (buddyInfo != null)
        {
            var buddyOffer = new DailyStoreOffer
            {
                ItemId = itemId,
                Name = buddyInfo.Name,
                Image = buddyInfo.Image,
                WeaponType = "SİLAH UĞURU",
                TierDevName = "Select",
                TierDisplayName = "UĞURLUK",
                TierColor = "#f59e0b",
                VpCost = vpCost > 0 ? vpCost : 475
            };
            ApplyTierDetails(buddyOffer);
            buddyOffer.TierColor = "#f59e0b";
            return buddyOffer;
        }

        // 5. Try online accessory fetch
        var onlineAccessory = FetchAccessoryOnline(itemId, vpCost);
        if (onlineAccessory != null) return onlineAccessory;

        return skinOffer;
    }

    private static ValNameImage FetchBundleDetailsOnline(Guid bundleGuid)
    {
        try
        {
            var url = $"https://valorant-api.com/v1/bundles/{bundleGuid}?language={L10n.ValApiLanguage}";
            var resp = HttpClient.GetStringAsync(url).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(resp))
            {
                using var doc = JsonDocument.Parse(resp);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    var name = data.TryGetProperty("displayName", out var dn) ? dn.GetString() : (L10n.IsEnglish ? "Featured Bundle" : "Öne Çıkan Paket");
                    var icon = data.TryGetProperty("displayIcon", out var di) ? di.GetString() : null;
                    var valNameImage = new ValNameImage
                    {
                        Name = name,
                        Image = icon != null ? new Uri(icon) : new Uri($"https://media.valorant-api.com/bundles/{bundleGuid}/displayicon.png")
                    };
                    lock (CacheLock)
                    {
                        _bundleCache ??= new Dictionary<Guid, ValNameImage>();
                        _bundleCache[bundleGuid] = valNameImage;
                    }
                    return valNameImage;
                }
            }
        }
        catch { }
        return null;
    }

    private static DailyStoreOffer FetchAccessoryOnline(Guid itemId, int vpCost)
    {
        try
        {
            // Try playercards
            try
            {
                var resp = HttpClient.GetStringAsync($"https://valorant-api.com/v1/playercards/{itemId}?language={L10n.ValApiLanguage}").GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(resp))
                {
                    using var doc = JsonDocument.Parse(resp);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var name = data.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                        var icon = data.TryGetProperty("largeArt", out var la) ? la.GetString() :
                                   data.TryGetProperty("displayIcon", out var di) ? di.GetString() : null;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(icon))
                        {
                            var off = new DailyStoreOffer
                            {
                                ItemId = itemId,
                                Name = name,
                                Image = new Uri(icon),
                                CardLargeArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/largeart.png"),
                                CardWideArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/wideart.png"),
                                CardSmallArt = new Uri($"https://media.valorant-api.com/playercards/{itemId}/smallart.png"),
                                WeaponType = L10n.IsEnglish ? "PLAYER CARD" : "OYUNCU KARTI",
                                TierDevName = "Select",
                                TierDisplayName = L10n.IsEnglish ? "PLAYER CARD" : "OYUNCU KARTI",
                                TierColor = "#a855f7",
                                VpCost = vpCost > 0 ? vpCost : 375
                            };
                            ApplyTierDetails(off);
                            off.TierColor = "#a855f7";
                            return off;
                        }
                    }
                }
            }
            catch { }

            // Try sprays
            try
            {
                var resp = HttpClient.GetStringAsync($"https://valorant-api.com/v1/sprays/{itemId}?language={L10n.ValApiLanguage}").GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(resp))
                {
                    using var doc = JsonDocument.Parse(resp);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var name = data.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                        var icon = data.TryGetProperty("fullTransparentIcon", out var fti) ? fti.GetString() :
                                   data.TryGetProperty("displayIcon", out var di) ? di.GetString() : null;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(icon))
                        {
                            var off = new DailyStoreOffer
                            {
                                ItemId = itemId,
                                Name = name,
                                Image = new Uri(icon),
                                WeaponType = L10n.IsEnglish ? "SPRAY" : "SPREY",
                                TierDevName = "Select",
                                TierDisplayName = L10n.IsEnglish ? "SPRAY" : "SPREY",
                                TierColor = "#06b6d4",
                                VpCost = vpCost > 0 ? vpCost : 325
                            };
                            ApplyTierDetails(off);
                            off.TierColor = "#06b6d4";
                            return off;
                        }
                    }
                }
            }
            catch { }

            // Try buddies
            try
            {
                var resp = HttpClient.GetStringAsync($"https://valorant-api.com/v1/buddies/{itemId}?language={L10n.ValApiLanguage}").GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(resp))
                {
                    using var doc = JsonDocument.Parse(resp);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var name = data.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                        var icon = data.TryGetProperty("displayIcon", out var di) ? di.GetString() : null;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(icon))
                        {
                            var off = new DailyStoreOffer
                            {
                                ItemId = itemId,
                                Name = name,
                                Image = new Uri(icon),
                                WeaponType = L10n.IsEnglish ? "GUN BUDDY" : "SİLAH UĞURU",
                                TierDevName = "Select",
                                TierDisplayName = L10n.IsEnglish ? "BUDDY" : "UĞURLUK",
                                TierColor = "#f59e0b",
                                VpCost = vpCost > 0 ? vpCost : 475
                            };
                            ApplyTierDetails(off);
                            off.TierColor = "#f59e0b";
                            return off;
                        }
                    }
                }
            }
            catch { }
        }
        catch { }
        return null;
    }

    public static async Task<List<DailyStoreOffer>> GetAllWeaponsCatalogAsync()
    {
        lock (CacheLock)
        {
            if (_allWeaponsCatalogCache != null && _allWeaponsCatalogCache.Count > 0)
                return _allWeaponsCatalogCache;
        }

        await EnsureCachesLoadedAsync().ConfigureAwait(false);

        var list = new List<DailyStoreOffer>();
        try
        {
            var allSkinsPath = Path.Combine(Constants.LocalAppDataPath ?? "", "ValAPI", "allskins.json");
            if (File.Exists(allSkinsPath))
            {
                var json = await File.ReadAllTextAsync(allSkinsPath).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var skinsArray) && skinsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in skinsArray.EnumerateArray())
                    {
                        var displayName = s.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
                        if (string.IsNullOrWhiteSpace(displayName) || displayName.StartsWith("Standart ", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var uuidStr = s.TryGetProperty("uuid", out var u) ? u.GetString() : null;
                        if (!Guid.TryParse(uuidStr, out var skinGuid))
                            continue;

                        var displayIcon = s.TryGetProperty("displayIcon", out var di) ? di.GetString() : null;
                        if (string.IsNullOrEmpty(displayIcon))
                        {
                            if (s.TryGetProperty("levels", out var lvls) && lvls.ValueKind == JsonValueKind.Array && lvls.GetArrayLength() > 0)
                            {
                                var l0 = lvls[0];
                                if (l0.TryGetProperty("displayIcon", out var ldi))
                                    displayIcon = ldi.GetString();
                            }
                        }

                        if (string.IsNullOrEmpty(displayIcon))
                            continue;

                        var offer = ResolveSkinOffer(skinGuid, 0);
                        offer.Name = displayName;
                        offer.Image = new Uri(displayIcon);
                        offer.WeaponType = DetectWeaponType(displayName);

                        if (offer.VpCost <= 0)
                            offer.VpCost = GetAccurateSkinPrice(displayName, offer.TierDevName, IsMeleeWeapon(displayName, offer.WeaponType));

                        ApplyTierDetails(offer);
                        list.Add(offer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("GetAllWeaponsCatalogAsync failed: {e}", ex);
        }

        list = list.OrderBy(x => x.WeaponType).ThenBy(x => x.Name).ToList();

        lock (CacheLock)
        {
            _allWeaponsCatalogCache = list;
        }

        return list;
    }

    public static List<FeaturedBundleOffer> GetDefaultFeaturedBundles()
    {
        var list = new List<FeaturedBundleOffer>();
        try
        {
            // 1. Champions 2026 (Live Riot Bundle: 6640 VP total, 9950 Base)
            var ch26Guid = new Guid("82a19267-4739-d2d0-2a6a-5db2e69e3d89");
            var ch26 = new FeaturedBundleOffer
            {
                BundleId = ch26Guid,
                DataAssetId = ch26Guid,
                Name = "CHAMPIONS 2026",
                DisplayIcon = new Uri("https://media.valorant-api.com/bundles/82a19267-4739-d2d0-2a6a-5db2e69e3d89/displayicon.png"),
                DisplayIcon2 = new Uri("https://media.valorant-api.com/bundles/82a19267-4739-d2d0-2a6a-5db2e69e3d89/displayicon2.png"),
                VerticalPromoImage = new Uri("https://media.valorant-api.com/bundles/82a19267-4739-d2d0-2a6a-5db2e69e3d89/verticalpromoimage.png"),
                Description = "Champions 2026 Koleksiyonu - Sınırlı Sürüm Turnuva Seti",
                ExtraDescription = "Bu sınırlı sürüm paketle Champions 2026'yı kutla ve tuttuğun takıma destek ol. Satış gelirinin bir kısmı VCT takımlarına aktarılır.",
                VpCost = 6640,
                BaseVpCost = 9950,
                DiscountPercent = 33,
                RemainingDurationSeconds = 86400 * 14 + 14400,
                RemainingTimeFormatted = "14 Gün 4 Saat Kaldı",
                VideoUrl = GetBundleCoverVideo(ch26Guid, "CHAMPIONS 2026")
            };

            var chPhantom = ResolveSkinOffer(new Guid("7b17cfbb-4d50-4908-9da5-18afb3a63d8a"), 2675);
            chPhantom.Name = "Champions 2026 Phantom";
            chPhantom.WeaponType = "PHANTOM";
            chPhantom.BundleDiscountedCost = 1766;
            chPhantom.DiscountPercentage = 34;
            ch26.Items.Add(chPhantom);

            var chMelee = ResolveSkinOffer(new Guid("f401b55f-4b7d-c7a5-d698-a5ab0a54df39"), 5350);
            chMelee.Name = L10n.IsEnglish ? "Champions 2026 Blade" : "Champions 2026 Yelpazesi";
            chMelee.WeaponType = L10n.IsEnglish ? "MELEE" : "YAKIN DÖVÜŞ";
            chMelee.BundleDiscountedCost = 3531;
            chMelee.DiscountPercentage = 34;
            ch26.Items.Add(chMelee);

            var chCard = new DailyStoreOffer
            {
                ItemId = new Guid("ba476930-4934-1161-c18e-e88a28aec00d"),
                Name = L10n.IsEnglish ? "Champions 2026 Card" : "Champions 2026 Kartı",
                Image = new Uri("https://media.valorant-api.com/playercards/ba476930-4934-1161-c18e-e88a28aec00d/largeart.png"),
                CardLargeArt = new Uri("https://media.valorant-api.com/playercards/ba476930-4934-1161-c18e-e88a28aec00d/largeart.png"),
                CardWideArt = new Uri("https://media.valorant-api.com/playercards/ba476930-4934-1161-c18e-e88a28aec00d/wideart.png"),
                CardSmallArt = new Uri("https://media.valorant-api.com/playercards/ba476930-4934-1161-c18e-e88a28aec00d/smallart.png"),
                WeaponType = L10n.IsEnglish ? "PLAYER CARD" : "OYUNCU KARTI",
                TierDevName = "Exclusive",
                TierDisplayName = "OYUNCU KARTI",
                TierColor = "#a855f7",
                VpCost = 375,
                BundleDiscountedCost = 263,
                DiscountPercentage = 30
            };
            ApplyTierDetails(chCard);
            chCard.TierColor = "#a855f7";
            ch26.Items.Add(chCard);

            list.Add(ch26);

            // 2. RGX 11z Pro
            var rgxGuid = new Guid("35815cab-429d-79e4-43f5-e0af8fdac22b");
            var rgx = new FeaturedBundleOffer
            {
                BundleId = rgxGuid,
                DataAssetId = rgxGuid,
                Name = "RGX 11Z PRO",
                DisplayIcon = new Uri("https://media.valorant-api.com/bundles/35815cab-429d-79e4-43f5-e0af8fdac22b/displayicon.png"),
                DisplayIcon2 = new Uri("https://media.valorant-api.com/bundles/35815cab-429d-79e4-43f5-e0af8fdac22b/displayicon2.png"),
                VerticalPromoImage = new Uri("https://media.valorant-api.com/bundles/35815cab-429d-79e4-43f5-e0af8fdac22b/verticalpromoimage.png"),
                Description = "RGX 11z Pro Koleksiyonu - Mekanik RGB ve Kill Sayacı",
                ExtraDescription = "Yüksek FPS stili, mekanik şeffaf kaplama ve LED renk geçişleri ile donatılmış özel koleksiyon.",
                VpCost = 8700,
                BaseVpCost = 11900,
                DiscountPercent = 27,
                RemainingDurationSeconds = 86400 * 6 + 7200,
                RemainingTimeFormatted = "6 Gün 2 Saat Kaldı",
            };

            var rgxVandal = ResolveSkinOffer(new Guid("d958b181-4e7b-dc60-7c3c-e3a3a376a8d2"), 2175);
            if (!string.IsNullOrEmpty(rgxVandal.Name))
            {
                rgxVandal.Name = "RGX 11z Pro Vandal";
                rgxVandal.WeaponType = "VANDAL";
                rgx.Items.Add(rgxVandal);
            }

            var rgxCard = new DailyStoreOffer
            {
                ItemId = new Guid("6b954e8f-410f-703a-4ed0-f8ae622e8917"),
                Name = "RGX 11z Pro Kartı",
                Image = new Uri("https://media.valorant-api.com/playercards/6b954e8f-410f-703a-4ed0-f8ae622e8917/largeart.png"),
                CardLargeArt = new Uri("https://media.valorant-api.com/playercards/6b954e8f-410f-703a-4ed0-f8ae622e8917/largeart.png"),
                CardWideArt = new Uri("https://media.valorant-api.com/playercards/6b954e8f-410f-703a-4ed0-f8ae622e8917/wideart.png"),
                CardSmallArt = new Uri("https://media.valorant-api.com/playercards/6b954e8f-410f-703a-4ed0-f8ae622e8917/smallart.png"),
                WeaponType = "OYUNCU KARTI",
                TierDevName = "Exclusive",
                TierDisplayName = "OYUNCU KARTI",
                TierColor = "#a855f7",
                VpCost = 375
            };
            ApplyTierDetails(rgxCard);
            rgxCard.TierColor = "#a855f7";
            rgx.Items.Add(rgxCard);

            list.Add(rgx);
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Error creating default featured bundles: {e}", ex.Message);
        }
        return list;
    }
}

