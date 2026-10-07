@echo off
cd /d "%~dp0"
if exist "ValGrid.exe" (
    start "" "ValGrid.exe"
    exit /b
)
if exist "%~dp0ValGrid\bin\Release\net6.0-windows\ValGrid.exe" (
    cd /d "%~dp0ValGrid\bin\Release\net6.0-windows"
    start "" "ValGrid.exe"
    exit /b
)
if exist "%~dp0publish\ValGrid.exe" (
    cd /d "%~dp0publish"
    start "" "ValGrid.exe"
    exit /b
)
echo ValGrid.exe bulunamadi, derlenip baslatiliyor...
dotnet run --project "%~dp0ValGrid\ValGrid.csproj" -c Release
