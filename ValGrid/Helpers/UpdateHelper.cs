using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows;
using AutoUpdaterDotNET;

namespace ValGrid.Helpers;

public static class UpdateHelper
{
    public const string GitHubReleasesLatestApiUrl = "https://api.github.com/repos/Efeumut0/ValGrid/releases/latest";
    private static Timer? _periodicTimer;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);
    private static bool _initialized = false;

    public static void InitializeAutoUpdater()
    {
        if (_initialized) return;

        try
        {
            var rawVersion = Constants.AppVersion;
            if (Version.TryParse(rawVersion, out var installedVer))
            {
                AutoUpdater.InstalledVersion = installedVer;
            }

            AutoUpdater.HttpUserAgent = "ValGrid-Updater";
            AutoUpdater.ReportErrors = false;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.RunUpdateAsAdmin = false;

            // Custom parser: directly parse GitHub's releases/latest REST API JSON payload.
            // Completely decouples update distribution from the main git branch commits or files.
            AutoUpdater.ParseUpdateInfoEvent += OnParseUpdateInfo;

            // Start periodic timer to check for updates every 30 minutes in the background
            _periodicTimer = new Timer(OnTimerTick, null, InitialDelay, CheckInterval);
            _initialized = true;
            Constants.Log?.Information("UpdateHelper periodic check initialized with GitHub Releases API. Interval: 30 minutes.");
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning(ex, "Failed to initialize UpdateHelper");
        }
    }

    private static void OnParseUpdateInfo(ParseUpdateInfoEventArgs args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args.RemoteData))
                return;

            using var doc = JsonDocument.Parse(args.RemoteData);
            var root = doc.RootElement;

            // Extract tag name (e.g., "v1.3.20" -> "1.3.20")
            var tagName = root.TryGetProperty("tag_name", out var tagElem) ? tagElem.GetString() ?? "" : "";
            var versionStr = tagName.Trim().TrimStart('v', 'V');

            var changelogUrl = root.TryGetProperty("html_url", out var htmlElem) ? htmlElem.GetString() ?? "" : "";

            var downloadUrl = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var dl) ? dl.GetString() ?? "" : "";
                        break;
                    }
                }
            }

            // Fallback direct release download URL if assets enumeration didn't catch an exe
            if (string.IsNullOrEmpty(downloadUrl) && !string.IsNullOrEmpty(tagName))
            {
                downloadUrl = $"https://github.com/Efeumut0/ValGrid/releases/download/{tagName}/ValGrid_Setup_{tagName}.exe";
            }

            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = versionStr,
                ChangelogURL = changelogUrl,
                DownloadURL = downloadUrl
            };
        }
        catch (Exception ex)
        {
            Constants.Log?.Warning(ex, "Failed to parse GitHub release update info in AutoUpdater");
        }
    }

    private static void OnTimerTick(object? state)
    {
        try
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                try
                {
                    AutoUpdater.ReportErrors = false;
                    AutoUpdater.Start(GitHubReleasesLatestApiUrl);
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning(ex, "Background AutoUpdater.Start tick failed");
                }
            });
        }
        catch { }
    }

    public static void CheckNow(bool reportErrors = false)
    {
        try
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                try
                {
                    AutoUpdater.ReportErrors = reportErrors;
                    AutoUpdater.Start(GitHubReleasesLatestApiUrl);
                }
                catch (Exception ex)
                {
                    Constants.Log?.Warning(ex, "Manual AutoUpdater.Start failed");
                }
            });
        }
        catch { }
    }
}
