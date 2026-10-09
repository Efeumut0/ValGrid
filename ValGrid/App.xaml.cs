using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using RestoreWindowPlace;
using Serilog;
using ValGrid.Helpers;
using ValGrid.Properties;
using ValGrid.ViewModels;
using static ValGrid.Helpers.ValApi;

namespace ValGrid;

public partial class App : Application
{
    public App()
    {
        Dispatcher.UnhandledException += OnDispatcherUnhandledException;

        var valGridAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid";
        var legacyAppDataNowt = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\NOWT";
        MigrateLegacyDataIfNeeded(legacyAppDataNowt, valGridAppData);

        WindowPlace = new WindowPlace(valGridAppData + "\\placement.config");

        // Read setup language if deployed by installer
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var setupLangFile = System.IO.Path.Combine(baseDir, "setup_lang.txt");
            var setupLangFileValGrid = System.IO.Path.Combine(valGridAppData, "setup_lang.txt");
            var setupLangFileLegacy = System.IO.Path.Combine(legacyAppDataNowt, "setup_lang.txt");

            string? lang = null;
            if (System.IO.File.Exists(setupLangFile))
            {
                lang = System.IO.File.ReadAllText(setupLangFile).Trim();
                try { System.IO.File.Delete(setupLangFile); } catch { }
            }
            else if (System.IO.File.Exists(setupLangFileValGrid))
            {
                lang = System.IO.File.ReadAllText(setupLangFileValGrid).Trim();
                try { System.IO.File.Delete(setupLangFileValGrid); } catch { }
            }
            else if (System.IO.File.Exists(setupLangFileLegacy))
            {
                lang = System.IO.File.ReadAllText(setupLangFileLegacy).Trim();
                try { System.IO.File.Delete(setupLangFileLegacy); } catch { }
            }

            if (!string.IsNullOrEmpty(lang) && (lang == "tr" || lang == "en"))
            {
                Settings.Default.Language = lang;
                Settings.Default.Save();
            }
        }
        catch { }

        // Default to Turkish if not set
        if (string.IsNullOrEmpty(Settings.Default.Language))
        {
            Settings.Default.Language = "tr";
            Settings.Default.Save();
        }

