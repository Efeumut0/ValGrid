# watch-valorant.ps1
# Valorant acildiginda yalnizca ValGrid uygulamasini otomatik baslatir.
# Arka planda sessizce calisir.

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = "C:\Users\Efe umut\Desktop\valo skin detecktor\ValGrid" }
$logFile    = Join-Path $scriptDir "watch-valorant.log"

function Log-Message([string]$msg) {
    try {
        $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        $line = "[$timestamp] $msg"
        Add-Content -Path $logFile -Value $line -Encoding UTF8 -ErrorAction SilentlyContinue

        if ((Get-Item $logFile -ErrorAction SilentlyContinue).Length -gt 100KB) {
            $lines = Get-Content -Path $logFile -Tail 150 -ErrorAction SilentlyContinue
            Set-Content -Path $logFile -Value $lines -Encoding UTF8 -ErrorAction SilentlyContinue
        }
    } catch { }
}

# --- Konsol Penceresini Gizle ---
try {
    Add-Type -Namespace Win32 -Name NativeWatcher -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
'@ -ErrorAction SilentlyContinue
    $h = [Win32.NativeWatcher]::GetConsoleWindow()
    if ($h -and $h -ne [System.IntPtr]::Zero) {
        [Win32.NativeWatcher]::ShowWindow($h, 0) | Out-Null
    }
} catch { }

# --- Tek Calisma Kontrolu (Single Instance via Mutex) ---
$createdNew = $false
$mutex = New-Object System.Threading.Mutex($true, "Global\ValGrid_Valorant_Watcher_Mutex", [ref]$createdNew)
if (-not $createdNew) {
    Log-Message "Baska bir watch-valorant ornegi zaten calisiyor. Bu ornek sonlandirildi."
    exit 0
}

Log-Message "=== ValGrid Valorant Watcher Baslatildi ==="

function Find-ValGridExe {
    $candidates = @(
        (Join-Path $scriptDir "ValGrid.exe"),
        (Join-Path $scriptDir "publish\ValGrid.exe"),
        (Join-Path $scriptDir "ValGrid\bin\Release\net6.0-windows\ValGrid.exe"),
        "$env:LOCALAPPDATA\Programs\ValGrid\ValGrid.exe",
        "$env:ProgramFiles\ValGrid\ValGrid.exe"
    )
    foreach ($cand in $candidates) {
        if (Test-Path $cand) { return $cand }
    }
    return $null
}

$gameProcs  = @("VALORANT-Win64-Shipping", "VALORANT")
$pollSec    = 4
$launched    = $false
$detectStreak = 0
$absentStreak = 0

while ($true) {
    try {
        $gameRunning = $false
        foreach ($procName in $gameProcs) {
            if ($null -ne (Get-Process -Name $procName -ErrorAction SilentlyContinue)) {
                $gameRunning = $true
                break
            }
        }

        if ($gameRunning) {
            $detectStreak++
            $absentStreak = 0

            if (-not $launched -and $detectStreak -ge 2) {
                Log-Message "Valorant tespit edildi! ValGrid kontrol ediliyor..."

                $valGridRunning = $null -ne (Get-Process -Name "ValGrid" -ErrorAction SilentlyContinue)
                if (-not $valGridRunning) {
                    $valGridExe = Find-ValGridExe
                    if ($valGridExe) {
                        Log-Message "ValGrid baslatiliyor: $valGridExe"
                        Start-Process -FilePath $valGridExe -WorkingDirectory (Split-Path -Parent $valGridExe)
                    } else {
                        Log-Message "ValGrid.exe bulunamadi!"
                    }
                } else {
                    Log-Message "ValGrid zaten calisiyor."
                }

                $launched = $true
            }
        } else {
            $absentStreak++
            $detectStreak = 0

            if ($launched -and $absentStreak -ge 3) {
                Log-Message "Valorant kapandi. Yeni oturum icin bekleniyor."
                $launched = $false
            }
        }
    } catch {
        Log-Message "Hata: $_"
    }

    Start-Sleep -Seconds $pollSec
}
