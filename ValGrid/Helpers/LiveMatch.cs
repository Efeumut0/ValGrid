using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using RestSharp;
using RestSharp.Serializers.Json;
using ValGrid.Objects;
using ValGrid.Properties;
using static ValGrid.Helpers.Login;

namespace ValGrid.Helpers;

public class LiveMatch
{
    public delegate void UpdateProgress(int percentage);

    public MatchDetails MatchInfo { get; } = new();
    public static Guid Matchid { get; set; }
    public static Guid Partyid { get; set; }
    public static string Stage { get; set; }
    public string QueueId { get; set; }
    public string Status { get; set; }

    private static readonly object StaticCacheLock = new();
    private static Dictionary<int, string> _cachedRankNames;
    private static Dictionary<Guid, ValNameImage> _cachedSkins;
    private static Dictionary<Guid, ValCard> _cachedCards;
    private static Dictionary<Guid, ValNameImage> _cachedSprays;
    private static Dictionary<Guid, ValNameImage> _cachedBuddies;

    private static async Task<bool> CheckAndSetLiveMatchIdAsync()
    {
        var client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/players/{Constants.Ppuuid}"
        );
        var request = new RestRequest();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync<MatchIDResponse>(request).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            Matchid = response.Data.MatchId;
            Stage = "core";
            return true;
        }

        client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/players/{Constants.Ppuuid}"
        );
        response = await client.ExecuteGetAsync<MatchIDResponse>(request).ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            Matchid = response.Data.MatchId;
            Stage = "pre";
            return true;
        }

        Constants.Log.Error(
            "CheckAndSetLiveMatchIdAsync() failed. Response: {Response}",
            response.ErrorException
        );
        Matchid = Guid.Empty;
        Stage = "";
        return false;
    }

    public async Task<bool> CheckAndSetPartyIdAsync()
    {
        var client = new RestClient(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/players/{Constants.Ppuuid}"
        );
        var request = new RestRequest();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync<PartyIdResponse>(request).ConfigureAwait(false);
        if (!response.IsSuccessful)
            return false;
        Partyid = response.Data.CurrentPartyId;
        return true;
    }

    public static async Task<bool> LiveMatchChecksAsync()
    {
        if (await Checks.CheckLoginAsync().ConfigureAwait(false))
        {
            await LocalRegionAsync().ConfigureAwait(false);
            return await CheckAndSetLiveMatchIdAsync().ConfigureAwait(false);
        }

        if (!await Checks.CheckLocalAsync().ConfigureAwait(false))
            return false;
        await LocalLoginAsync().ConfigureAwait(false);
        await Checks.CheckLoginAsync().ConfigureAwait(false);
        await LocalRegionAsync().ConfigureAwait(false);

        return await CheckAndSetLiveMatchIdAsync().ConfigureAwait(false);
    }

    private static async Task<LiveMatchResponse> GetLiveMatchDetailsAsync()
    {
        RestClient client =
            new(
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/matches/{Matchid}"
            );
        RestRequest request = new();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client
            .ExecuteGetAsync<LiveMatchResponse>(request)
            .ConfigureAwait(false);
        if (response.IsSuccessful)
            return response.Data;
        Constants.Log.Error(
            "GetLiveMatchDetailsAsync() failed. Response: {Response}",
            response.ErrorException
        );
        return null;
    }

    private static async Task<PreMatchResponse> GetPreMatchDetailsAsync()
    {
        RestClient client =
            new(
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/matches/{Matchid}"
            );
        RestRequest request = new();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client
            .ExecuteGetAsync<PreMatchResponse>(request)
            .ConfigureAwait(false);
        if (response.IsSuccessful)
            return response.Data;
        Constants.Log.Error(
            "GetPreMatchDetailsAsync() failed. Response: {Response}",
            response.ErrorException
        );
        return null;
    }

    private static async Task<PartyResponse> GetPartyDetailsAsync()
    {
        RestClient client =
            new(
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/parties/v1/parties/{Partyid}"
            );
        RestRequest request = new();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync<PartyResponse>(request).ConfigureAwait(false);
        if (response.IsSuccessful)
            return response.Data;
        Constants.Log.Error(
            "GetPreMatchDetailsAsync() failed. Response: {Response}",
            response.ErrorException
        );
        return null;
    }

    private async Task<Player> GetPrePlayerInfo(
        RiotPrePlayer riotPlayer,
        sbyte index,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Player player = new();
        player.IsInMatch = true;
        player.Active = Visibility.Visible;
        try
        {
            var cardTask = GetCardAsync(riotPlayer.PlayerIdentity.PlayerCardId, index);
            var historyTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);
            var skinTask = GetPreSkinInfoAsync(riotPlayer.Subject, riotPlayer.CharacterId, index, riotPlayer.PlayerIdentity.PlayerCardId);
            var presenceTask = GetPresenceInfoAsync(riotPlayer.Subject, presencesResponse);

            await Task.WhenAll(cardTask, historyTask, skinTask, presenceTask).ConfigureAwait(false);

            if (riotPlayer.CharacterId != Guid.Empty)
            {
                var agentInfo = LiveMatch.GetAgentInfo(riotPlayer.CharacterId);
                if (!string.IsNullOrEmpty(agentInfo.Name) && agentInfo.Name != "Bilinmeyen")
                {
                    player.IdentityData = new IdentityData { Name = agentInfo.Name, Image = agentInfo.Image };
                }
                else
                {
                    player.IdentityData = cardTask.Result;
                }
            }
            else
            {
                player.IdentityData = cardTask.Result;
            }
            player.RankData = historyTask.Result;
            player.SkinData = skinTask.Result;
            player.PlayerUiData = presenceTask.Result;
            player.IgnData = await GetIgcUsernameAsync(
                    riotPlayer.Subject,
                    riotPlayer.PlayerIdentity.Incognito,
                    false
                )
                .ConfigureAwait(false);
            player.AccountLevel = !riotPlayer.PlayerIdentity.HideAccountLevel
                ? riotPlayer.PlayerIdentity.AccountLevel.ToString()
                : "-";
            player.TeamId = "Blue";
            player.Active = Visibility.Visible;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerInfo() (PRE) failed for player {index}: {e}", index, e);
        }

        return player;
    }

    private async Task<Player> GetLivePlayerInfo(
        RiotLivePlayer riotPlayer,
        sbyte index,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Player player = new();
        player.IsInMatch = true;
        // Karti her zaman goster; bir veri (rank/skin/agent) cekilemese bile
        // oyuncu gizlenmesin (deathmatch'te "12'den 11" sorununu onler).
        player.Active = Visibility.Visible;
        try
        {
            var agentTask = GetAgentInfoAsync(riotPlayer.CharacterId);
            var playerTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);
            var skinTask = GetMatchSkinInfoAsync(riotPlayer.Subject, riotPlayer.CharacterId, index, riotPlayer.PlayerIdentity.PlayerCardId);
            var presenceTask = GetPresenceInfoAsync(riotPlayer.Subject, presencesResponse);

            await Task.WhenAll(agentTask, playerTask, skinTask, presenceTask).ConfigureAwait(false);

            player.IdentityData = agentTask.Result;
            player.RankData = playerTask.Result;
            player.SkinData = skinTask.Result;
            player.PlayerUiData = presenceTask.Result;
            player.IgnData = await GetIgcUsernameAsync(
                    riotPlayer.Subject,
                    riotPlayer.PlayerIdentity.Incognito,
                    false
                )
                .ConfigureAwait(false);
            player.AccountLevel = !riotPlayer.PlayerIdentity.HideAccountLevel
                ? riotPlayer.PlayerIdentity.AccountLevel.ToString()
                : "-";
            player.TeamId = riotPlayer.TeamId;
            player.Active = Visibility.Visible;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerInfo() (LIVE) failed for player {index}: {e}", index, e);
        }

        return player;
    }

    private async Task GetPrePlayers(
        List<Task<Player>> playerTasks,
        PreMatchResponse matchIdInfo,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Task sTask = Task.Run(
            async () => seasonData = await GetSeasonsAsync().ConfigureAwait(false)
        );
        Task pTask = Task.Run(
            async () => presencesResponse = await GetPresencesAsync().ConfigureAwait(false)
        );
        await Task.WhenAll(sTask, pTask).ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in matchIdInfo.AllyTeam.Players)
        {
            playerTasks.Add(GetPrePlayerInfo(riotPlayer, index, seasonData, presencesResponse));
            index++;
        }
    }

    private async Task GetLivePlayers(
        List<Task<Player>> playerTasks,
        LiveMatchResponse matchIdInfo,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        Task sTask = Task.Run(
            async () => seasonData = await GetSeasonsAsync().ConfigureAwait(false)
        );
        Task pTask = Task.Run(
            async () => presencesResponse = await GetPresencesAsync().ConfigureAwait(false)
        );
        await Task.WhenAll(sTask, pTask).ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in matchIdInfo.Players)
        {
            if (riotPlayer.IsCoach)
                continue;

            playerTasks.Add(GetLivePlayerInfo(riotPlayer, index, seasonData, presencesResponse));

            index++;
        }
    }

    private async Task<dynamic> GetMatchResponse()
    {
        if (Stage == "pre")
        {
            return await GetPreMatchDetailsAsync().ConfigureAwait(false);
        }
        return await GetLiveMatchDetailsAsync().ConfigureAwait(false);
    }

    private async Task GetPlayers(
        UpdateProgress updateProgress,
        List<Task<Player>> playerTasks,
        Guid[] seasonData,
        PresencesResponse presencesResponse
    )
    {
        var matchIdInfo = await GetMatchResponse();
        updateProgress(10);

        if (matchIdInfo == null)
            return;

        // Resolve Map immediately
        try
        {
            string rawMapId = "";
            if (matchIdInfo is LiveMatchResponse lmr)
                rawMapId = lmr.MapId ?? "";
            else if (matchIdInfo is PreMatchResponse pmr)
                rawMapId = pmr.MapId ?? "";

            if (string.IsNullOrEmpty(rawMapId) && presencesResponse?.Presences != null)
            {
                var myPresence = presencesResponse.Presences.FirstOrDefault(p => p.Puuid == Constants.Ppuuid && p.Product == "valorant")
                                 ?? presencesResponse.Presences.FirstOrDefault(p => p.Puuid == Constants.Ppuuid);
                if (myPresence != null && !string.IsNullOrEmpty(myPresence.Private))
                {
                    try
                    {
                        var pJson = Encoding.UTF8.GetString(Convert.FromBase64String(myPresence.Private));
                        var pContent = JsonSerializer.Deserialize<PresencesPrivate>(pJson);
                        rawMapId = pContent?.MatchMap ?? pContent?.PartyOwnerMatchMap ?? "";
                    }
                    catch { }
                }
            }

            if (!string.IsNullOrEmpty(rawMapId))
            {
                var resolved = MapHelper.ResolveMapName(rawMapId);
                if (!string.IsNullOrEmpty(resolved) && resolved != "Bilinmeyen Harita")
                {
                    MatchInfo.Map = resolved;
                }
                var mapImg = MapHelper.ResolveMapImage(rawMapId);
                if (mapImg != null)
                    MatchInfo.MapImage = mapImg;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Failed to resolve map in GetPlayers: {e}", ex.Message);
        }

        // Process Server & Connection Details
        try
        {
            if (matchIdInfo is LiveMatchResponse liveMatch)
            {
                var podId = liveMatch.GamePodId ?? "";
                var conn = liveMatch.ConnectionDetails;
                var host = conn?.GameServerHost ?? (conn?.GameServerHosts?.Length > 0 ? conn.GameServerHosts[0] : "");
                var port = conn?.GameServerPort ?? 0;
                var playerKey = conn?.PlayerKey ?? "";

                MatchInfo.ServerRegion = ServerHelper.GetPodLocationName(podId);
                MatchInfo.ServerPodId = podId;
                MatchInfo.ServerHost = host;
                MatchInfo.ServerPort = port;
                MatchInfo.ServerIpPort = port > 0 ? $"{host}:{port}" : (!string.IsNullOrEmpty(host) ? host : "--");
                MatchInfo.PlayerKey = playerKey;
                MatchInfo.PlayerKeyMasked = ServerHelper.MaskPlayerKey(playerKey);
                MatchInfo.PlayerKeyVisibility = !string.IsNullOrEmpty(playerKey) ? Visibility.Visible : Visibility.Collapsed;
                MatchInfo.ServerInfoVisibility = !string.IsNullOrEmpty(podId) || !string.IsNullOrEmpty(host) ? Visibility.Visible : Visibility.Collapsed;
                var initPing = ServerHelper.GetPingForPod(podId);
                if (initPing > 0)
                {
                    MatchInfo.ServerPing = $"{initPing} ms";
                    MatchInfo.ServerPingColor = ServerHelper.GetPingColor(initPing);
                    MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, host, port, initPing, playerKey);
                }
                else
                {
                    MatchInfo.ServerPing = "-- ms";
                    MatchInfo.ServerPingColor = "#8e9bb5";
                    MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, host, port, -1, playerKey);
                }

                _ = Task.Run(async () =>
                {
                    var resolvedPing = initPing;
                    if (resolvedPing <= 0)
                    {
                        resolvedPing = await ServerHelper.FetchRiotPodPingAsync(podId).ConfigureAwait(false);
                    }

                    if (resolvedPing <= 0 && !string.IsNullOrEmpty(host))
                    {
                        resolvedPing = await ServerHelper.MeasurePingAsync(host, port).ConfigureAwait(false);
                    }

                    if (resolvedPing > 0)
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            MatchInfo.ServerPing = $"{resolvedPing} ms";
                            MatchInfo.ServerPingColor = ServerHelper.GetPingColor(resolvedPing);
                            MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, host, port, resolvedPing, playerKey);
                        });
                    }
                });
            }
            else if (matchIdInfo is PreMatchResponse preMatch)
            {
                var podId = preMatch.GamePodId ?? "";
                MatchInfo.ServerRegion = ServerHelper.GetPodLocationName(podId);
                MatchInfo.ServerPodId = podId;
                MatchInfo.ServerIpPort = "Hazırlanıyor...";
                MatchInfo.PlayerKey = "";
                MatchInfo.PlayerKeyMasked = "--";
                MatchInfo.PlayerKeyVisibility = Visibility.Collapsed;
                MatchInfo.ServerInfoVisibility = !string.IsNullOrEmpty(podId) ? Visibility.Visible : Visibility.Collapsed;

                var prePing = ServerHelper.GetPingForPod(podId);
                if (prePing > 0)
                {
                    MatchInfo.ServerPing = $"{prePing} ms";
                    MatchInfo.ServerPingColor = ServerHelper.GetPingColor(prePing);
                    MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, "", 0, prePing, "");
                }
                else
                {
                    MatchInfo.ServerPing = "-- ms";
                    MatchInfo.ServerPingColor = "#8e9bb5";
                    MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, "", 0, -1, "");

                    _ = Task.Run(async () =>
                    {
                        var fetchedPing = await ServerHelper.FetchRiotPodPingAsync(podId).ConfigureAwait(false);
                        if (fetchedPing > 0)
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                MatchInfo.ServerPing = $"{fetchedPing} ms";
                                MatchInfo.ServerPingColor = ServerHelper.GetPingColor(fetchedPing);
                                MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(MatchInfo.ServerRegion, podId, "", 0, fetchedPing, "");
                            });
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Warning("Server connection details extraction failed: {e}", ex.Message);
        }

        if (Stage == "pre")
        {
            await GetPrePlayers(playerTasks, matchIdInfo, seasonData, presencesResponse);
            return;
        }
        await GetLivePlayers(playerTasks, matchIdInfo, seasonData, presencesResponse);
    }

    private void AddPlayerParties(List<Player> playerList)
    {
        var playerPartyColors = new List<string>
        {
            "#32e2b2",       // Mint/Teal
            "#f0b232",       // Gold/Orange
            "#e24a8d",       // Pink/Magenta
            "#38bdf8",       // Sky Blue
            "#a855f7",       // Purple
            "#22c55e",       // Emerald Green
            "#f97316",       // Vivid Orange
            "#ef4444"        // Red
        };

        // Group players by PartyUuid where PartyUuid is valid and not Guid.Empty
        var partyGroups = playerList
            .Where(p => p.PlayerUiData != null && p.PlayerUiData.PartyUuid != Guid.Empty)
            .GroupBy(p => p.PlayerUiData.PartyUuid)
            .Where(g => g.Count() >= 2)
            .ToList();

        var colorIndex = 0;
        foreach (var group in partyGroups)
        {
            var color = playerPartyColors[colorIndex % playerPartyColors.Count];
            colorIndex++;

            var members = group.ToList();
            var partySize = members.Count;
            var partyName = partySize switch
            {
                2 => "Duo (2)",
                3 => "Trio (3)",
                4 => "4'lü Grup",
                5 => "5'li Takım",
                _ => $"{partySize}'li Parti"
            };

            var memberNames = string.Join(", ", members.Select(m =>
                !string.IsNullOrEmpty(m.IgnData?.Username) ? m.IgnData.Username : (!string.IsNullOrEmpty(m.IdentityData?.Name) ? m.IdentityData.Name : "Oyuncu")));

            foreach (var player in members)
            {
                player.PlayerUiData.PartyColour = color;
                player.PlayerUiData.PartyName = partyName;
                player.PlayerUiData.PartyMemberNames = memberNames;
                player.PlayerUiData.PartySize = partySize;
                player.PlayerUiData.IsInParty = true;
                player.PlayerUiData.PartyTooltip = $"🎮 Parti ({partyName}): {memberNames}";
            }
        }

        // Arena mode fallback: If we have >10 players and >2 unique TeamIds,
        // group unpartnered players by TeamId as duos (each TeamId = 1 duo)
        var uniqueTeamIds = playerList
            .Where(p => !string.IsNullOrEmpty(p.TeamId))
            .Select(p => p.TeamId)
            .Distinct()
            .ToList();

        if (playerList.Count > 10 && uniqueTeamIds.Count > 2)
        {
            var teamGroups = playerList
                .Where(p => p.PlayerUiData != null && !p.PlayerUiData.IsInParty && !string.IsNullOrEmpty(p.TeamId))
                .GroupBy(p => p.TeamId)
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var group in teamGroups)
            {
                var color = playerPartyColors[colorIndex % playerPartyColors.Count];
                colorIndex++;

                var members = group.ToList();
                var memberNames = string.Join(", ", members.Select(m =>
                    !string.IsNullOrEmpty(m.IgnData?.Username) ? m.IgnData.Username : (!string.IsNullOrEmpty(m.IdentityData?.Name) ? m.IdentityData.Name : "Oyuncu")));

                foreach (var player in members)
                {
                    player.PlayerUiData.PartyColour = color;
                    player.PlayerUiData.PartyName = "Duo (2)";
                    player.PlayerUiData.PartyMemberNames = memberNames;
                    player.PlayerUiData.PartySize = members.Count;
                    player.PlayerUiData.IsInParty = true;
                    player.PlayerUiData.PartyTooltip = $"🎮 Takım Duo: {memberNames}";
                }
            }
        }

        // Reset solo players
        foreach (var player in playerList)
        {
            if (player.PlayerUiData != null && !player.PlayerUiData.IsInParty)
            {
                player.PlayerUiData.PartyColour = "Transparent";
                player.PlayerUiData.PartyTooltip = "";
                player.PlayerUiData.IsInParty = false;
            }
        }
    }

    public async Task<List<Player>> LiveMatchOutputAsync(UpdateProgress updateProgress)
    {
        _ = Task.Run(CurrencyHelper.RefreshExchangeRateAsync);

        // Fetch user's current party details directly from GLZ party endpoint (100% reliable for user's party)
        HashSet<Guid> localPartyMembers = new();
        try
        {
            if (await CheckAndSetPartyIdAsync().ConfigureAwait(false) && Partyid != Guid.Empty)
            {
                var partyDetails = await GetPartyDetailsAsync().ConfigureAwait(false);
                if (partyDetails?.Members != null)
                {
                    foreach (var member in partyDetails.Members)
                    {
                        if (member.Subject != Guid.Empty)
                            localPartyMembers.Add(member.Subject);
                    }
                    var me = partyDetails.Members.FirstOrDefault(m => m.Subject == Constants.Ppuuid);
                    if (me?.Pings != null)
                    {
                        ServerHelper.CurrentUserPings = me.Pings;
                        if (MatchInfo != null && !string.IsNullOrEmpty(MatchInfo.ServerPodId))
                        {
                            var livePing = ServerHelper.GetPingForPod(MatchInfo.ServerPodId);
                            if (livePing > 0)
                            {
                                Application.Current?.Dispatcher?.Invoke(() =>
                                {
                                    MatchInfo.ServerPing = $"{livePing} ms";
                                    MatchInfo.ServerPingColor = ServerHelper.GetPingColor(livePing);
                                    MatchInfo.ServerTooltip = ServerHelper.FormatServerTooltip(
                                        MatchInfo.ServerRegion, MatchInfo.ServerPodId,
                                        MatchInfo.ServerHost, MatchInfo.ServerPort,
                                        livePing, MatchInfo.PlayerKey
                                    );
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log.Warning("Could not fetch remote party details: {e}", ex.Message);
        }

        var playerList = new List<Player>();
        var playerTasks = new List<Task<Player>>();
        var seasonData = new Guid[4];
        var presencesResponse = new PresencesResponse();

        await GetPlayers(updateProgress, playerTasks, seasonData, presencesResponse);

        playerList.AddRange(await Task.WhenAll(playerTasks).ConfigureAwait(false));
        updateProgress(75);

        // Guarantee user's own party members have matching PartyUuid even if presences failed
        if (Partyid != Guid.Empty && localPartyMembers.Count > 1)
        {
            foreach (var p in playerList)
            {
                if (p.PlayerUiData != null && localPartyMembers.Contains(p.PlayerUiData.Puuid))
                {
                    p.PlayerUiData.PartyUuid = Partyid;
                }
            }
        }

        try
        {
            AddPlayerParties(playerList);
            updateProgress(90);
        }
        catch (Exception e)
        {
            Constants.Log.Error("LiveMatchOutputAsync() party colour failed: {e}", e);
        }

        // Calculate Lobby & Team Inventory Values
        try
        {
            var lobbyVp = playerList.Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
            var blueVp = playerList.Where(p => p.TeamId == "Blue").Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
            var redVp = playerList.Where(p => p.TeamId == "Red").Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);

            if (blueVp == 0 && redVp == 0 && playerList.Count > 1)
            {
                var mid = (int)Math.Ceiling(playerList.Count / 2.0);
                blueVp = playerList.Take(mid).Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
                redVp = playerList.Skip(mid).Sum(p => p.SkinData?.TotalInventoryValueVp ?? 0);
            }

            MatchInfo.LobbyTotalValueVp = lobbyVp;
            MatchInfo.TeamBlueValueVp = blueVp;
            MatchInfo.TeamRedValueVp = redVp;
            updateProgress(100);
        }
        catch (Exception e)
        {
            Constants.Log.Error("LiveMatchOutputAsync() inventory calculation failed: {e}", e);
        }

        foreach (var p in playerList)
        {
            p.IsInMatch = true;
            p.RefreshBlacklistStatus();
        }

        EncounterTracker.Process(playerList, Matchid, Constants.Ppuuid, Stage == "core" || Stage == "pre", MatchInfo?.Map ?? "");
        if (Matchid != Guid.Empty && playerList.Count >= 1)
        {
            MatchHistoryManager.RecordLiveMatch(this, playerList, Constants.Ppuuid);
        }

        return playerList;
    }


    private async Task<Player> GetPartyPlayerInfo(Member riotPlayer, sbyte index, Guid[] seasonData, PresencesResponse presencesResponse)
    {
        Player player = new();
        player.IsInMatch = false;

        PlayerPresenceData playerPres = null;
        bool isPartyOwner = riotPlayer.IsOwner;

        if (presencesResponse?.Presences != null)
        {
            var pItem = presencesResponse.Presences.FirstOrDefault(f => f.Puuid == riotPlayer.Subject && f.Product == "valorant")
                     ?? presencesResponse.Presences.FirstOrDefault(f => f.Puuid == riotPlayer.Subject);
            if (pItem != null && !string.IsNullOrEmpty(pItem.Private))
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(pItem.Private));
                    var content = JsonSerializer.Deserialize<PresencesPrivate>(json);
                    if (content?.PlayerPresenceData != null)
                    {
                        playerPres = content.PlayerPresenceData;
                    }
                    if (content?.PartyPresenceData != null)
                    {
                        isPartyOwner = content.PartyPresenceData.IsPartyOwner || isPartyOwner;
                    }
                }
                catch { }
            }
        }

        var cardId = (playerPres != null && playerPres.PlayerCardId != Guid.Empty)
            ? playerPres.PlayerCardId
            : (riotPlayer.PlayerIdentity != null ? riotPlayer.PlayerIdentity.PlayerCardId : Guid.Empty);

        var cardTask = GetCardAsync(cardId, index);
        var historyTask = GetMatchHistoryAsync(riotPlayer.Subject);
        var playerTask = GetPlayerHistoryAsync(riotPlayer.Subject, seasonData);

        await Task.WhenAll(cardTask, historyTask, playerTask).ConfigureAwait(false);

        player.IdentityData = cardTask.Result ?? new IdentityData { Name = Resources.Player + " " + (index + 1) };
        if (string.IsNullOrEmpty(player.IdentityData.Name))
        {
            player.IdentityData.Name = Resources.Player + " " + (index + 1);
        }

        player.MatchHistoryData = historyTask.Result ?? new MatchHistoryData();
        player.RankData = playerTask.Result ?? new RankData();

        // If pd MMR endpoint failed/returned unrated, use CompetitiveTier from Party/Presence!
        int compTier = (playerPres != null && playerPres.CompetitiveTier > 0)
            ? playerPres.CompetitiveTier
            : (int)riotPlayer.CompetitiveTier;

        if (compTier > 0 && (player.RankData.RankImages == null || player.RankData.RankImages.Length == 0 || player.RankData.RankNames == null || player.RankData.RankNames.Length == 0 || player.RankData.RankNames[0] == "UNRATED"))
        {
            player.RankData.RankImages = new Uri[4];
            player.RankData.RankNames = new string[4];
            Array.Fill(player.RankData.RankImages, new Uri(Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\0.png"));
            Array.Fill(player.RankData.RankNames, "UNRATED");

            player.RankData.RankImages[0] = new Uri(Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{compTier}.png");

            string tierName = "Derecesiz";
            try
            {
                Dictionary<int, string> rankNames;
                lock (StaticCacheLock) { rankNames = _cachedRankNames; }
                if (rankNames == null)
                {
                    var compPath = Constants.LocalAppDataPath + "\\ValAPI\\competitivetiers.json";
                    if (File.Exists(compPath))
                    {
                        var json = (await File.ReadAllTextAsync(compPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            rankNames = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
                            lock (StaticCacheLock) { _cachedRankNames = rankNames; }
                        }
                    }
                }
                if (rankNames != null && rankNames.TryGetValue(compTier, out var name))
                    tierName = name;
            }
            catch { }

            player.RankData.RankNames[0] = tierName;
            player.RankData.PeakRankImage ??= player.RankData.RankImages[0];
            player.RankData.PeakRankName ??= tierName;
            player.RankData.PeakRankTooltip = $"Peak: {tierName}";
        }

        player.PlayerUiData = new PlayerUIData
        {
            BackgroundColour = "#252A40",
            PartyUuid = Partyid,
            PartyColour = "Transparent",
            Puuid = riotPlayer.Subject
        };

        player.IgnData = await GetIgcUsernameAsync(riotPlayer.Subject, false, true).ConfigureAwait(false);

        int accLevel = playerPres?.AccountLevel ?? 0;
        if (accLevel == 0 && riotPlayer.PlayerIdentity != null && !riotPlayer.PlayerIdentity.HideAccountLevel)
        {
            accLevel = (int)riotPlayer.PlayerIdentity.AccountLevel;
        }
        player.AccountLevel = accLevel > 0 ? accLevel.ToString() : (riotPlayer.PlayerIdentity?.HideAccountLevel == true ? "-" : "0");

        player.IsPartyOwner = isPartyOwner;
        player.TeamId = "Blue";
        player.Active = Visibility.Visible;
        return player;
    }

    private async Task GetPartyPlayers(PartyResponse partyInfo, List<Task<Player>> playerTasks)
    {
        if (partyInfo?.Members == null)
            return;

        var seasonData = await GetSeasonsAsync().ConfigureAwait(false);
        var presencesResponse = await GetPresencesAsync().ConfigureAwait(false);
        sbyte index = 0;

        foreach (var riotPlayer in partyInfo.Members)
        {
            playerTasks.Add(GetPartyPlayerInfo(riotPlayer, index, seasonData, presencesResponse));
            index++;
        }
    }

    public async Task<List<Player>> PartyOutputAsync()
    {
        var playerList = new List<Player>();
        var playerTasks = new List<Task<Player>>();
        var partyInfo = await GetPartyDetailsAsync().ConfigureAwait(false);

        await GetPartyPlayers(partyInfo, playerTasks);

        playerList.AddRange(await Task.WhenAll(playerTasks).ConfigureAwait(false));

        foreach (var p in playerList)
        {
            p.IsInMatch = false;
            p.RefreshBlacklistStatus();
        }

        return playerList;
    }

    private static async Task<IgnData> GetIgcUsernameAsync(
        Guid puuid,
        bool isIncognito,
        bool inParty
    )
    {
        IgnData ignData = new();
        ignData.TrackerEnabled = Visibility.Hidden;
        ignData.TrackerDisabled = Visibility.Visible;

        if (isIncognito && !inParty)
        {
            var knownName = EncounterTracker.GetKnownPlayerName(puuid);
            if (string.IsNullOrEmpty(knownName))
            {
                try
                {
                    var fetched = await GetNameServiceGetUsernameAsync(puuid).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(fetched) && fetched.Contains("#") && fetched != "----")
                    {
                        knownName = fetched;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(knownName))
            {
                EncounterTracker.RecordKnownPlayerName(puuid, knownName);
                ignData.IsStreamerMode = true;
                ignData.IsUnmaskedFromMemory = true;
                ignData.Username = $"{knownName} 👁";
                ignData.Tooltip = $"Streamer Modu (Tespit edildi: {knownName})\nSol tık: Tracker profili aç\nSağ tık: Adı kopyala";

                var trackerUri = await TrackerAsync(knownName).ConfigureAwait(false);
                if (trackerUri != null)
                {
                    ignData.TrackerEnabled = Visibility.Visible;
                    ignData.TrackerDisabled = Visibility.Collapsed;
                    ignData.TrackerUri = trackerUri;
                    ignData.Username = $"{knownName} 👁 🔗";
                }
                return ignData;
            }

            ignData.IsStreamerMode = true;
            ignData.Username = "----";
            ignData.Tooltip = "Streamer Modu (Gizli Profil)";
            return ignData;
        }

        ignData.Username = await GetNameServiceGetUsernameAsync(puuid).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(ignData.Username) && ignData.Username.Contains("#") && ignData.Username != "----")
        {
            EncounterTracker.RecordKnownPlayerName(puuid, ignData.Username);
        }
        else if (string.IsNullOrEmpty(ignData.Username) || ignData.Username == "----")
        {
            var fallback = EncounterTracker.GetKnownPlayerName(puuid);
            if (!string.IsNullOrEmpty(fallback))
            {
                ignData.Username = fallback;
            }
        }

        var trackerUriNormal = await TrackerAsync(ignData.Username).ConfigureAwait(false);

        if (trackerUriNormal != null)
        {
            ignData.TrackerEnabled = Visibility.Visible;
            ignData.TrackerDisabled = Visibility.Collapsed;
            ignData.TrackerUri = trackerUriNormal;
            ignData.Username = ignData.Username + " 🔗";
        }

        return ignData;
    }

    private static Dictionary<Guid, string> _agentsCache;
    private static readonly object AgentLock = new();

    public static (string Name, Uri Image) GetAgentInfo(Guid agentid)
    {
        if (agentid == Guid.Empty)
            return ("Bilinmeyen", null);

        string agentName = "Bilinmeyen";
        try
        {
            lock (AgentLock)
            {
                if (_agentsCache == null)
                {
                    var p = (string.IsNullOrEmpty(Constants.LocalAppDataPath)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                        : Constants.LocalAppDataPath) + "\\ValAPI\\agents.json";
                    if (File.Exists(p))
                    {
                        var json = File.ReadAllText(p).Trim().Trim('\uFEFF', '\u200B');
                        if (!string.IsNullOrWhiteSpace(json))
                            _agentsCache = JsonSerializer.Deserialize<Dictionary<Guid, string>>(json);
                    }
                }
                _agentsCache?.TryGetValue(agentid, out agentName);
            }
        }
        catch { }

        Uri imgUri = null;
        try
        {
            var imgPath = (string.IsNullOrEmpty(Constants.LocalAppDataPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid"
                : Constants.LocalAppDataPath) + $"\\ValAPI\\agentsimg\\{agentid}.png";
            if (File.Exists(imgPath))
                imgUri = new Uri(imgPath);
            else
                imgUri = new Uri($"https://media.valorant-api.com/agents/{agentid}/displayicon.png");
        }
        catch { }

        return (agentName ?? "Bilinmeyen", imgUri);
    }

    private static Task<IdentityData> GetAgentInfoAsync(Guid agentid)
    {
        var info = GetAgentInfo(agentid);
        return Task.FromResult(new IdentityData
        {
            Name = info.Name,
            Image = info.Image
        });
    }

    private static async Task<IdentityData> GetCardAsync(Guid cardid, sbyte index)
    {
        if (cardid != Guid.Empty)
        {
            try
            {
                var cardsPath = Constants.LocalAppDataPath + "\\ValAPI\\cards.json";
                if (File.Exists(cardsPath))
                {
                    var json = (await File.ReadAllTextAsync(cardsPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var cards = JsonSerializer.Deserialize<Dictionary<Guid, ValCard>>(json);
                        if (cards != null && cards.TryGetValue(cardid, out var card) && card != null && card.Image != null)
                        {
                            return new IdentityData
                            {
                                Image = card.Image,
                                Name = Resources.Player + " " + (index + 1)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Error("GetCardAsync cards.json read error: {e}", ex);
            }

            return new IdentityData
            {
                Image = new Uri($"https://media.valorant-api.com/playercards/{cardid}/displayicon.png"),
                Name = Resources.Player + " " + (index + 1)
            };
        }

        return new IdentityData
        {
            Name = Resources.Player + " " + (index + 1)
        };
    }

    private static async Task<SkinData> GetMatchSkinInfoAsync(Guid playerPuuid, Guid characterId, sbyte playerno, Guid cardid)
    {
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/matches/{Matchid}/loadouts",
                true
            )
            .ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            try
            {
                var content = JsonSerializer.Deserialize<MatchLoadoutsResponse>(response.Content);
                if (content?.Loadouts != null && content.Loadouts.Length > 0)
                {
                    LoadoutElement matched = null;

                    // 1. Primary: Match by player PUUID (Subject)
                    if (playerPuuid != Guid.Empty)
                    {
                        matched = content.Loadouts.FirstOrDefault(l =>
                            (l.Subject != Guid.Empty && l.Subject == playerPuuid) ||
                            (l.Loadout?.Subject != null && l.Loadout.Subject != Guid.Empty && l.Loadout.Subject == playerPuuid));
                    }

                    // 2. Secondary: Match by Agent (CharacterID)
                    if (matched == null && characterId != Guid.Empty)
                    {
                        matched = content.Loadouts.FirstOrDefault(l => l.CharacterId != Guid.Empty && l.CharacterId == characterId);
                    }

                    // 3. Fallback: Only if single player loadout (e.g. Range/Solo)
                    if (matched == null && content.Loadouts.Length == 1 && (playerno == 0 || playerPuuid == Constants.Ppuuid))
                    {
                        matched = content.Loadouts[0];
                    }

                    var effectiveLoadout = matched?.EffectiveLoadout;
                    if (effectiveLoadout != null)
                    {
                        return await GetSkinInfoAsync(effectiveLoadout, cardid).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log.Error("GetMatchSkinInfoAsync parse failed: {e}", ex);
            }
            return await CreateFallbackSkinDataAsync(cardid).ConfigureAwait(false);
        }

        Constants.Log.Error("GetMatchSkinInfoAsync Failed: {e}", response.ErrorException);
        return await CreateFallbackSkinDataAsync(cardid).ConfigureAwait(false);
    }

    private static async Task<SkinData> CreateFallbackSkinDataAsync(Guid cardid)
    {
        try
        {
            return await GetSkinInfoAsync(new LoadoutLoadout(), cardid).ConfigureAwait(false);
        }
        catch
        {
            var sd = new SkinData();
            sd.ApplySlots();
            return sd;
        }
    }

    private static async Task<SkinData> GetPreSkinInfoAsync(Guid playerPuuid, Guid characterId, sbyte playerno, Guid cardid)
    {
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/matches/{Matchid}/loadouts",
                true
            )
            .ConfigureAwait(false);
        if (response.IsSuccessful)
        {
            try
            {
                var content = JsonSerializer.Deserialize<PreMatchLoadoutsResponse>(
                    response.Content
                );
                if (content?.Loadouts != null && content.Loadouts.Length > 0)
                {
                    LoadoutElement matched = null;

                    if (playerPuuid != Guid.Empty)
                    {
                        matched = content.Loadouts.FirstOrDefault(l =>
                            (l.Subject != Guid.Empty && l.Subject == playerPuuid) ||
                            (l.Loadout?.Subject != null && l.Loadout.Subject != Guid.Empty && l.Loadout.Subject == playerPuuid));
                    }

                    if (matched == null && characterId != Guid.Empty)
                    {
                        matched = content.Loadouts.FirstOrDefault(l => l.CharacterId != Guid.Empty && l.CharacterId == characterId);
                    }

                    // 3. Fallback: Only if single player loadout (e.g. Range/Solo)
                    if (matched == null && content.Loadouts.Length == 1 && (playerno == 0 || playerPuuid == Constants.Ppuuid))
                    {
                        matched = content.Loadouts[0];
                    }

                    var effectiveLoadout = matched?.EffectiveLoadout;
                    if (effectiveLoadout != null)
                    {
                        return await GetSkinInfoAsync(effectiveLoadout, cardid).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log.Error("GetPreSkinInfoAsync parse failed: {e}", ex);
            }
            return await CreateFallbackSkinDataAsync(cardid).ConfigureAwait(false);
        }

        Constants.Log.Error("GetPreSkinInfoAsync Failed: {e}", response.ErrorException);
        return await CreateFallbackSkinDataAsync(cardid).ConfigureAwait(false);
    }

    private static async Task<SkinData> GetSkinInfoAsync(LoadoutLoadout loadout, Guid cardid)
    {
        Dictionary<Guid, ValCard> cards = null;
        Dictionary<Guid, ValNameImage> sprays = null;
        Dictionary<Guid, ValNameImage> skins = null;
        Dictionary<Guid, ValNameImage> buddies = null;

        lock (StaticCacheLock)
        {
            skins = _cachedSkins;
            cards = _cachedCards;
            sprays = _cachedSprays;
            buddies = _cachedBuddies;
        }

        if (skins == null || cards == null || sprays == null)
        {
            try
            {
                var basePath = Constants.LocalAppDataPath + "\\ValAPI";
                var chromasPath = basePath + "\\skinchromas.json";
                var cardsPath = basePath + "\\cards.json";
                var spraysPath = basePath + "\\sprays.json";
                var buddiesPath = basePath + "\\buddies.json";
                var flexPath = basePath + "\\flex.json";

                if (skins == null && File.Exists(chromasPath))
                {
                    var json = (await File.ReadAllTextAsync(chromasPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                        skins = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
                }

                if (cards == null && File.Exists(cardsPath))
                {
                    var json = (await File.ReadAllTextAsync(cardsPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                        cards = JsonSerializer.Deserialize<Dictionary<Guid, ValCard>>(json);
                }

                if (sprays == null && File.Exists(spraysPath))
                {
                    var json = (await File.ReadAllTextAsync(spraysPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                        sprays = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
                }

                if (buddies == null && File.Exists(buddiesPath))
                {
                    var json = (await File.ReadAllTextAsync(buddiesPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                        buddies = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
                }

                if (File.Exists(flexPath))
                {
                    try
                    {
                        var json = (await File.ReadAllTextAsync(flexPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var flexDict = JsonSerializer.Deserialize<Dictionary<Guid, ValNameImage>>(json);
                            if (flexDict != null)
                            {
                                sprays ??= new Dictionary<Guid, ValNameImage>();
                                foreach (var kvp in flexDict)
                                {
                                    sprays[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                    }
                    catch { }
                }

                lock (StaticCacheLock)
                {
                    _cachedSkins = skins;
                    _cachedCards = cards;
                    _cachedSprays = sprays;
                    _cachedBuddies = buddies;
                }
            }
            catch (Exception e)
            {
                Constants.Log.Error("GetSkinInfoAsync failed: {e}", e);
            }
        }

        ValNameImage defNI = new();
        ValNameImage defEmptySpray = new() { Name = "Kuşanılmadı", Image = null };
        ValCard defCard = new();
        ValCard card = SafeDict.GetValue(cards, cardid, defCard);

        ValNameImage GetSpraySafe(int index)
        {
            if (loadout?.Sprays?.SpraySelections == null || sprays == null)
                return defEmptySpray;

            var selections = loadout.Sprays.SpraySelections;
            if (index < 0 || index >= selections.Length)
                return defEmptySpray;

            var sel = selections[index];
            if (sel == null)
                return defEmptySpray;

            Guid targetId = sel.ResolvedId;
            if (targetId != Guid.Empty && sprays.TryGetValue(targetId, out var found) && found != null && found.Image != null)
                return found;

            if (sel.SprayId.HasValue && sel.SprayId.Value != Guid.Empty && sprays.TryGetValue(sel.SprayId.Value, out var sSpray) && sSpray?.Image != null)
                return sSpray;

            if (sel.AssetId.HasValue && sel.AssetId.Value != Guid.Empty && sprays.TryGetValue(sel.AssetId.Value, out var sAsset) && sAsset?.Image != null)
                return sAsset;

            if (sel.LevelId.HasValue && sel.LevelId.Value != Guid.Empty && sprays.TryGetValue(sel.LevelId.Value, out var sLevel) && sLevel?.Image != null)
                return sLevel;

            return defEmptySpray;
        }

        var s1 = GetSpraySafe(0);
        var s2 = GetSpraySafe(1);
        var s3 = GetSpraySafe(2);
        var s4 = GetSpraySafe(3);

        var skinData = new SkinData
        {
            CardImage = card.Image,
            LargeCardImage = card.FullImage,
            CardName = card.Name,
            Spray1Image = s1.Image,
            Spray1Name = s1.Name,
            Spray2Image = s2.Image,
            Spray2Name = s2.Name,
            Spray3Image = s3.Image,
            Spray3Name = s3.Name,
            Spray4Image = s4.Image,
            Spray4Name = s4.Name
        };

        (Uri Image, string Name) GetSkinSafe(string itemUuid)
        {
            if (loadout?.Items != null && loadout.Items.TryGetValue(itemUuid, out var item))
            {
                // 1. Try Chroma socket (3ad1b2b2-acdb-4524-852f-954a76ddae0a)
                if (item.Sockets != null &&
                    item.Sockets.TryGetValue("3ad1b2b2-acdb-4524-852f-954a76ddae0a", out var cs) &&
                    cs?.Item != null &&
                    skins != null && skins.TryGetValue(cs.Item.Id, out var csSkin) &&
                    csSkin?.Image != null)
                {
                    return (csSkin.Image, csSkin.Name ?? "");
                }

                // 2. Try Skin Level socket (e7c63390-eda7-46e0-bb7a-a6abdacd2433)
                if (item.Sockets != null &&
                    (item.Sockets.TryGetValue("e7c63390-eda7-46e0-bb7a-a6abdacd2433", out var ls) ||
                     item.Sockets.TryGetValue("e7c634d7-453c-4f28-89b2-7c79403e5633", out ls)) &&
                    ls?.Item != null &&
                    skins != null && skins.TryGetValue(ls.Item.Id, out var lsSkin) &&
                    lsSkin?.Image != null)
                {
                    return (lsSkin.Image, lsSkin.Name ?? "");
                }

                // 3. Try any other socket in item.Sockets (excluding Gun Buddy sockets and Buddy IDs)
                if (item.Sockets != null && skins != null)
                {
                    foreach (var (sockKey, sock) in item.Sockets)
                    {
                        if (sockKey.Equals("bcef87d6-4131-4696-847a-7c907b8b209e", StringComparison.OrdinalIgnoreCase) ||
                            sockKey.Equals("7be270f8-4528-a080-6901-209228d447d9", StringComparison.OrdinalIgnoreCase) ||
                            sockKey.Equals("b8f7fed9-4084-7a6c-e54e-0d8591ef38a0", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (sock?.Item != null && buddies != null && buddies.ContainsKey(sock.Item.Id))
                        {
                            continue;
                        }

                        if (sock?.Item != null && skins.TryGetValue(sock.Item.Id, out var sSkin) && sSkin?.Image != null)
                        {
                            return (sSkin.Image, sSkin.Name ?? "");
                        }
                    }
                }

                // 4. Try item.Id itself
                if (skins != null && skins.TryGetValue(item.Id, out var baseSkin) && baseSkin?.Image != null)
                {
                    return (baseSkin.Image, baseSkin.Name ?? "");
                }
            }

            // Fallback: If skin not matched but weapon UUID exists in skins (e.g. Standard weapon)
            if (Guid.TryParse(itemUuid, out var parsedWeaponGuid) && skins != null && skins.TryGetValue(parsedWeaponGuid, out var defSkin) && defSkin?.Image != null)
            {
                return (defSkin.Image, defSkin.Name ?? "");
            }

            return (defNI.Image, "");
        }

        WeaponBuddyInfo GetBuddySafe(string itemUuid)
        {
            if (loadout?.Items != null && loadout.Items.TryGetValue(itemUuid, out var item) && item.Sockets != null && buddies != null)
            {
                foreach (var sock in item.Sockets.Values)
                {
                    if (sock?.Item != null && buddies.TryGetValue(sock.Item.Id, out var b) && (!string.IsNullOrWhiteSpace(b?.Name) || b?.Image != null))
                    {
                        return new WeaponBuddyInfo { Name = b?.Name ?? "", Image = b?.Image };
                    }
                }
            }
            return null;
        }

        var classic = GetSkinSafe("29a0cfab-485b-f5d5-779a-b59f85e204a8");
        var shorty = GetSkinSafe("42da8ccc-40d5-affc-beec-15aa47b42eda");
        var frenzy = GetSkinSafe("44d4e95c-4157-0037-81b2-17841bf2e8e3");
        var ghost = GetSkinSafe("1baa85b4-4c70-1284-64bb-6481dfc3bb4e");
        var bandit = GetSkinSafe("410b2e0b-4ceb-1321-1727-20858f7f3477");
        var sheriff = GetSkinSafe("e336c6b8-418d-9340-d77f-7a9e4cfe0702");
        var stinger = GetSkinSafe("f7e1b454-4ad4-1063-ec0a-159e56b58941");
        var spectre = GetSkinSafe("462080d1-4035-2937-7c09-27aa2a5c27a7");
        var bucky = GetSkinSafe("910be174-449b-c412-ab22-d0873436b21b");
        var judge = GetSkinSafe("ec845bf4-4f79-ddda-a3da-0db3774b2794");
        var bulldog = GetSkinSafe("ae3de142-4d85-2547-dd26-4e90bed35cf7");
        var guardian = GetSkinSafe("4ade7faa-4cf1-8376-95ef-39884480959b");
        var warden = GetSkinSafe("8db0a1bf-4a50-832a-4566-faaaa6d250ca");
        var phantom = GetSkinSafe("ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a");
        var vandal = GetSkinSafe("9c82e19d-4575-0200-1a81-3eacf00cf872");
        var marshal = GetSkinSafe("c4883e50-4494-202c-3ec3-6b8a9284f00b");
        var outlaw = GetSkinSafe("5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c");
        var op = GetSkinSafe("a03b24d3-4319-996d-0f8c-94bbfba1dfc7");
        var ares = GetSkinSafe("55d8a0f4-4274-ca67-fe2c-06ab45efdf58");
        var odin = GetSkinSafe("63e6c2b6-4a8e-869c-3d4c-e38355226584");
        var melee = GetSkinSafe("2f59173c-4bed-b6c3-2191-dea9b58be9c7");

        skinData.ClassicImage = classic.Image;
        skinData.ClassicName = classic.Name;
        skinData.ShortyImage = shorty.Image;
        skinData.ShortyName = shorty.Name;
        skinData.FrenzyImage = frenzy.Image;
        skinData.FrenzyName = frenzy.Name;
        skinData.GhostImage = ghost.Image;
        skinData.GhostName = ghost.Name;
        skinData.BanditImage = bandit.Image;
        skinData.BanditName = bandit.Name;
        skinData.SheriffImage = sheriff.Image;
        skinData.SheriffName = sheriff.Name;
        skinData.StingerImage = stinger.Image;
        skinData.StingerName = stinger.Name;
        skinData.SpectreImage = spectre.Image;
        skinData.SpectreName = spectre.Name;
        skinData.BuckyImage = bucky.Image;
        skinData.BuckyName = bucky.Name;
        skinData.JudgeImage = judge.Image;
        skinData.JudgeName = judge.Name;
        skinData.BulldogImage = bulldog.Image;
        skinData.BulldogName = bulldog.Name;
        skinData.GuardianImage = guardian.Image;
        skinData.GuardianName = guardian.Name;
        skinData.WardenImage = warden.Image;
        skinData.WardenName = warden.Name;
        skinData.PhantomImage = phantom.Image;
        skinData.PhantomName = phantom.Name;
        skinData.VandalImage = vandal.Image;
        skinData.VandalName = vandal.Name;
        skinData.MarshalImage = marshal.Image;
        skinData.MarshalName = marshal.Name;
        skinData.OutlawImage = outlaw.Image;
        skinData.OutlawName = outlaw.Name;
        skinData.OperatorImage = op.Image;
        skinData.OperatorName = op.Name;
        skinData.AresImage = ares.Image;
        skinData.AresName = ares.Name;
        skinData.OdinImage = odin.Image;
        skinData.OdinName = odin.Name;
        skinData.MeleeImage = melee.Image;
        skinData.MeleeName = melee.Name;

        if (skinData != null)
        {
            try
            {
                var skinMetas = await ValApi.GetSkinMetaAsync().ConfigureAwait(false);
                var totalVp = 0;
                var hasUltraRare = false;
                var hasLimited = false;
                var hasUltra = false;
                var rareCount = 0;

                var rareSkins = new List<(string Weapon, ValSkinMeta Meta)>();

                void ProcessWeapon(string weaponName, string weaponUuid)
                {
                    if (loadout?.Items != null && loadout.Items.TryGetValue(weaponUuid, out var item))
                    {
                        ValSkinMeta meta = null;

                        // 1. Try chroma socket
                        if (item.Sockets != null &&
                            item.Sockets.TryGetValue("3ad1b2b2-acdb-4524-852f-954a76ddae0a", out var cs) &&
                            cs?.Item != null &&
                            skinMetas != null && skinMetas.TryGetValue(cs.Item.Id, out var cm))
                        {
                            meta = cm;
                        }

                        // 2. Try level socket (e7c63390-eda7-46e0-bb7a-a6abdacd2433)
                        if (meta == null && item.Sockets != null &&
                            (item.Sockets.TryGetValue("e7c63390-eda7-46e0-bb7a-a6abdacd2433", out var ls) ||
                             item.Sockets.TryGetValue("e7c634d7-453c-4f28-89b2-7c79403e5633", out ls)) &&
                            ls?.Item != null &&
                            skinMetas != null && skinMetas.TryGetValue(ls.Item.Id, out var lm))
                        {
                            meta = lm;
                        }

                        // 3. Try any socket (excluding Gun Buddy sockets and Buddy IDs)
                        if (meta == null && item.Sockets != null && skinMetas != null)
                        {
                            foreach (var (sockKey, sock) in item.Sockets)
                            {
                                if (sockKey.Equals("bcef87d6-4131-4696-847a-7c907b8b209e", StringComparison.OrdinalIgnoreCase) ||
                                    sockKey.Equals("7be270f8-4528-a080-6901-209228d447d9", StringComparison.OrdinalIgnoreCase) ||
                                    sockKey.Equals("b8f7fed9-4084-7a6c-e54e-0d8591ef38a0", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (sock?.Item != null && buddies != null && buddies.ContainsKey(sock.Item.Id))
                                {
                                    continue;
                                }

                                if (sock?.Item != null && skinMetas.TryGetValue(sock.Item.Id, out var sm))
                                {
                                    meta = sm;
                                    break;
                                }
                            }
                        }

                        // 4. Try item.Id
                        if (meta == null && skinMetas != null && skinMetas.TryGetValue(item.Id, out var bm))
                        {
                            meta = bm;
                        }

                        if (meta != null)
                        {
                            skinData.WeaponMetas[weaponName] = meta;
                            totalVp += meta.VpCost;
                            if (meta.IsRare)
                            {
                                hasUltraRare = true;
                                rareCount++;
                                rareSkins.Add((weaponName, meta));
                                var rLabel = meta.RarityLabel ?? "";
                                if (rLabel.Contains("CHAMPIONS") || rLabel.Contains("ARCANE") ||
                                    rLabel.Contains("LOCK") || rLabel.Contains("IGNITE"))
                                {
                                    hasLimited = true;
                                }
                                else
                                {
                                    hasUltra = true;
                                }
                            }
                        }
                    }

                    var buddy = GetBuddySafe(weaponUuid);
                    if (buddy != null)
                    {
                        skinData.WeaponBuddies[weaponName] = buddy;
                    }
                }

                ProcessWeapon("Classic", "29a0cfab-485b-f5d5-779a-b59f85e204a8");
                ProcessWeapon("Shorty", "42da8ccc-40d5-affc-beec-15aa47b42eda");
                ProcessWeapon("Frenzy", "44d4e95c-4157-0037-81b2-17841bf2e8e3");
                ProcessWeapon("Ghost", "1baa85b4-4c70-1284-64bb-6481dfc3bb4e");
                ProcessWeapon("Bandit", "410b2e0b-4ceb-1321-1727-20858f7f3477");
                ProcessWeapon("Sheriff", "e336c6b8-418d-9340-d77f-7a9e4cfe0702");
                ProcessWeapon("Stinger", "f7e1b454-4ad4-1063-ec0a-159e56b58941");
                ProcessWeapon("Spectre", "462080d1-4035-2937-7c09-27aa2a5c27a7");
                ProcessWeapon("Bucky", "910be174-449b-c412-ab22-d0873436b21b");
                ProcessWeapon("Judge", "ec845bf4-4f79-ddda-a3da-0db3774b2794");
                ProcessWeapon("Bulldog", "ae3de142-4d85-2547-dd26-4e90bed35cf7");
                ProcessWeapon("Guardian", "4ade7faa-4cf1-8376-95ef-39884480959b");
                ProcessWeapon("Warden", "8db0a1bf-4a50-832a-4566-faaaa6d250ca");
                ProcessWeapon("Phantom", "ee8e8d15-496b-07ac-e5f6-8fae5d4c7b1a");
                ProcessWeapon("Vandal", "9c82e19d-4575-0200-1a81-3eacf00cf872");
                ProcessWeapon("Marshal", "c4883e50-4494-202c-3ec3-6b8a9284f00b");
                ProcessWeapon("Outlaw", "5f0aaf7a-4289-3998-d5ff-eb9a5cf7ef5c");
                ProcessWeapon("Operator", "a03b24d3-4319-996d-0f8c-94bbfba1dfc7");
                ProcessWeapon("Ares", "55d8a0f4-4274-ca67-fe2c-06ab45efdf58");
                ProcessWeapon("Odin", "63e6c2b6-4a8e-869c-3d4c-e38355226584");
                ProcessWeapon("Melee", "2f59173c-4bed-b6c3-2191-dea9b58be9c7");

                string collectionType;
                string badgeIcon;
                if (hasLimited && hasUltra)
                {
                    collectionType = "Limited & Ultra Koleksiyon";
                    badgeIcon = "⭐";
                }
                else if (hasLimited)
                {
                    collectionType = "Limited Koleksiyon";
                    badgeIcon = "⭐";
                }
                else if (hasUltra)
                {
                    collectionType = "Ultra Koleksiyon";
                    badgeIcon = "⭐";
                }
                else
                {
                    collectionType = "Koleksiyon";
                    badgeIcon = "💎";
                }

                skinData.TotalInventoryValueVp = totalVp;
                skinData.HasUltraRareSkin = hasUltraRare;
                skinData.RareBadgeIcon = badgeIcon;
                skinData.CollectionType = collectionType;
                if (hasUltraRare)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"⭐ [{collectionType}]");
                    sb.AppendLine($"Toplam Değer: {CurrencyHelper.FormatVpAndTl(totalVp)}");
                    sb.AppendLine();
                    sb.AppendLine(rareSkins.Count == 1 ? "⭐ Parlama Sebebi Nadir Skin:" : $"⭐ Parlama Sebebi Nadir Skinler ({rareSkins.Count}):");
                    foreach (var (_, r) in rareSkins)
                    {
                        var costStr = r.VpCost > 0 ? $" ({CurrencyHelper.FormatVpAndTl(r.VpCost)})" : "";
                        var tag = !string.IsNullOrWhiteSpace(r.RarityLabel) ? $"[{r.RarityLabel}] " : "";
                        sb.AppendLine($" • {tag}{r.SkinName}{costStr}");
                    }
                    skinData.InventorySummaryTooltip = sb.ToString().TrimEnd();
                }
                else
                {
                    skinData.InventorySummaryTooltip = $"💎 Toplam Envanter Değeri: {CurrencyHelper.FormatVpAndTl(totalVp)}";
                }

            }
            catch (Exception ex)
            {
                Constants.Log.Warning("Failed to process skin metadata: {err}", ex.Message);
            }

            skinData.ApplySlots();
        }
        else
        {
            Constants.Log.Error("GetSkinInfoAsync failed: skinData is null");
        }


        return skinData;
    }

    public static async Task<MatchHistoryData> GetMatchHistoryAsync(Guid puuid)
    {
        MatchHistoryData history =
            new()
            {
                PreviousGameColours = new string[3] { "#7f7f7f", "#7f7f7f", "#7f7f7f" },
                PreviousGames = new int[3]
            };

        try
        {
            if (puuid == Guid.Empty)
            {
                Constants.Log.Error("GetMatchHistoryAsync: Puuid is null");
                return history;
            }
            var response = await DoCachedRequestAsync(
                    Method.Get,
                    $"https://pd.{Constants.Region}.a.pvp.net/mmr/v1/players/{puuid}/competitiveupdates?queue=competitive",
                    true
                )
                .ConfigureAwait(false);
            if (!response.IsSuccessful)
            {
                Constants.Log.Error(
                    "GetMatchHistoryAsync request failed: {e}",
                    response.ErrorException
                );
                return history;
            }

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            var content = JsonSerializer.Deserialize<CompetitiveUpdatesResponse>(
                response.Content,
                options
            );

            if (content?.Matches == null || content.Matches.Length == 0)
            {
                return history;
            }

            history.RankProgress = content.Matches[0].RankedRatingAfterUpdate;

            for (int i = 0; i < 3; i++)
            {
                if (i >= content.Matches.Length)
                    break;
                var match = content.Matches[i].RankedRatingEarned;
                history.PreviousGames[i] = Math.Abs(match);
                history.PreviousGameColours[i] = match switch
                {
                    > 0 => "#32e2b2",
                    < 0 => "#ff4654",
                    _ => "#7f7f7f"
                };
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetMatchHistoryAsync failed: {e}", e);
        }

        return history;
    }

    private static async Task<RankData> GetPlayerHistoryAsync(Guid puuid, Guid[] seasonData)
    {
        var rankData = new RankData();
        var ranks = new int[4];

        rankData.RankImages = new Uri[ranks.Length];
        rankData.RankNames = new string[ranks.Length];
        Array.Fill(
            rankData.RankImages,
            new Uri(Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\0.png")
        );
        Array.Fill(rankData.RankNames, "UNRATED");
        rankData.PeakRankImage = new Uri(
            Constants.LocalAppDataPath + "\\ValAPI\\ranksimg\\0.png"
        );

        if (puuid == Guid.Empty)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed: PUUID is empty");
            return rankData;
        }
        var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://pd.{Constants.Region}.a.pvp.net/mmr/v1/players/{puuid}",
                true
            )
            .ConfigureAwait(false);

        if (!response.IsSuccessful && response.Content != null)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed: {e}", response.ErrorException);
            return rankData;
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        var content = JsonSerializer.Deserialize<MmrResponse>(response.Content, options);

        if (content?.QueueSkills?.Competitive?.SeasonalInfoBySeasonId?.Act is null)
        {
            return rankData;
        }

        var SeasonInfo = content.QueueSkills.Competitive.SeasonalInfoBySeasonId.Act;

        for (int i = 0; i < ranks.Length; i++)
        {
            if (!SeasonInfo.TryGetValue(seasonData[i].ToString(), out var currentActJsonElement))
                continue;

            var act = currentActJsonElement.Deserialize<ActInfo>();
            var rank = act.CompetitiveTier;

            if (rank is 1 or 2)
                rank = 0;
            if (Constants.BeforeAscendantSeasons.Contains(seasonData[i]))
                rank += 3;

            ranks[i] = rank;
        }

        // Peak rank = highest tier across ALL acts (with the same normalisation).
        var peakTier = 0;
        var peakRr = 0;
        foreach (var kv in SeasonInfo)
        {
            try
            {
                var actInfo = kv.Value.Deserialize<ActInfo>();
                if (actInfo == null)
                    continue;
                var tier = actInfo.CompetitiveTier;
                if (tier is 1 or 2)
                    tier = 0;
                if (
                    Guid.TryParse(kv.Key, out var seasonGuid)
                    && Constants.BeforeAscendantSeasons.Contains(seasonGuid)
                )
                    tier += 3;
                if (tier > peakTier || (tier == peakTier && actInfo.RankedRating > peakRr))
                {
                    peakTier = tier;
                    peakRr = actInfo.RankedRating;
                }
            }
            catch (Exception e)
            {
                Constants.Log.Error("GetPlayerHistoryAsync peak calc error: {e}", e);
            }
        }

        if (ranks[0] >= 24)
        {
            var leaderboardResponse = await DoCachedRequestAsync(
                    Method.Get,
                    $"https://pd.{Constants.Shard}.a.pvp.net/mmr/v1/leaderboards/affinity/{Constants.Region}/queue/competitive/season/{seasonData[0]}?startIndex=0&size=0",
                    true
                )
                .ConfigureAwait(false);
            if (leaderboardResponse.Content != null && leaderboardResponse.IsSuccessful)
            {
                var leaderboardcontent = JsonSerializer.Deserialize<LeaderboardsResponse>(
                    leaderboardResponse.Content
                );
                try
                {
                    rankData.MaxRr = leaderboardcontent.TierDetails[
                        ranks[0].ToString()
                    ].RankedRatingThreshold;
                }
                catch (Exception e)
                {
                    Constants.Log.Error(
                        "GetPlayerHistoryAsync Failed; leaderboardcontent error: {e}",
                        e
                    );
                }
            }
            else
            {
                Constants.Log.Error(
                    "GetPlayerHistoryAsync Failed; leaderboardResponse error: {e}",
                    leaderboardResponse.ErrorException
                );
            }
        }

        try
        {
            Dictionary<int, string> rankNames;
            lock (StaticCacheLock) { rankNames = _cachedRankNames; }
            if (rankNames == null)
            {
                var compPath = Constants.LocalAppDataPath + "\\ValAPI\\competitivetiers.json";
                if (File.Exists(compPath))
                {
                    var json = (await File.ReadAllTextAsync(compPath).ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        rankNames = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
                        lock (StaticCacheLock) { _cachedRankNames = rankNames; }
                    }
                }
            }

            if (rankNames != null)
            {
                for (int i = 0; i < ranks.Length; i++)
                {
                    rankNames.TryGetValue(ranks[i], out var rank);
                    rankData.RankImages[i] = new Uri(
                        Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{ranks[i]}.png"
                    );
                    rankData.RankNames[i] = rank;
                }

                rankNames.TryGetValue(peakTier, out var peakName);
                peakName ??= "UNRATED";
                rankData.PeakRankImage = new Uri(
                    Constants.LocalAppDataPath + $"\\ValAPI\\ranksimg\\{peakTier}.png"
                );
                rankData.PeakRankName = peakName;
                rankData.PeakRankTooltip =
                    peakTier > 0 && peakRr > 0 ? $"Peak: {peakName} ({peakRr} RR)" : $"Peak: {peakName}";
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPlayerHistoryAsync Failed; rank dictionary error: {e}", e);
        }

        return rankData;
    }

    private static async Task<Guid[]> GetSeasonsAsync()
    {
        var seasonData = new Guid[4];
        try
        {
            var response = await DoCachedRequestAsync(
                Method.Get,
                $"https://shared.{Constants.Region}.a.pvp.net/content-service/v3/content",
                true
            );

            if (!response.IsSuccessful)
            {
                Constants.Log.Error("GetSeasonsAsync Failed: {e}", response.ErrorException);
                return seasonData;
            }

            var data = JsonSerializer.Deserialize<ContentResponse>(response.Content);

            sbyte index = 0;
            var seasons = data.Seasons.Reverse();
            var acts = seasons.Where(season => season.Type == "act");

            foreach (var act in acts)
            {
                if (index >= seasonData.Length)
                    break;
                if (index > 0)
                {
                    seasonData[index] = act.Id;
                    index++;
                }
                if (index == 0 & act.IsActive)
                {
                    seasonData[0] = act.Id;
                    index++;
                }
            }
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetSeasonsAsync Failed: {Exception}", e);
        }

        return seasonData;
    }

    private static async Task<Uri> TrackerAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
            return null;
        try
        {
            var encodedUsername = Uri.EscapeDataString(username);
            var url = new Uri(
                "https://api.tracker.network/api/v2/valorant/standard/profile/riot/"
                    + encodedUsername
            );
            var response = await DoCachedRequestAsync(
                    Method.Get,
                    url.ToString(),
                    false,
                    false,
                    false,
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 OverwolfClient/0.190.0.13"
                )
                .ConfigureAwait(false);
            var numericStatusCode = (short)response.StatusCode;
            if (numericStatusCode == 200)
                return new Uri("https://tracker.gg/valorant/profile/riot/" + encodedUsername);
        }
        catch (Exception e)
        {
            Constants.Log.Error("TrackerAsync Failed: {Exception}", e);
        }
        return null;
    }

    private static async Task<PresencesResponse> GetPresencesAsync()
    {
        var options = new RestClientOptions($"https://127.0.0.1:{Constants.Port}/chat/v4/presences")
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                true
        };
        var client = new RestClient(options);
        var base64String = "";
        try
        {
            base64String = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"riot:{Constants.LPassword}")
            );
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPresencesAsync Failed; To Base 64 failed: {Exception}", e);
            return null;
        }

        var request = new RestRequest()
            .AddHeader("Authorization", $"Basic {base64String}")
            .AddHeader("X-Riot-ClientPlatform", Constants.Platform)
            .AddHeader("X-Riot-ClientVersion", Constants.Version);
        client.UseSystemTextJson(
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            }
        );
        var response = await client
            .ExecuteGetAsync<PresencesResponse>(request)
            .ConfigureAwait(false);
        if (response.IsSuccessful)
            return response.Data;
        Constants.Log.Error(
            "GetPresencesAsync Failed: {e}. Presences: {presences}",
            response.ErrorException,
            response.Data
        );
        return null;
    }

    private async Task<PlayerUIData> GetPresenceInfoAsync(Guid puuid, PresencesResponse presences)
    {
        PlayerUIData playerUiData = new() { BackgroundColour = "#252A40", Puuid = puuid };

        try
        {
            if (presences?.Presences == null)
                return playerUiData;

            var friend = presences.Presences.FirstOrDefault(f => f.Puuid == puuid && f.Product == "valorant")
                         ?? presences.Presences.FirstOrDefault(f => f.Puuid == puuid);

            if (friend == null || string.IsNullOrEmpty(friend.Private))
                return playerUiData;

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(friend.Private));
            var content = JsonSerializer.Deserialize<PresencesPrivate>(json);
            if (content == null)
                return playerUiData;

            playerUiData.PartyUuid = content.PartyId;

            if (puuid != Constants.Ppuuid)
                return playerUiData;


            var presMap = !string.IsNullOrEmpty(content.MatchMap) ? content.MatchMap : content.PartyOwnerMatchMap;
            if (!string.IsNullOrEmpty(presMap))
            {
                var resolvedMap = MapHelper.ResolveMapName(presMap);
                if (!string.IsNullOrEmpty(resolvedMap) && resolvedMap != "Bilinmeyen Harita")
                {
                    MatchInfo.Map = resolvedMap;
                }
                var mapImg = MapHelper.ResolveMapImage(presMap);
                if (mapImg != null)
                {
                    MatchInfo.MapImage = mapImg;
                }
            }

            playerUiData.BackgroundColour = "#181E34";
            Constants.PPartyId = content.PartyId;

            if (content?.ProvisioningFlow == "CustomGame")
            {
                MatchInfo.GameMode = "Custom";
                MatchInfo.GameModeImage = new Uri(
                    Constants.LocalAppDataPath
                        + "\\ValAPI\\gamemodeimg\\96bd3920-4f36-d026-2b28-c683eb0bcac5.png"
                );
                return playerUiData;
            }
            var textInfo = new CultureInfo("en-US", false).TextInfo;

            var gameModeName = "";
            var gameModeId = Guid.Parse("96bd3920-4f36-d026-2b28-c683eb0bcac5");
            QueueId = content?.QueueId;
            Status = content?.SessionLoopState;

            switch (content?.QueueId)
            {
                case "competitive":
                    gameModeName = "Competitive";
                    break;
                case "unrated":
                    gameModeName = "Unrated";
                    break;
                case "deathmatch":
                    gameModeId = Guid.Parse("a8790ec5-4237-f2f0-e93b-08a8e89865b2");
                    break;
                case "spikerush":
                    gameModeId = Guid.Parse("e921d1e6-416b-c31f-1291-74930c330b7b");
                    break;
                case "ggteam":
                    gameModeId = Guid.Parse("a4ed6518-4741-6dcb-35bd-f884aecdc859");
                    break;
                case "newmap":
                    gameModeName = "New Map";
                    break;
                case "onefa":
                    gameModeId = Guid.Parse("96bd3920-4f36-d026-2b28-c683eb0bcac5");
                    break;
                case "snowball":
                    gameModeId = Guid.Parse("57038d6d-49b1-3a74-c5ef-3395d9f23a97");
                    break;
                case "abilitydraftarena":
                case "arena":
                    gameModeName = "Ability Draft Arena";
                    break;
                default:
                    gameModeName = !string.IsNullOrEmpty(content?.QueueId) ? textInfo.ToTitleCase(content.QueueId) : "";
                    break;
            }

            MatchInfo.GameMode = gameModeName;

            if (string.IsNullOrEmpty(gameModeName) && File.Exists(Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json"))
            {
                var gmJson = (await File.ReadAllTextAsync(Constants.LocalAppDataPath + "\\ValAPI\\gamemode.json").ConfigureAwait(false)).Trim().Trim('\uFEFF', '\u200B');
                if (!string.IsNullOrWhiteSpace(gmJson))
                {
                    var gamemodes = JsonSerializer.Deserialize<Dictionary<Guid, string>>(gmJson);
                    if (gamemodes != null && gamemodes.TryGetValue(gameModeId, out var gamemode))
                        MatchInfo.GameMode = gamemode;
                }
            }

            if (File.Exists(Constants.LocalAppDataPath + $"\\ValAPI\\gamemodeimg\\{gameModeId}.png"))
            {
                MatchInfo.GameModeImage = new Uri(
                    Constants.LocalAppDataPath + $"\\ValAPI\\gamemodeimg\\{gameModeId}.png"
                );
            }
        }
        catch (InvalidOperationException)
        {
            return playerUiData;
        }
        catch (Exception e)
        {
            Constants.Log.Error("GetPresenceInfoAsync Failed; To Base 64 failed: {Exception}", e);
        }

        return playerUiData;
    }
}