        var activeLang = Settings.Default.Language;
        try
        {
            var uiCulture = new CultureInfo(activeLang == "en" ? "en-US" : "tr-TR");
            Thread.CurrentThread.CurrentUICulture = uiCulture;
            CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
            ValGrid.Properties.Resources.Culture = uiCulture;

            // Keep CurrentCulture invariant so WPF DirectWrite, pack URIs, and font metrics don't suffer the Turkish I bug
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        }
        catch
        {
            var fallback = new CultureInfo("tr-TR");
            Thread.CurrentThread.CurrentUICulture = fallback;
            ValGrid.Properties.Resources.Culture = fallback;
        }
    }

    public WindowPlace WindowPlace { get; }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        var ex = e.Exception;
        while (ex != null)
        {
            Constants.Log.Error(
                "Unhandled Exception: {Message}\n{Stacktrace}",
                ex.Message,
                ex.StackTrace
            );
            ex = ex.InnerException;
        }
        e.Handled = true;
    }

    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _bringToFrontEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "ValGrid_SingleInstance_Mutex_EfeUmut";
        const string eventName = "ValGrid_BringToFront_Event";

        bool isNewInstance;
        try
        {
            _singleInstanceMutex = new Mutex(true, mutexName, out isNewInstance);
        }
        catch
        {
            isNewInstance = true;
        }

        if (!isNewInstance)
        {
            // Var olan ValGrid örneğini öne getir ve çık
            try
            {
                if (EventWaitHandle.TryOpenExisting(eventName, out var evt))
                {
                    evt.Set();
                }
            }
            catch { }

            Shutdown();
            return;
        }

        try
        {
            _bringToFrontEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (_bringToFrontEvent.WaitOne())
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                if (Application.Current.MainWindow is MainWindow mw)
                                {
                                    mw.BringToForeground();
                                }
                            });
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }
        catch { }

        base.OnStartup(e);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.4.08";

        var valGridAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ValGrid";
        var legacyAppDataNowt = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\NOWT";
        MigrateLegacyDataIfNeeded(legacyAppDataNowt, valGridAppData);

        Constants.LocalAppDataPath = valGridAppData;
        Constants.Log = new LoggerConfiguration().MinimumLevel
            .Debug()
            .WriteTo.Async(
                a =>
                    a.File(
                        Constants.LocalAppDataPath + "\\logs\\log.txt",
                        shared: true,
                        rollingInterval: RollingInterval.Day
                    )
            )
            .CreateLogger();
        Constants.Log.Information("ValGrid Application Start. Version: {Version}", version);

        try
        {
            WatcherHelper.CleanLegacyWatchers();
        }
        catch { }

        CheckAndUpdateJsonAsync().ConfigureAwait(false);

        var conventionViewFactory = new NamingConventionViewFactory();

        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                .AddTransient<HomeViewModel>()
                .AddTransient<InfoViewModel>()
                .AddTransient<MatchViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IViewFactory>(conventionViewFactory)
                .BuildServiceProvider()
        );

        // Periodic auto update system (checks on launch + every 30 minutes in background)
        UpdateHelper.InitializeAutoUpdater();

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Constants.Log.Information("Application Stop");
        Settings.Default.Save();
        WindowPlace.Save();
    }

    private static void MigrateLegacyDataIfNeeded(string legacyDir, string newDir)
    {
        try
        {
            if (!System.IO.Directory.Exists(legacyDir)) return;
            if (!System.IO.Directory.Exists(newDir))
                System.IO.Directory.CreateDirectory(newDir);

            // Copy all json files from legacy NOWT folder if not yet existing in ValGrid
            foreach (var file in System.IO.Directory.GetFiles(legacyDir, "*.json"))
            {
                var dest = System.IO.Path.Combine(newDir, System.IO.Path.GetFileName(file));
                if (!System.IO.File.Exists(dest))
                {
                    System.IO.File.Copy(file, dest, false);
                }
            }

            // Copy placement.config if not present
            var placementSrc = System.IO.Path.Combine(legacyDir, "placement.config");
            var placementDest = System.IO.Path.Combine(newDir, "placement.config");
            if (System.IO.File.Exists(placementSrc) && !System.IO.File.Exists(placementDest))
            {
                System.IO.File.Copy(placementSrc, placementDest, false);
            }

            // Copy ValAPI folder (cached skin metadata, chromas, ranksimg, imgcache)
            var valApiSrc = System.IO.Path.Combine(legacyDir, "ValAPI");
            var valApiDest = System.IO.Path.Combine(newDir, "ValAPI");
            if (System.IO.Directory.Exists(valApiSrc) && !System.IO.Directory.Exists(valApiDest))
            {
                CopyDirectoryRecursive(valApiSrc, valApiDest);
            }
        }
        catch { }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        try
        {
            System.IO.Directory.CreateDirectory(targetDir);
            foreach (var file in System.IO.Directory.GetFiles(sourceDir))
            {
                var dest = System.IO.Path.Combine(targetDir, System.IO.Path.GetFileName(file));
                if (!System.IO.File.Exists(dest))
                    System.IO.File.Copy(file, dest, false);
            }
            foreach (var sub in System.IO.Directory.GetDirectories(sourceDir))
            {
                CopyDirectoryRecursive(sub, System.IO.Path.Combine(targetDir, System.IO.Path.GetFileName(sub)));
            }
        }
        catch { }
    }

    public static void RestartApp()
    {
        try
        {
            if (_singleInstanceMutex != null)
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }
        catch { }

        try
        {
            if (_bringToFrontEvent != null)
            {
                _bringToFrontEvent.Dispose();
                _bringToFrontEvent = null;
            }
        }
        catch { }

        try
        {
            var procPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(procPath) || !File.Exists(procPath))
            {
                procPath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (!string.IsNullOrEmpty(procPath) && File.Exists(procPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = procPath,
                    UseShellExecute = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                });
            }
        }
        catch (Exception ex)
        {
            Constants.Log?.Error(ex, "Failed to restart application");
        }

        Environment.Exit(0);
    }
}

