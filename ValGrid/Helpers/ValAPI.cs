using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using RestSharp;
using ValGrid.Objects;
using Settings = ValGrid.Properties.Settings;

namespace ValGrid.Helpers;

public static class ValApi
{
    private static readonly RestClient Client;
    private static readonly RestClient MediaClient;

    private static Urls _mapsInfo;
    private static Urls _agentsInfo;
    private static Urls _ranksInfo;
    private static Urls _versionInfo;
    private static Urls _skinsInfo;
    private static Urls _cardsInfo;
    private static Urls _spraysInfo;
    private static Urls _gamemodeInfo;
    private static Urls _skinMetaInfo;
    private static Urls _bundlesInfo;
    private static Urls _buddiesInfo;
    private static Urls _flexInfo;
    private static List<Urls> _allInfo;
    private static Dictionary<Guid, ValSkinMeta> _skinMetaCache;
    private static readonly object SkinMetaLock = new();
    private static string _currentLanguage = "tr-TR";

    private static readonly Dictionary<string, string> ValApiLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "ar", "ar-AE" },
            { "de", "de-DE" },
            { "en", "en-US" },
            { "es", "es-ES" },
            { "fr", "fr-FR" },
            { "id", "id-ID" },
            { "it", "it-IT" },
            { "ja", "ja-JP" },
            { "ko", "ko-KR" },
            { "pl", "pl-PL" },
            { "pt", "pt-BR" },
            { "ru", "ru-RU" },
            { "th", "th-TH" },
            { "tr", "tr-TR" },
            { "vi", "vi-VN" },
            { "zh", "zh-CN" }
        };

    static ValApi()
    {
        Client = new RestClient("https://valorant-api.com/v1");
        MediaClient = new RestClient();
    }

    private static async Task<RestResponse<T>> Fetch<T>(string url)
    {
        var request = new RestRequest(url);
        return await Client.ExecuteGetAsync<T>(request).ConfigureAwait(false);
    }

    private static async Task<string> GetValApiVersionAsync()
    {
        var response = await Fetch<VapiVersionResponse>("/version");
        return !response.IsSuccessful ? null : response.Data.Data.BuildDate;
    }

    private static async Task<string> GetLocalValApiVersionAsync()
    {
        if (!File.Exists(Constants.LocalAppDataPath + "\\ValAPI\\version.json"))
            return null;
        try
        {
            var lines = await File.ReadAllLinesAsync(
                    Constants.LocalAppDataPath + "\\ValAPI\\version.json"
                )
                .ConfigureAwait(false);
            return lines[1];
        }
        catch
        {
            return "";
        }
    }

    private static async Task<string> GetLocalValApiLanguageAsync()
    {
        if (!File.Exists(Constants.LocalAppDataPath + "\\ValAPI\\version.json"))
            return null;
        try
        {
            var lines = await File.ReadAllLinesAsync(
                    Constants.LocalAppDataPath + "\\ValAPI\\version.json"
                )
                .ConfigureAwait(false);
            return lines.Length > 2 ? lines[2] : "";
        }
        catch
        {
            return "";
        }
    }

    private static Task GetUrlsAsync()
    {
        var lang = Settings.Default.Language;
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = "tr";
            Settings.Default.Language = "tr";
            Settings.Default.Save();
        }

        _currentLanguage = ValApiLanguages.GetValueOrDefault(lang, "tr-TR");
        var language = _currentLanguage;
        _mapsInfo = new Urls
        {
            Name = "Maps",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\maps.json",
            Url = $"/maps?language={language}"
        };
        _agentsInfo = new Urls
        {
            Name = "Agents",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\agents.json",
            Url = $"/agents?language={language}"
        };
        _skinsInfo = new Urls
        {
            Name = "Skins",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\skinchromas.json",
            Url = $"/weapons/skinchromas?language={language}"
        };
        _cardsInfo = new Urls
        {
            Name = "Cards",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\cards.json",
            Url = $"/playercards?language={language}"
        };
        _spraysInfo = new Urls
        {
            Name = "Sprays",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\sprays.json",
            Url = $"/sprays?language={language}"
        };
        _ranksInfo = new Urls
        {
            Name = "Ranks",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\competitivetiers.json",
            Url = $"/competitivetiers?language={language}"
        };
        _versionInfo = new Urls
        {
            Name = "Version",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\version.json",
            Url = "/version"
        };
        _gamemodeInfo = new Urls
        {
            Name = "Gamemode",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json",
            Url = $"/gamemodes?language={language}"
        };
        _skinMetaInfo = new Urls
        {
            Name = "SkinMeta",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\skinmeta.json",
            Url = $"/weapons/skins?language={language}"
        };
        _bundlesInfo = new Urls
        {
            Name = "Bundles",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\bundles.json",
            Url = $"/bundles?language={language}"
        };
        _buddiesInfo = new Urls
        {
            Name = "Buddies",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\buddies.json",
            Url = $"/buddies?language={language}"
        };
        _flexInfo = new Urls
        {
            Name = "Flex",
            Filepath = Constants.LocalAppDataPath + "\\ValAPI\\flex.json",
            Url = $"/flex?language={language}"
        };
        _allInfo = new List<Urls>
        {
            _mapsInfo,
            _agentsInfo,
            _ranksInfo,
            _versionInfo,
            _skinsInfo,
            _cardsInfo,
            _spraysInfo,
            _gamemodeInfo,
            _skinMetaInfo,
            _bundlesInfo,
            _buddiesInfo,
            _flexInfo
        };
        return Task.CompletedTask;
    }

    public static async Task<ValApiUpdateResult> UpdateFilesAsync(IProgress<string> progress = null)
    {
        var result = new ValApiUpdateResult { Success = false };
        try
        {
            await GetUrlsAsync().ConfigureAwait(false);
            if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI"))
                Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI");

            async Task UpdateVersion()
            {
                var versionRequest = new RestRequest(_versionInfo.Url);
                var versionResponse = await Client
                    .ExecuteGetAsync<VapiVersionResponse>(versionRequest)
                    .ConfigureAwait(false);
                if (!versionResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateVersion Failed, Response:{error}",
                        versionResponse.ErrorException
                    );
                    return;
                }
                string[] lines =
                {
                    versionResponse.Data?.Data.RiotClientVersion,
                    versionResponse.Data?.Data.BuildDate,
                    _currentLanguage
                };
                await File.WriteAllLinesAsync(_versionInfo.Filepath, lines).ConfigureAwait(false);
            }

            async Task UpdateMapsDictionary()
            {
                var mapsResponse = await Fetch<ValApiMapsResponse>(_mapsInfo.Url);
                if (!mapsResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateMapsDictionary Failed, Response:{error}",
                        mapsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<string, ValMap> mapsDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\mapsimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\mapsimg");
                if (mapsResponse.Data?.Data != null)
                    foreach (var map in mapsResponse.Data.Data)
                    {
                        mapsDictionary.TryAdd(
                            map.MapUrl,
                            new ValMap { Name = map.DisplayName, UUID = map.Uuid }
                        );
                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\mapsimg\\{map.Uuid}.png";
                        var request = new RestRequest(map.ListViewIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        if (response != null)
                            await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                    }

                await File.WriteAllTextAsync(
                        _mapsInfo.Filepath,
                        JsonSerializer.Serialize(mapsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateAgentsDictionary()
            {
                var agentsResponse = await Fetch<ValApiAgentsResponse>(_agentsInfo.Url);
                if (!agentsResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateAgentsDictionary Failed, Response:{error}",
                        agentsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, string> agentsDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\agentsimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\agentsimg");
                if (agentsResponse.Data != null)
                    foreach (var agent in agentsResponse.Data.Data)
                    {
                        agentsDictionary.TryAdd(agent.Uuid, agent.DisplayName);

                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\agentsimg\\{agent.Uuid}.png";
                        var request = new RestRequest(agent.DisplayIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        if (response != null)
                            await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                    }

                await File.WriteAllTextAsync(
                        _agentsInfo.Filepath,
                        JsonSerializer.Serialize(agentsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task<int> UpdateSkinsDictionary()
            {
                Dictionary<Guid, ValNameImage> skinsDictionary = new();

                // 1. Fetch /weapons to get default weapon names & icons
                try
                {
                    var weaponsRequest = new RestRequest($"/weapons?language={_currentLanguage}");
                    var weaponsResponse = await Client.ExecuteGetAsync(weaponsRequest).ConfigureAwait(false);
                    if (weaponsResponse.IsSuccessful && !string.IsNullOrEmpty(weaponsResponse.Content))
                    {
                        using var wdoc = JsonDocument.Parse(weaponsResponse.Content);
                        if (wdoc.RootElement.TryGetProperty("data", out var wdata))
                        {
                            foreach (var w in wdata.EnumerateArray())
                            {
                                if (w.TryGetProperty("uuid", out var wu) && wu.TryGetGuid(out var wGuid))
                                {
                                    var wName = w.TryGetProperty("displayName", out var dn) ? dn.GetString() : "Standart";
                                    wName = WeaponHelper.GetTurkishWeaponName(wName);
                                    Uri wIcon = null;
                                    if (w.TryGetProperty("displayIcon", out var di) && !string.IsNullOrEmpty(di.GetString()))
                                        wIcon = new Uri(di.GetString());

                                    if (wIcon != null)
                                    {
                                        skinsDictionary[wGuid] = new ValNameImage
                                        {
                                            Name = "Standart " + wName,
                                            Image = wIcon
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log.Warning("UpdateSkinsDictionary weapons fetch failed: {e}", ex.Message);
                }

                // 2. Fetch /weapons/skins to get ALL skins, levels, and chromas
                try
                {
                    var skinsRequest = new RestRequest($"/weapons/skins?language={_currentLanguage}");
                    var skinsResponse = await Client.ExecuteGetAsync(skinsRequest).ConfigureAwait(false);
                    if (skinsResponse.IsSuccessful && !string.IsNullOrEmpty(skinsResponse.Content))
                    {
                        using var sdoc = JsonDocument.Parse(skinsResponse.Content);
                        if (sdoc.RootElement.TryGetProperty("data", out var sdata))
                        {
                            foreach (var s in sdata.EnumerateArray())
                            {
                                var skinName = s.TryGetProperty("displayName", out var sn) ? sn.GetString() ?? "" : "";
                                skinName = skinName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                Uri skinIcon = null;
                                if (s.TryGetProperty("displayIcon", out var di) && !string.IsNullOrEmpty(di.GetString()))
                                    skinIcon = new Uri(di.GetString());
                                else if (s.TryGetProperty("fullRender", out var fr) && !string.IsNullOrEmpty(fr.GetString()))
                                    skinIcon = new Uri(fr.GetString());

                                if (s.TryGetProperty("uuid", out var su) && su.TryGetGuid(out var skinGuid))
                                {
                                    if (skinIcon != null)
                                    {
                                        skinsDictionary[skinGuid] = new ValNameImage
                                        {
                                            Name = skinName,
                                            Image = skinIcon
                                        };
                                    }
                                }

                                // Index all levels (Level 1, Level 2, Level 3, Level 4)
                                if (s.TryGetProperty("levels", out var levels))
                                {
                                    foreach (var lvl in levels.EnumerateArray())
                                    {
                                        if (lvl.TryGetProperty("uuid", out var lu) && lu.TryGetGuid(out var lvlGuid))
                                        {
                                            var lvlName = lvl.TryGetProperty("displayName", out var ln) ? ln.GetString() ?? skinName : skinName;
                                            lvlName = lvlName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                            Uri lvlIcon = null;
                                            if (lvl.TryGetProperty("displayIcon", out var ldi) && !string.IsNullOrEmpty(ldi.GetString()))
                                                lvlIcon = new Uri(ldi.GetString());
                                            else
                                                lvlIcon = skinIcon;

                                            if (lvlIcon != null)
                                            {
                                                skinsDictionary[lvlGuid] = new ValNameImage
                                                {
                                                    Name = lvlName,
                                                    Image = lvlIcon
                                                };
                                            }
                                        }
                                    }
                                }

                                // Index all chromas
                                if (s.TryGetProperty("chromas", out var chromas))
                                {
                                    foreach (var chr in chromas.EnumerateArray())
                                    {
                                        if (chr.TryGetProperty("uuid", out var cu) && cu.TryGetGuid(out var chrGuid))
                                        {
                                            var chrName = chr.TryGetProperty("displayName", out var cn) ? cn.GetString() ?? skinName : skinName;
                                            chrName = chrName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                            Uri chrIcon = null;
                                            if (chr.TryGetProperty("fullRender", out var cfr) && !string.IsNullOrEmpty(cfr.GetString()))
                                                chrIcon = new Uri(cfr.GetString());
                                            else if (chr.TryGetProperty("displayIcon", out var cdi) && !string.IsNullOrEmpty(cdi.GetString()))
                                                chrIcon = new Uri(cdi.GetString());
                                            else
                                                chrIcon = skinIcon;

                                            if (chrIcon != null)
                                            {
                                                skinsDictionary[chrGuid] = new ValNameImage
                                                {
                                                    Name = chrName,
                                                    Image = chrIcon
                                                };
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("UpdateSkinsDictionary skins fetch failed: {e}", ex.Message);
                }

                // 3. Fallback / ensure /weapons/skinchromas are also merged
                try
                {
                    var chromasResponse = await Fetch<ValApiSkinsResponse>(_skinsInfo.Url);
                    if (chromasResponse?.Data?.Data != null)
                    {
                        foreach (var chr in chromasResponse.Data.Data)
                        {
                            var img = chr.FullRender ?? chr.DisplayIcon;
                            if (img != null && !skinsDictionary.ContainsKey(chr.Uuid))
                            {
                                skinsDictionary[chr.Uuid] = new ValNameImage
                                {
                                    Name = chr.DisplayName,
                                    Image = img
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("UpdateSkinsDictionary skinchromas fetch failed: {e}", ex.Message);
                }

                await File.WriteAllTextAsync(
                        _skinsInfo.Filepath,
                        JsonSerializer.Serialize(skinsDictionary)
                    )
                    .ConfigureAwait(false);

                return skinsDictionary.Count;
            }

            async Task UpdateCardsDictionary()
            {
                var cardsResponse = await Fetch<ValApiCardsResponse>(_cardsInfo.Url);
                if (!cardsResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateCardsDictionary Failed, Response:{error}",
                        cardsResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValCard> cardsDictionary = new();
                if (cardsResponse.Data != null)
                    foreach (var card in cardsResponse.Data.Data)
                        cardsDictionary.TryAdd(
                            card.Uuid,
                            new ValCard
                            {
                                Name = card.DisplayName,
                                Image = card.DisplayIcon,
                                FullImage = card.LargeArt
                            }
                        );
                await File.WriteAllTextAsync(
                        _cardsInfo.Filepath,
                        JsonSerializer.Serialize(cardsDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateSpraysDictionary()
            {
                var spraysResponse = await Fetch<ValApiSpraysResponse>(_spraysInfo.Url);
                if (!spraysResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateSpraysDictionary Failed, Response:{error}",
                        spraysResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, ValNameImage> spraysDictionary = new();
                if (spraysResponse.Data?.Data != null)
                {
                    foreach (var spray in spraysResponse.Data.Data)
                    {
                        var bestIcon = spray.FullTransparentIcon ?? spray.DisplayIcon ?? spray.FullIcon;
                        if (bestIcon == null && spray.Levels != null && spray.Levels.Length > 0)
                        {
                            bestIcon = spray.Levels[0]?.DisplayIcon;
                        }

                        // 1. Add base spray uuid
                        spraysDictionary.TryAdd(
                            spray.Uuid,
                            new ValNameImage
                            {
                                Name = spray.DisplayName,
                                Image = bestIcon
                            }
                        );

                        // 2. Add all level uuids (Riot match loadouts frequently send level UUIDs)
                        if (spray.Levels != null)
                        {
                            foreach (var level in spray.Levels)
                            {
                                if (level == null) continue;
                                var levelIcon = level.DisplayIcon ?? bestIcon;
                                spraysDictionary.TryAdd(
                                    level.Uuid,
                                    new ValNameImage
                                    {
                                        Name = spray.DisplayName,
                                        Image = levelIcon
                                    }
                                );
                            }
                        }
                    }
                }

                // 3. Fetch and add all Flex / Donat items (Valorant expression wheel items)
                try
                {
                    var flexResponse = await Fetch<ValApiFlexResponse>($"/flex?language={_currentLanguage}");
                    if (flexResponse.IsSuccessful && flexResponse.Data?.Data != null)
                    {
                        Dictionary<Guid, ValNameImage> flexDictionary = new();
                        foreach (var flex in flexResponse.Data.Data)
                        {
                            if (flex == null || flex.Uuid == Guid.Empty) continue;
                            var flexEntry = new ValNameImage
                            {
                                Name = flex.DisplayName,
                                Image = flex.DisplayIcon
                            };
                            spraysDictionary[flex.Uuid] = flexEntry;
                            flexDictionary[flex.Uuid] = flexEntry;
                        }

                        if (_flexInfo != null && !string.IsNullOrEmpty(_flexInfo.Filepath))
                        {
                            await File.WriteAllTextAsync(
                                    _flexInfo.Filepath,
                                    JsonSerializer.Serialize(flexDictionary)
                                )
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("Failed to fetch or merge flex items: {e}", ex.Message);
                }

                await File.WriteAllTextAsync(
                        _spraysInfo.Filepath,
                        JsonSerializer.Serialize(spraysDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateRanksDictionary()
            {
                var ranksRequest = new RestRequest(_ranksInfo.Url);
                var ranksResponse = await Client
                    .ExecuteGetAsync<ValApiRanksResponse>(ranksRequest)
                    .ConfigureAwait(false);
                if (!ranksResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateRanksDictionary Failed, Response:{error}",
                        ranksResponse.ErrorException
                    );
                    return;
                }
                Dictionary<int, string> ranksDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\ranksimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\ranksimg");
                if (ranksResponse.Data != null)
                    foreach (var rank in ranksResponse.Data.Data.Last().Tiers)
                    {
                        var tier = rank.TierTier;
                        ranksDictionary.TryAdd(tier, rank.TierName);

                        switch (tier)
                        {
                            case 1
                            or 2:
                                continue;
                            case 0:
                            {
                                try
                                {
                                    const string imagePath = "pack://application:,,,/Assets/0.png";
                                    var imageInfo = Application.GetResourceStream(new Uri(imagePath));
                                    using var ms = new MemoryStream();
                                    if (imageInfo != null)
                                    {
                                        await imageInfo.Stream.CopyToAsync(ms);
                                        var imageBytes = ms.ToArray();
                                        await File.WriteAllBytesAsync(
                                            Constants.LocalAppDataPath + "\\ValAPI\\ranksimg\\0.png",
                                            imageBytes
                                        );
                                    }
                                }
                                catch
                                {
                                    // Application.GetResourceStream may fail in headless/test environments
                                }

                                continue;
                            }
                        }

                        var fileName =
                            Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{tier}.png";

                        var request = new RestRequest(rank.LargeIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);

                        if (response != null)
                            await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                    }

                await File.WriteAllTextAsync(
                        _ranksInfo.Filepath,
                        JsonSerializer.Serialize(ranksDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateGamemodeDictionary()
            {
                var gameModeResponse = await Fetch<ValApiGamemodeResponse>(_gamemodeInfo.Url);
                if (!gameModeResponse.IsSuccessful)
                {
                    Constants.Log?.Error(
                        "updateGamemodeDictionary Failed, Response:{error}",
                        gameModeResponse.ErrorException
                    );
                    return;
                }
                Dictionary<Guid, string> gamemodeDictionary = new();
                if (!Directory.Exists(Constants.LocalAppDataPath + "\\ValAPI\\gamemodeimg"))
                    Directory.CreateDirectory(Constants.LocalAppDataPath + "\\ValAPI\\gamemodeimg");
                if (gameModeResponse.Data != null)
                    foreach (var gamemode in gameModeResponse.Data.Data)
                    {
                        if (gamemode.DisplayIcon == null)
                            continue;
                        gamemodeDictionary.TryAdd(gamemode.Uuid, gamemode.DisplayName);

                        var fileName =
                            Constants.LocalAppDataPath
                            + $"\\ValAPI\\gamemodeimg\\{gamemode.Uuid}.png";
                        var request = new RestRequest(gamemode.DisplayIcon);
                        var response = await MediaClient
                            .DownloadDataAsync(request)
                            .ConfigureAwait(false);
                        if (response != null)
                            await File.WriteAllBytesAsync(fileName, response).ConfigureAwait(false);
                    }

                await File.WriteAllTextAsync(
                        _gamemodeInfo.Filepath,
                        JsonSerializer.Serialize(gamemodeDictionary)
                    )
                    .ConfigureAwait(false);
            }

            async Task UpdateBundlesDictionary()
            {
                try
                {
                    var bundlesRequest = new RestRequest(_bundlesInfo.Url);
                    var bundlesResponse = await Client.ExecuteGetAsync(bundlesRequest).ConfigureAwait(false);
                    if (bundlesResponse.IsSuccessful && !string.IsNullOrEmpty(bundlesResponse.Content))
                    {
                        Dictionary<Guid, ValNameImage> bundlesDictionary = new();
                        using var bdoc = JsonDocument.Parse(bundlesResponse.Content);
                        if (bdoc.RootElement.TryGetProperty("data", out var bdata))
                        {
                            foreach (var b in bdata.EnumerateArray())
                            {
                                if (b.TryGetProperty("uuid", out var bu) && bu.TryGetGuid(out var bGuid))
                                {
                                    var bName = b.TryGetProperty("displayName", out var bn) ? bn.GetString() ?? "" : "";
                                    Uri bIcon = null;
                                    if (b.TryGetProperty("displayIcon", out var di) && !string.IsNullOrEmpty(di.GetString()))
                                        bIcon = new Uri(di.GetString());
                                    else if (b.TryGetProperty("verticalPromoImage", out var vpi) && !string.IsNullOrEmpty(vpi.GetString()))
                                        bIcon = new Uri(vpi.GetString());

                                    if (bIcon != null)
                                    {
                                        bundlesDictionary[bGuid] = new ValNameImage
                                        {
                                            Name = bName,
                                            Image = bIcon
                                        };
                                    }
                                }
                            }
                        }
                        await File.WriteAllTextAsync(_bundlesInfo.Filepath, JsonSerializer.Serialize(bundlesDictionary)).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log.Warning("UpdateBundlesDictionary Failed: {error}", ex.Message);
                }
            }

            async Task UpdateBuddiesDictionary()
            {
                try
                {
                    var response = await Fetch<ValApiBuddiesResponse>(_buddiesInfo.Url).ConfigureAwait(false);
                    if (!response.IsSuccessful || response.Data?.Data == null)
                    {
                        Constants.Log?.Error("UpdateBuddiesDictionary failed, Response: {error}", response.ErrorException);
                        return;
                    }
                    Dictionary<Guid, ValNameImage> buddiesDict = new();
                    foreach (var b in response.Data.Data)
                    {
                        if (b == null) continue;
                        var cleanName = (b.DisplayName ?? "").Replace("\r\n", " ").Replace("\n", " ").Trim();
                        if (b.Uuid != Guid.Empty)
                        {
                            buddiesDict[b.Uuid] = new ValNameImage { Name = cleanName, Image = b.DisplayIcon };
                        }
                        if (b.Levels != null)
                        {
                            foreach (var lvl in b.Levels)
                            {
                                if (lvl != null && lvl.Uuid != Guid.Empty)
                                {
                                    buddiesDict[lvl.Uuid] = new ValNameImage { Name = cleanName, Image = lvl.DisplayIcon ?? b.DisplayIcon };
                                }
                            }
                        }
                    }
                    var jsonOpts = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                    await File.WriteAllTextAsync(_buddiesInfo.Filepath, JsonSerializer.Serialize(buddiesDict, jsonOpts)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("UpdateBuddiesDictionary exception: {error}", ex.Message);
                }
            }

            async Task UpdateSkinMetaDictionary()
            {
                try
                {
                    var tiersRequest = new RestRequest("/contenttiers");
                    var tiersResponse = await Client.ExecuteGetAsync(tiersRequest).ConfigureAwait(false);

                    var skinsRequest = new RestRequest(_skinMetaInfo.Url);
                    var skinsResponse = await Client.ExecuteGetAsync(skinsRequest).ConfigureAwait(false);

                    if (!skinsResponse.IsSuccessful || string.IsNullOrEmpty(skinsResponse.Content))
                        return;

                    Dictionary<string, (string devName, string color)> tierMap = new(StringComparer.OrdinalIgnoreCase);
                    if (tiersResponse?.IsSuccessful == true && !string.IsNullOrEmpty(tiersResponse.Content))
                    {
                        using var tdoc = JsonDocument.Parse(tiersResponse.Content);
                        if (tdoc.RootElement.TryGetProperty("data", out var tdata))
                        {
                            foreach (var t in tdata.EnumerateArray())
                            {
                                var uuid = t.TryGetProperty("uuid", out var u) ? u.GetString() : null;
                                var dev = t.TryGetProperty("devName", out var d) ? d.GetString() : "Standard";
                                var col = t.TryGetProperty("highlightColor", out var c) ? c.GetString() : "";
                                var hex = string.IsNullOrEmpty(col) ? "#7f8c8d" : ("#" + (col.Length >= 6 ? col.Substring(0, 6) : col));
                                if (!string.IsNullOrEmpty(uuid))
                                    tierMap[uuid] = (dev ?? "Standard", hex);
                            }
                        }
                    }

                    var tierPrices = new Dictionary<string, (int weapon, int melee)>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Select", (875, 1750) },
                        { "Deluxe", (1275, 2550) },
                        { "Premium", (1775, 3550) },
                        { "Exclusive", (2175, 4350) },
                        { "Ultra", (2475, 4950) }
                    };
                    var limitedKeywords = new[] { "champions", "arcane", "lock//in", "ignite" };

                    Dictionary<Guid, ValSkinMeta> metaDict = new();
                    using var sdoc = JsonDocument.Parse(skinsResponse.Content);
                    if (sdoc.RootElement.TryGetProperty("data", out var sdata))
                    {
                        foreach (var skin in sdata.EnumerateArray())
                        {
                            var skinName = skin.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "";
                            skinName = skinName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                            var tierUuid = skin.TryGetProperty("contentTierUuid", out var ctu) ? ctu.GetString() ?? "" : "";
                            var assetPath = skin.TryGetProperty("assetPath", out var ap) ? ap.GetString() ?? "" : "";
                            var isMelee = assetPath.Contains("melee", StringComparison.OrdinalIgnoreCase);

                            var tinfo = tierMap.TryGetValue(tierUuid, out var mappedTier) ? mappedTier : ("Standard", "#7f8c8d");
                            var tierDev = tinfo.Item1;
                            var tierColor = tinfo.Item2;
                            var prices = tierPrices.TryGetValue(tierDev, out var pr) ? pr : (0, 0);
                            var vp = isMelee ? prices.Item2 : prices.Item1;

                            var lowerName = skinName.ToLowerInvariant();
                            var isRare = false;
                            var rarityLabel = "";

                            if (limitedKeywords.Any(k => lowerName.Contains(k)))
                            {
                                isRare = true;
                                if (lowerName.Contains("champions"))
                                {
                                    rarityLabel = "CHAMPIONS";
                                    vp = isMelee ? 5350 : 2675;
                                }
                                else if (lowerName.Contains("arcane"))
                                {
                                    rarityLabel = "ARCANE";
                                    vp = isMelee ? 4350 : (lowerName.Contains("sheriff") ? 2377 : 2175);
                                }
                                else if (lowerName.Contains("lock//in"))
                                {
                                    rarityLabel = "VCT LOCK//IN";
                                    vp = 5440;
                                }
                                else if (lowerName.Contains("ignite"))
                                {
                                    rarityLabel = "IGNITE";
                                    vp = 4710;
                                }
                            }
                            else if (lowerName.Contains("spectrum") || lowerName.Contains("waveform"))
                            {
                                isRare = true;
                                rarityLabel = "EXCLUSIVE (ZEDD)";
                                vp = isMelee ? 5350 : 2675;
                            }
                            else if (lowerName.Contains("kuronami"))
                            {
                                isRare = true;
                                rarityLabel = "EXCLUSIVE (KURONAMI)";
                                vp = isMelee ? 5350 : 2375;
                            }
                            else if (lowerName.Contains("radiant entertainment") || lowerName.Contains("power-fist"))
                            {
                                isRare = true;
                                rarityLabel = "ULTRA (RES)";
                                vp = isMelee ? 5950 : 2975;
                            }
                            else if (lowerName.Contains("onimaru"))
                            {
                                isRare = true;
                                rarityLabel = "EXCLUSIVE (ONI 2.0)";
                                vp = 5350;
                            }
                            else if (tierDev.Equals("Ultra", StringComparison.OrdinalIgnoreCase))
                            {
                                isRare = true;
                                rarityLabel = lowerName.Contains("evori") ? "ULTRA (EVORI)" : "ULTRA";
                            }

                            // 1. Index base skin Guid
                            if (skin.TryGetProperty("uuid", out var su) && su.TryGetGuid(out var skinGuid))
                            {
                                metaDict[skinGuid] = new ValSkinMeta
                                {
                                    SkinName = skinName,
                                    ChromaName = skinName,
                                    TierDevName = tierDev,
                                    TierColor = tierColor,
                                    VpCost = vp,
                                    IsRare = isRare,
                                    RarityLabel = rarityLabel,
                                    IsMelee = isMelee
                                };
                            }

                            // 2. Index all level Guids
                            if (skin.TryGetProperty("levels", out var levels))
                            {
                                foreach (var level in levels.EnumerateArray())
                                {
                                    if (level.TryGetProperty("uuid", out var lu) && lu.TryGetGuid(out var levelGuid))
                                    {
                                        var levelName = level.TryGetProperty("displayName", out var ln) ? ln.GetString() ?? skinName : skinName;
                                        levelName = levelName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                        metaDict[levelGuid] = new ValSkinMeta
                                        {
                                            SkinName = skinName,
                                            ChromaName = levelName,
                                            TierDevName = tierDev,
                                            TierColor = tierColor,
                                            VpCost = vp,
                                            IsRare = isRare,
                                            RarityLabel = rarityLabel,
                                            IsMelee = isMelee
                                        };
                                    }
                                }
                            }

                            // 3. Index all chroma Guids
                            if (skin.TryGetProperty("chromas", out var chromas))
                            {
                                foreach (var chroma in chromas.EnumerateArray())
                                {
                                    if (chroma.TryGetProperty("uuid", out var cu) && cu.TryGetGuid(out var chromaGuid))
                                    {
                                        var chromaName = chroma.TryGetProperty("displayName", out var cn) ? cn.GetString() ?? skinName : skinName;
                                        chromaName = chromaName.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                        metaDict[chromaGuid] = new ValSkinMeta
                                        {
                                            SkinName = skinName,
                                            ChromaName = chromaName,
                                            TierDevName = tierDev,
                                            TierColor = tierColor,
                                            VpCost = vp,
                                            IsRare = isRare,
                                            RarityLabel = rarityLabel,
                                            IsMelee = isMelee
                                        };
                                    }
                                }
                            }
                        }
                    }

                    await File.WriteAllTextAsync(_skinMetaInfo.Filepath, JsonSerializer.Serialize(metaDict)).ConfigureAwait(false);
                    lock (SkinMetaLock)
                    {
                        _skinMetaCache = metaDict;
                    }
                }
                catch (Exception ex)
                {
                    Constants.Log?.Error("updateSkinMetaDictionary Failed: {error}", ex);
                }
            }

            async Task SafeRun(Func<Task> action, string name)
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning("{name} failed: {error}", name, ex.Message);
                }
            }

            try
            {
                progress?.Report(L10n.Get("ApiUpdateMapsAgents"));
                await Task.WhenAll(
                    SafeRun(UpdateVersion, "UpdateVersion"),
                    SafeRun(UpdateRanksDictionary, "UpdateRanksDictionary"),
                    SafeRun(UpdateAgentsDictionary, "UpdateAgentsDictionary"),
                    SafeRun(UpdateMapsDictionary, "UpdateMapsDictionary"),
                    SafeRun(UpdateGamemodeDictionary, "UpdateGamemodeDictionary"),
                    SafeRun(UpdateBundlesDictionary, "UpdateBundlesDictionary")
                ).ConfigureAwait(false);

                progress?.Report(L10n.Get("ApiUpdateSkins"));
                result.SkinCount = await UpdateSkinsDictionary().ConfigureAwait(false);

                progress?.Report(L10n.Get("ApiUpdateCardsSprays"));
                await Task.WhenAll(
                    SafeRun(UpdateCardsDictionary, "UpdateCardsDictionary"),
                    SafeRun(UpdateSpraysDictionary, "UpdateSpraysDictionary"),
                    SafeRun(UpdateSkinMetaDictionary, "UpdateSkinMetaDictionary"),
                    SafeRun(UpdateBuddiesDictionary, "UpdateBuddiesDictionary")
                ).ConfigureAwait(false);

                result.Success = true;
                progress?.Report(L10n.FormatDownloadSuccess(result.SkinCount));
            }
            catch (Exception e)
            {
                Constants.Log?.Error(
                    "UpdateFilesAsync parallel tasks failed, Response:{error}",
                    e
                );
                result.ErrorMessage = e.Message;
            }
        }
        catch (Exception e)
        {
            Constants.Log?.Error("UpdateFilesAsync Failed, Response:{error}", e);
            result.ErrorMessage = e.Message;
        }

        return result;
    }

    public static async Task<bool> CheckAndUpdateJsonAsync(IProgress<string> progress = null)
    {
        try
        {
            await GetUrlsAsync().ConfigureAwait(false);

            var remoteVer = await GetValApiVersionAsync().ConfigureAwait(false);
            var localVer = await GetLocalValApiVersionAsync().ConfigureAwait(false);
            var localLang = await GetLocalValApiLanguageAsync().ConfigureAwait(false);
            var filesMissing = _allInfo.Any(url => !File.Exists(url.Filepath));

            var cacheNeedsLangSync = false;
            if (File.Exists(_skinsInfo.Filepath))
            {
                try
                {
                    using var stream = File.OpenRead(_skinsInfo.Filepath);
                    using var reader = new StreamReader(stream);
                    var buffer = new char[4096];
                    var read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    var sample = new string(buffer, 0, read);
                    if (_currentLanguage == "tr-TR" && (sample.Contains("(Variant ") || sample.Contains("Variant 1") || sample.Contains("Standard ")))
                    {
                        cacheNeedsLangSync = true;
                    }
                    else if (_currentLanguage == "en-US" && (sample.Contains("(Varyant ") || sample.Contains("Varyant 1") || sample.Contains("Standart ")))
                    {
                        cacheNeedsLangSync = true;
                    }
                }
                catch { }
            }

            var spraysNeedUpdate = false;
            if (File.Exists(_spraysInfo.Filepath))
            {
                try
                {
                    var fi = new FileInfo(_spraysInfo.Filepath);
                    if (fi.Length < 250000)
                    {
                        spraysNeedUpdate = true;
                    }
                }
                catch { }
            }

            if (remoteVer != localVer || filesMissing || localLang != _currentLanguage || cacheNeedsLangSync || spraysNeedUpdate)
            {
                progress?.Report(L10n.Get("ApiUpdateCheckingLang"));
                var updateRes = await UpdateFilesAsync(progress).ConfigureAwait(false);
                return updateRes.Success;
            }

            return false;
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("CheckAndUpdateJsonAsync Failed: {e}", ex);
            return false;
        }
    }

    public static async Task<Dictionary<Guid, ValSkinMeta>> GetSkinMetaAsync()
    {
        lock (SkinMetaLock)
        {
            if (_skinMetaCache != null && _skinMetaCache.Count > 0)
                return _skinMetaCache;
        }

        var path = Constants.LocalAppDataPath + "\\ValAPI\\skinmeta.json";
        if (File.Exists(path))
        {
            try
            {
                var json = (await File.ReadAllTextAsync(path).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                var dict = JsonSerializer.Deserialize<Dictionary<Guid, ValSkinMeta>>(json);
                if (dict != null && dict.Count > 0)
                {
                    lock (SkinMetaLock)
                    {
                        _skinMetaCache = dict;
                    }
                    return dict;
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("Failed to read skinmeta.json: {e}", ex);
            }
        }

        return new Dictionary<Guid, ValSkinMeta>();
    }
}


