# Build-Installer.ps1
# ValGrid Otomatik Installer Derleyicisi

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "      ValGrid Inno Setup Derleyici       " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Sürüm bilgisini ValGrid.csproj içerisinden oku
$csprojPath = Join-Path $root "ValGrid\ValGrid.csproj"
[xml]$csprojXml = Get-Content $csprojPath
$version = $csprojXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

if (-not $version) {
    $version = "1.3.12"
}

Write-Host "Bulunan Sürüm: $version" -ForegroundColor Green

# 2. ValGrid.iss dosyasındaki MyAppVersion'ı doğrula / senkronize et
$issPath = Join-Path $root "ValGrid.iss"
$issContent = Get-Content $issPath -Raw
if ($issContent -match '#define MyAppVersion "([^"]+)"') {
    $issContent = $issContent -replace '#define MyAppVersion "[^"]+"', "#define MyAppVersion `"$version`""
    Set-Content -Path $issPath -Value $issContent -Encoding UTF8
}

# 3. Klasörleri hazırla
$publishDir = Join-Path $root "publish"
$distDir = Join-Path $root "dist"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }

# 4. Projeyi Release modunda yayınla (win-x64, framework-dependent)
Write-Host "`n[1/3] ValGrid Release modunda derleniyor ve yayınlanıyor..." -ForegroundColor Yellow
dotnet publish "$csprojPath" -c Release -r win-x64 --self-contained false -o "$publishDir"
if ($LASTEXITCODE -ne 0) { throw "ValGrid dotnet publish başarısız oldu!" }

# 4.1 ValGridWatcher (Valorant Algılayıcı Servisi) Release modunda yayınla
$watcherCsprojPath = Join-Path $root "Watcher\ValGridWatcher.csproj"
if (Test-Path $watcherCsprojPath) {
    Write-Host "`n[1.1/3] ValGridWatcher servisi Release modunda derleniyor ve yayınlanıyor..." -ForegroundColor Yellow
    dotnet publish "$watcherCsprojPath" -c Release -r win-x64 --self-contained false -o "$publishDir"
    if ($LASTEXITCODE -ne 0) { throw "ValGridWatcher dotnet publish başarısız oldu!" }
}

# 5. Inno Setup Derleyicisini (ISCC.exe) bul
Write-Host "`n[2/3] Inno Setup derleyicisi aranıyor..." -ForegroundColor Yellow
$isccCandidates = @(
    "iscc",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 5\ISCC.exe"
)

$isccPath = $null
foreach ($candidate in $isccCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $isccPath = $candidate
        break
    }
    if (Test-Path $candidate) {
        $isccPath = $candidate
        break
    }
}

if (-not $isccPath) {
    throw "Inno Setup derleyicisi (ISCC.exe) bulunamadı! Lütfen Inno Setup 6 kurun."
}

Write-Host "Kullanılan Derleyici: $isccPath" -ForegroundColor DarkCyan

# 6. Inno Setup ile Setup.exe derle
Write-Host "`n[3/3] Kurulum paketi (ValGrid_Setup_v$version.exe) oluşturuluyor..." -ForegroundColor Yellow
& "$isccPath" "/Q" "$issPath"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup derlemesi başarısız oldu!" }

$setupOutput = Join-Path $distDir "ValGrid_Setup_v$version.exe"
if (Test-Path $setupOutput) {
    $item = Get-Item $setupOutput
    $sizeMB = [math]::Round($item.Length / 1MB, 2)
    
    # Proje ana dizinine de bir kopya bırak
    $rootCopy = Join-Path $root "ValGrid_Setup_v$version.exe"
    Copy-Item $setupOutput -Destination $rootCopy -Force
    
    Write-Host "`n=========================================" -ForegroundColor Green
    Write-Host " BAŞARILI! Kurulum paketi hazırlandı." -ForegroundColor Green
    Write-Host " Dosya: ValGrid_Setup_v$version.exe ($sizeMB MB)" -ForegroundColor Green
    Write-Host " Konum: $setupOutput" -ForegroundColor Green
    Write-Host " Kök Dizin: $rootCopy" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
} else {
    throw "Kurulum dosyası üretilemedi: $setupOutput"
}
