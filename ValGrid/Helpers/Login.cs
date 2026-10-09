using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using ValGrid.Objects;

namespace ValGrid.Helpers;

public static class Login
{
    public static async Task<bool> LocalLoginAsync()
    {
        await GetLatestVersionAsync().ConfigureAwait(false);
        var options = new RestClientOptions(
            $"https://127.0.0.1:{Constants.Port}/entitlements/v1/token"
        )
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                true
        };
        var client = new RestClient(options);
        var request = new RestRequest().AddHeader(
            "Authorization",
            $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{Constants.LPassword}"))}"
        );
        var response = await client
            .ExecuteGetAsync<EntitlementsResponse>(request)
            .ConfigureAwait(false);
        if (!response.IsSuccessful)
        {
            Constants.Log.Error("LocalLoginAsync Failed");
            return false;
        }

        Constants.AccessToken = response.Data.AccessToken;
        Constants.EntitlementToken = response.Data.Token;
        Constants.Ppuuid = response.Data.Subject;
        Constants.Log.Information("Logged in as {Ppuuid}", Constants.Ppuuid);
        return true;
    }

    public static async Task LocalRegionAsync()
    {
        var options = new RestClientOptions(
            new Uri($"https://127.0.0.1:{Constants.Port}/product-session/v1/external-sessions")
        )
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                true
        };

        var client = new RestClient(options);
        var request = new RestRequest()
            .AddHeader(
                "Authorization",
                $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{Constants.LPassword}"))}"
            )
            .AddHeader("X-Riot-ClientPlatform", Constants.Platform)
            .AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client
            .ExecuteGetAsync<ExternalSessionsResponse>(request)
            .ConfigureAwait(false);
        if (response.IsSuccessful && response.Content != "{}" && response.Data?.ExtensionData != null)
        {
            foreach (var session in response.Data.ExtensionData)
            {
                try
                {
                    var game = session.Value.Deserialize<ExternalSessions>();
                    if (game is { ProductId: "valorant" } && game.LaunchConfiguration?.Arguments != null)
                    {
                        foreach (var arg in game.LaunchConfiguration.Arguments)
                        {
                            if (arg.Contains("-ares-deployment=", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = arg.Split('=');
                                if (parts.Length > 1)
                                {
                                    var dep = parts[1].Trim().ToLowerInvariant();
                                    switch (dep)
                                    {
                                        case "latam":
                                            Constants.Region = "na";
                                            Constants.Shard = "latam";
                                            break;
                                        case "br":
                                            Constants.Region = "na";
                                            Constants.Shard = "br";
                                            break;
                                        default:
                                            Constants.Region = dep;
                                            Constants.Shard = dep;
                                            break;
                                    }
                                    break;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(Constants.Region))
                            break;
                    }
                }
                catch { }
            }
        }

        if (string.IsNullOrEmpty(Constants.Region))
        {
            try
            {
                var valConfigDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VALORANT", "Saved", "Config"
                );
                if (Directory.Exists(valConfigDir))
                {
                    var dirs = Directory.GetDirectories(valConfigDir);
                    string prefix = Constants.Ppuuid != Guid.Empty ? Constants.Ppuuid.ToString().ToLowerInvariant() : "";
                    foreach (var dir in dirs)
                    {
                        var name = Path.GetFileName(dir).ToLowerInvariant();
                        if (!string.IsNullOrEmpty(prefix) && name.StartsWith(prefix))
                        {
                            var parts = name.Split('-');
                            if (parts.Length >= 6)
                            {
                                var r = parts[^1].Trim();
                                if (!string.IsNullOrEmpty(r) && r.Length <= 5)
                                {
                                    Constants.Region = r;
                                    Constants.Shard = r;
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(Constants.Region))
                    {
                        var knownRegions = new[] { "eu", "na", "ap", "kr", "latam", "br" };
                        foreach (var dir in dirs)
                        {
                            var name = Path.GetFileName(dir).ToLowerInvariant();
                            foreach (var reg in knownRegions)
                            {
                                if (name.EndsWith("-" + reg))
                                {
                                    Constants.Region = reg;
                                    Constants.Shard = reg;
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(Constants.Region)) break;
                        }
                    }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(Constants.Region))
        {
            Constants.Region = "eu";
            Constants.Shard = "eu";
        }
    }

    public static void AddAuthToRequest(RestRequest request)
    {
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        if (!string.IsNullOrEmpty(Constants.Version))
            request.AddHeader("User-Agent", $"ShooterGame/18 {Constants.Version} valorant");
    }

    public static async Task<string[]> GetNameServiceGetUsernamesAsync(Guid[] puuids)
    {
        if (puuids == null || puuids.Length == 0)
            return null;

        if (string.IsNullOrEmpty(Constants.AccessToken) || string.IsNullOrEmpty(Constants.EntitlementToken))
        {
            await LocalLoginAsync().ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(Constants.Region))
            Constants.Region = "eu";

        var options = new RestClientOptions(
            new Uri($"https://pd.{Constants.Region}.a.pvp.net/name-service/v2/players")
        )
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                true
        };
        var client = new RestClient(options);
        RestRequest request = new() { RequestFormat = DataFormat.Json };

        AddAuthToRequest(request);

        string[] body = new string[puuids.Length];
        for (int i = 0; i < puuids.Length; i++)
        {
            body[i] = puuids[i].ToString();
        }

        request.AddJsonBody(body);
        var response = await client.ExecutePutAsync(request).ConfigureAwait(false);
        string[] names = new string[puuids.Length];
        if (response.IsSuccessful)
            try
            {
                var incorrectContent = response.Content.Replace("\n", string.Empty);
                var content = JsonSerializer.Deserialize<NameServiceResponse[]>(incorrectContent);
                var map = new Dictionary<Guid, string>();
                if (content != null)
                {
                    foreach (var item in content)
                    {
                        if (!string.IsNullOrEmpty(item.Subject) &&
                            Guid.TryParse(item.Subject, out var subjGuid) &&
                            subjGuid != Guid.Empty &&
                            !string.IsNullOrEmpty(item.GameName) &&
                            !string.IsNullOrEmpty(item.TagLine))
                        {
                            map[subjGuid] = $"{item.GameName}#{item.TagLine}";
                        }
                    }
                }
                for (int i = 0; i < puuids.Length; i++)
                {
                    names[i] = map.TryGetValue(puuids[i], out var n) ? n : "";
                }
                return names;
            }
            catch (Exception e)
            {
                Constants.Log?.Error("GetNameServiceGetUsernameAsync Failed: {e}", e);
                return new string[] { "" };
            }

        Constants.Log?.Error("GetNameServiceGetUsernameAsync Failed: {e}", response.ErrorException);
        return new string[] { "" };
    }

    public static async Task<string> GetNameServiceGetUsernameAsync(Guid puuid)
    {
        if (puuid == Guid.Empty)
            return null;
        string[] names = await GetNameServiceGetUsernamesAsync(new Guid[1] { puuid });
        return names[0];
    }

    private static async Task GetLatestVersionAsync()
    {
        try
        {
            var path = Constants.LocalAppDataPath + "\\ValAPI\\version.json";
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path).ConfigureAwait(false);
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                {
                    Constants.Version = lines[0].Trim();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Login GetLatestVersionAsync failed: {e}", ex.Message);
        }

        if (string.IsNullOrEmpty(Constants.Version))
        {
            Constants.Version = "release-09.00-shipping";
        }
    }

    public static async Task<RestResponse> DoCachedRequestAsync(
        Method method,
        string url,
        bool addRiotAuth,
        bool bypassCache = false,
        bool displayError = true,
        string? userAgent = null
    )
    {
        var attemptCache = method == Method.Get && !bypassCache;
        if (attemptCache)
            if (Constants.UrlToBody.TryGetValue(url, out var res))
                return res;
        var client = new RestClient(url);

        if (userAgent is not null)
        {
            client.AddDefaultHeader("User-Agent", userAgent);
        }

        var request = new RestRequest();
        if (addRiotAuth)
        {
            AddAuthToRequest(request);
        }

        var response = await client.ExecuteAsync(request, method).ConfigureAwait(false);
        if (!response.IsSuccessful && displayError)
        {
            Constants.Log.Error("Request to {url} Failed: {e}", url, response.ErrorException);
            return response;
        }

        if (attemptCache && response.IsSuccessful)
            Constants.UrlToBody.TryAdd(url, response);
        return response;
    }
}

