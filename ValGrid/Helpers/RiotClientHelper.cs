using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ValGrid.Helpers;

public static class RiotClientHelper
{
    private static string _cachedRiotClientPath;
    private static bool? _isValorantInstalledCache;

    public static bool IsValorantInstalled()
    {
        if (_isValorantInstalledCache.HasValue)
            return _isValorantInstalledCache.Value;

        var path = GetRiotClientExePath();
        _isValorantInstalledCache = !string.IsNullOrEmpty(path);
        return _isValorantInstalledCache.Value;
    }

    public static string GetRiotClientExePath()
    {
        if (!string.IsNullOrEmpty(_cachedRiotClientPath) && File.Exists(_cachedRiotClientPath))
            return _cachedRiotClientPath;

        // 1. Check ProgramData RiotClientInstalls.json
        try
        {
            var pData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var jsonPath = Path.Combine(pData, "Riot Games", "RiotClientInstalls.json");
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("rc_default", out var rcDef) &&
                    !string.IsNullOrEmpty(rcDef.GetString()) &&
                    File.Exists(rcDef.GetString()))
                {
                    _cachedRiotClientPath = rcDef.GetString();
                    return _cachedRiotClientPath;
                }

                if (doc.RootElement.TryGetProperty("rc_live", out var rcLive) &&
                    !string.IsNullOrEmpty(rcLive.GetString()) &&
                    File.Exists(rcLive.GetString()))
                {
                    _cachedRiotClientPath = rcLive.GetString();
                    return _cachedRiotClientPath;
                }
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning("Error reading RiotClientInstalls.json: {e}", ex.Message);
        }

        // 2. Standard drive paths
        var potentialPaths = new[]
        {
            @"C:\Riot Games\Riot Client\RiotClientServices.exe",
            @"D:\Riot Games\Riot Client\RiotClientServices.exe",
            @"E:\Riot Games\Riot Client\RiotClientServices.exe",
            @"F:\Riot Games\Riot Client\RiotClientServices.exe",
            @"C:\Program Files\Riot Vanguard\RiotClientServices.exe"
        };

        foreach (var p in potentialPaths)
        {
            if (File.Exists(p))
            {
                _cachedRiotClientPath = p;
                return _cachedRiotClientPath;
            }
        }

        return null;
    }

    public static string GetValorantLivePath()
    {
        try
        {
            var pData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var jsonPath = Path.Combine(pData, "Riot Games", "RiotClientInstalls.json");
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("associated_client", out var assoc))
                {
                    foreach (var prop in assoc.EnumerateObject())
                    {
                        var dir = prop.Name.TrimEnd('/', '\\');
                        if (dir.Contains("VALORANT", StringComparison.OrdinalIgnoreCase) && Directory.Exists(dir))
                        {
                            return dir;
                        }
                    }
                }
            }
        }
        catch { }

        var potentialDirs = new[]
        {
            @"C:\Riot Games\VALORANT\live",
            @"D:\Riot Games\VALORANT\live",
            @"E:\Riot Games\VALORANT\live",
            @"F:\Riot Games\VALORANT\live"
        };

        foreach (var d in potentialDirs)
        {
            if (Directory.Exists(d)) return d;
        }

        return null;
    }

    public static bool IsRiotClientRunning()
    {
        try
        {
            return Process.GetProcessesByName("RiotClientServices").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool LaunchRiotClient()
    {
        try
        {
            var exePath = GetRiotClientExePath();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Constants.Log?.Warning("LaunchRiotClient: RiotClientServices.exe not found.");
                return false;
            }

            Constants.Log?.Information("Launching Riot Client from {path}", exePath);
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "",
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            Constants.Log?.Error("LaunchRiotClient exception: {e}", ex);
            return false;
        }
    }

    public static async Task<bool> WaitForRiotLoginAsync(
        Action<string> onStatusUpdate,
        CancellationToken cancellationToken)
    {
        // 1. First test if already actively logged in via local port
        var hasLocalOnInit = await Checks.CheckLocalAsync().ConfigureAwait(false);
        if (hasLocalOnInit)
        {
            var alreadyLoggedIn = await Login.LocalLoginAsync().ConfigureAwait(false);
            if (alreadyLoggedIn)
            {
                await Login.LocalRegionAsync().ConfigureAwait(false);
                onStatusUpdate?.Invoke("Oturum doğrulandı! Mağaza verileri çekiliyor...");
                return true;
            }
        }

        // 2. If not actively logged in (even if lockfile exists in background), launch/foreground Riot Client
        onStatusUpdate?.Invoke("Riot Client başlatılıyor...");
        LaunchRiotClient();
        await Task.Delay(1800, cancellationToken).ConfigureAwait(false);

        // 3. Wait in a loop for lockfile & login tokens (up to 90 seconds)
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(90);
        int retryCount = 0;

        while (!cancellationToken.IsCancellationRequested && (DateTime.UtcNow - startTime) < timeout)
        {
            try
            {
                var hasLocal = await Checks.CheckLocalAsync().ConfigureAwait(false);
                if (hasLocal)
                {
                    var loggedIn = await Login.LocalLoginAsync().ConfigureAwait(false);
                    if (loggedIn)
                    {
                        await Login.LocalRegionAsync().ConfigureAwait(false);
                        var validLogin = await Checks.CheckLoginAsync().ConfigureAwait(false);
                        if (validLogin || (Constants.Ppuuid != Guid.Empty && !string.IsNullOrEmpty(Constants.AccessToken)))
                        {
                            onStatusUpdate?.Invoke("Giriş başarılı! Mağaza verileri çekiliyor...");
                            return true;
                        }
                    }
                    else
                    {
                        onStatusUpdate?.Invoke("Riot Client algılandı! Kullanıcı girişi bekleniyor...");
                        retryCount++;
                        // If lockfile exists but local login fails repeatedly (e.g. background stub), poke LaunchRiotClient once
                        if (retryCount == 4)
                        {
                            LaunchRiotClient();
                        }
                    }
                }
                else
                {
                    onStatusUpdate?.Invoke("Riot Client başlatılması bekleniyor...");
                    retryCount++;
                    if (retryCount % 4 == 0)
                    {
                        LaunchRiotClient();
                    }
                }
            }
            catch (Exception ex)
            {
                Constants.Log?.Warning("WaitForRiotLoginAsync polling error: {e}", ex.Message);
            }

            await Task.Delay(1200, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

