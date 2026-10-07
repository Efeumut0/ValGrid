using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ValGridWatcher;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
    private const int ASFW_ANY = -1;

    private const string MutexName = @"Global\ValGrid_Watcher_Singleton_Mutex";
    private const string ShutdownEventName = @"Global\ValGrid_Watcher_Shutdown_Event";
    private const string BringToFrontEventName = @"ValGrid_BringToFront_Event";

    private static string GetLogPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ValGrid",
            "logs"
        );
        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch { }
        return Path.Combine(dir, "watcher.log");
    }

    private static void Log(string message)
    {
        try
        {
            var logPath = GetLogPath();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, line);

            var fi = new FileInfo(logPath);
            if (fi.Exists && fi.Length > 150_000)
            {
                var lines = File.ReadAllLines(logPath);
                if (lines.Length > 200)
                {
                    var tail = lines[^150..];
                    File.WriteAllLines(logPath, tail);
                }
            }
        }
        catch { }
    }

    private static string? FindValGridExe()
    {
        // 1. Same directory as the watcher (Installer installation)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var sameDirExe = Path.Combine(baseDir, "ValGrid.exe");
        if (File.Exists(sameDirExe)) return sameDirExe;

        // 2. LocalAppData Programs
        var localAppExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "ValGrid", "ValGrid.exe"
        );
        if (File.Exists(localAppExe)) return localAppExe;

        // 3. Program Files
        var progFilesExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "ValGrid", "ValGrid.exe"
        );
        if (File.Exists(progFilesExe)) return progFilesExe;

        // 4. Dev environment relative path
        var devExe = Path.Combine(baseDir, "..", "..", "..", "..", "ValGrid", "bin", "Release", "net6.0-windows", "ValGrid.exe");
        if (File.Exists(devExe)) return Path.GetFullPath(devExe);

        var devExe2 = Path.Combine(baseDir, "..", "ValGrid", "bin", "Release", "net6.0-windows", "ValGrid.exe");
        if (File.Exists(devExe2)) return Path.GetFullPath(devExe2);

        return null;
    }

    [STAThread]
    static void Main()
    {
        try
        {
            // 1. Singleton Mutex Check
            using var mutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                Log("Başka bir ValGridWatcher örneği zaten arka planda çalışıyor. Çıkılıyor.");
                return;
            }

            using var shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ShutdownEventName);

            var currentProc = Process.GetCurrentProcess();
            Log($"=== ValGridWatcher (Sessiz Arka Plan İzleyici) Başlatıldı [PID: {currentProc.Id}] ===");

            bool isSessionActive = false;
            int detectedCount = 0;
            int absentCount = 0;

            while (true)
            {
                // Check if shutdown was requested from ValGrid Settings
                if (shutdownEvent.WaitOne(0))
                {
                    Log("Kullanıcı tarafından kapatma sinyali alındı. ValGridWatcher sonlandırılıyor.");
                    break;
                }

                try
                {
                    bool isGameFound = Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0
                                    || Process.GetProcessesByName("VALORANT").Length > 0;

                    if (isGameFound)
                    {
                        detectedCount++;
                        absentCount = 0;

                        // Oyunun stabil olarak çalıştığından emin olmak için 2 döngü (5 saniye) kontrol
                        if (!isSessionActive && detectedCount >= 2)
                        {
                            Log("Valorant oyunu açık tespit edildi! ValGrid kontrol ediliyor...");

                            if (Process.GetProcessesByName("ValGrid").Length == 0)
                            {
                                var valGridExe = FindValGridExe();
                                if (valGridExe != null && File.Exists(valGridExe))
                                {
                                    try
                                    {
                                        Log($"ValGrid başlatılıyor: {valGridExe}");
                                        try { AllowSetForegroundWindow(ASFW_ANY); } catch { }

                                        var psi = new ProcessStartInfo(valGridExe)
                                        {
                                            WorkingDirectory = Path.GetDirectoryName(valGridExe) ?? "",
                                            UseShellExecute = true
                                        };
                                        var proc = Process.Start(psi);
                                        Log($"ValGrid başlatıldı. PID: {proc?.Id}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log($"ValGrid başlatılamadı: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    Log("Hata: ValGrid.exe bulunamadı!");
                                }
                            }
                            else
                            {
                                Log("ValGrid zaten çalışıyor. Öne getirme sinyali iletiliyor...");
                                try
                                {
                                    if (EventWaitHandle.TryOpenExisting(BringToFrontEventName, out var evt))
                                    {
                                        evt.Set();
                                    }
                                }
                                catch { }
                            }

                            isSessionActive = true;
                            Log("Oturum aktif. Oyun kapanana kadar izleniyor.");
                        }
                    }
                    else
                    {
                        detectedCount = 0;
                        absentCount++;

                        // Oyun kapandıktan sonra oturumu sıfırla (alt-tab / çözünürlük dalgalanması için 12 saniye bekle)
                        if (isSessionActive && absentCount >= 5)
                        {
                            Log("Valorant tamamen kapandı. Yeni oturum için bekleniyor.");
                            isSessionActive = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Döngü hatası: {ex.Message}");
                }

                Thread.Sleep(2500);
            }
        }
        catch (Exception ex)
        {
            Log($"ValGridWatcher KRİTİK HATA: {ex}");
        }
    }
}
