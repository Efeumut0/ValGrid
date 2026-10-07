using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace ValGrid.Helpers;

public static class WatcherHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "ValGridWatcher";
    private const string ShutdownEventName = @"Global\ValGrid_Watcher_Shutdown_Event";
    private static readonly string FlagFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ValGrid",
        "watcher_enabled.flag"
    );

    public static string? FindWatcherExe()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Same directory (Installed)
        var sameDir = Path.Combine(baseDir, "ValGridWatcher.exe");
        if (File.Exists(sameDir)) return sameDir;

        // 2. LocalAppData Programs
        var localApp = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "ValGrid", "ValGridWatcher.exe"
        );
        if (File.Exists(localApp)) return localApp;

        // 3. Program Files
        var progFiles = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "ValGrid", "ValGridWatcher.exe"
        );
        if (File.Exists(progFiles)) return progFiles;

        // 4. Dev environment relative path
        var devExe = Path.Combine(baseDir, "..", "..", "..", "..", "Watcher", "bin", "Release", "net6.0-windows", "ValGridWatcher.exe");
        if (File.Exists(devExe)) return Path.GetFullPath(devExe);

        return null;
    }

    public static bool IsWatcherRunning()
    {
        return Process.GetProcessesByName("ValGridWatcher").Length > 0;
    }

    public static bool IsWatcherAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(RunValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsWatcherEnabled()
    {
        try
        {
            // 1. Check flag file if exists
            if (File.Exists(FlagFilePath))
            {
                var content = File.ReadAllText(FlagFilePath).Trim();
                if (content == "1") return true;
                if (content == "0") return false;
            }

            // 2. Fallback to registry check
            return IsWatcherAutoStartEnabled();
        }
        catch
        {
            return false;
        }
    }

    public static bool SetWatcherEnabled(bool enable)
    {
        try
        {
            // Clean up any old legacy names
            CleanLegacyWatchers();

            // Save state to persistent flag file
            try
            {
                var dir = Path.GetDirectoryName(FlagFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(FlagFilePath, enable ? "1" : "0");
            }
            catch { }

            if (enable)
            {
                var watcherExe = FindWatcherExe();
                if (watcherExe != null && File.Exists(watcherExe))
                {
                    // Set startup in HKCU Run
                    using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                    key?.SetValue(RunValueName, $"\"{watcherExe}\"");

                    // Start process if not running
                    if (!IsWatcherRunning())
                    {
                        Process.Start(new ProcessStartInfo(watcherExe)
                        {
                            WorkingDirectory = Path.GetDirectoryName(watcherExe) ?? "",
                            UseShellExecute = true
                        });
                    }
                    return true;
                }
                return false;
            }
            else
            {
                // Remove startup registry
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.DeleteValue(RunValueName, false);

                // Send clean shutdown signal
                try
                {
                    if (EventWaitHandle.TryOpenExisting(ShutdownEventName, out var shutdownEvt))
                    {
                        shutdownEvt.Set();
                    }
                }
                catch { }

                // Terminate processes if still running
                foreach (var p in Process.GetProcessesByName("ValGridWatcher"))
                {
                    try { p.Kill(); } catch { }
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "SetWatcherEnabled failed in WatcherHelper");
            return false;
        }
    }

    public static void CleanLegacyWatchers()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key != null)
            {
                key.DeleteValue("ValPulseWatcher", false);
                key.DeleteValue("NOWT-ValorantWatcher", false);
                key.DeleteValue("ValorantWatcher", false);
            }
        }
        catch { }

        try
        {
            foreach (var p in Process.GetProcessesByName("ValPulseWatcher"))
            {
                try { p.Kill(); } catch { }
            }
            foreach (var p in Process.GetProcessesByName("ValorantWatcher"))
            {
                try { p.Kill(); } catch { }
            }
        }
        catch { }
    }
}
