# Rebuild & Restart ValGrid
Write-Host "1. Closing ValGrid process..." -ForegroundColor Cyan
taskkill /F /IM ValGrid.exe /T 2>$null
schtasks /end /tn "ValGridRunner" 2>$null
schtasks /end /tn "NOWTRunner" 2>$null
do {
    Start-Sleep -Milliseconds 400
} while (Get-Process -Name "ValGrid" -ErrorAction SilentlyContinue)
Start-Sleep -Milliseconds 600

Write-Host "2. Building in Release mode..." -ForegroundColor Cyan
dotnet build ValGrid.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "3. Reopening ValGrid..." -ForegroundColor Cyan
schtasks /run /tn "ValGridRunner" 2>$null
Start-Sleep -Milliseconds 1500
$p = Get-Process -Name "ValGrid" -ErrorAction SilentlyContinue | Select-Object -Last 1
if (-not $p) {
    $exePath = "C:\Users\Efe umut\Desktop\valo skin detecktor\ValGrid\ValGrid\bin\Release\net6.0-windows\ValGrid.exe"
    if (Test-Path $exePath) {
        Start-Process "explorer.exe" -ArgumentList "`"$exePath`"" -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

$p = Get-Process -Name "ValGrid" -ErrorAction SilentlyContinue | Select-Object -Last 1
if ($p) {
    Write-Host "SUCCESS: ValGrid running (PID: $($p.Id))" -ForegroundColor Green
} else {
    Write-Warning "ValGrid process did not start yet."
}
