Set WshShell = CreateObject("WScript.Shell")
scriptPath = "C:\Users\Efe umut\Desktop\valo skin detecktor\ValGrid\watch-valorant.ps1"
cmd = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File """ & scriptPath & """"
WshShell.Run cmd, 0, False
