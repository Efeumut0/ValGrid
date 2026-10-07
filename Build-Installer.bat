@echo off
cd /d "%~dp0"
echo ==============================================
echo    ValGrid Kurulum Paketi Derleniyor...
echo ==============================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1"
echo.
pause
