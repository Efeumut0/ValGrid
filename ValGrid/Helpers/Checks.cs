using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace ValGrid.Helpers;

public class Checks
{
    public static async Task<bool> CheckLoginAsync()
    {
        if (Constants.Region == null || Constants.Ppuuid == Guid.Empty)
            return false;
        var options = new RestClientOptions(
            $"https://pd.{Constants.Region}.a.pvp.net/account-xp/v1/players/{Constants.Ppuuid}"
        )
        {
            MaxTimeout = 5000
        };
        var client = new RestClient(options);

        var request = new RestRequest()
            .AddHeader("Authorization", $"Bearer {Constants.AccessToken}")
            .AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken)
            .AddHeader("X-Riot-ClientPlatform", Constants.Platform)
            .AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync(request).ConfigureAwait(false);
        if (response.IsSuccessful)
            return true;
        Constants.Log?.Warning(
            "CheckLoginAsync() failed. Response: {Response}",
            response.ErrorException
        );
        return false;
    }

    public static async Task<bool> IsRiotConnectedAsync()
    {
        try
        {
            var hasLocal = await CheckLocalAsync().ConfigureAwait(false);
            if (hasLocal)
            {
                var loggedIn = await Login.LocalLoginAsync().ConfigureAwait(false);
                if (loggedIn)
                {
                    await Login.LocalRegionAsync().ConfigureAwait(false);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(Constants.AccessToken) && !string.IsNullOrEmpty(Constants.EntitlementToken))
            {
                return await CheckLoginAsync().ConfigureAwait(false);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> CheckLocalAsync()
    {
        try
        {
            var lockfileLocation =
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Riot Games\Riot Client\Config\lockfile";

            if (!File.Exists(lockfileLocation))
                return false;

            string lockFileString = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var file = new FileStream(
                        lockfileLocation,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete
                    );
                    using var reader = new StreamReader(file, Encoding.UTF8);
                    lockFileString = await reader.ReadToEndAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(lockFileString))
                        break;
                }
                catch
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }

            if (string.IsNullOrWhiteSpace(lockFileString))
                return false;

            var parts = lockFileString.Split(":");
            if (parts.Length < 4)
                return false;

            Constants.Port = parts[2];
            Constants.LPassword = parts[3];
            return true;
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("CheckLocalAsync error: {e}", ex.Message);
            return false;
        }
    }

    public static async Task<bool> CheckMatchAsync()
    {
        var options1 = new RestClientOptions(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/core-game/v1/players/{Constants.Ppuuid}"
        ) { MaxTimeout = 5000 };
        var client = new RestClient(options1);
        var request = new RestRequest();
        request.AddHeader("X-Riot-Entitlements-JWT", Constants.EntitlementToken);
        request.AddHeader("Authorization", $"Bearer {Constants.AccessToken}");
        request.AddHeader("X-Riot-ClientPlatform", Constants.Platform);
        request.AddHeader("X-Riot-ClientVersion", Constants.Version);
        var response = await client.ExecuteGetAsync(request).ConfigureAwait(false);
        if (response.IsSuccessful)
            return true;

        var options2 = new RestClientOptions(
            $"https://glz-{Constants.Shard}-1.{Constants.Region}.a.pvp.net/pregame/v1/players/{Constants.Ppuuid}"
        ) { MaxTimeout = 5000 };
        client = new RestClient(options2);
        response = await client.ExecuteGetAsync(request).ConfigureAwait(false);
        if (response.IsSuccessful)
            return true;

        return false;
    }
}

