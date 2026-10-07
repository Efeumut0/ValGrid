#define MyAppName "ValGrid"
#define MyAppVersion "1.3.14"
#define MyAppPublisher "ValGrid Team"
#define MyAppURL "https://github.com/Efeumut0/ValGrid"
#define MyAppExeName "ValGrid.exe"
#define MyWatcherExeName "ValGridWatcher.exe"

[Setup]
AppId={{5E285D8B-A9E7-47C1-82CE-71C5681E3871}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableWelcomePage=no
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=ValGrid_Setup_v{#MyAppVersion}
SetupIconFile=ValGrid\Assets\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
tr.CreateDesktopIcon=MasaÃƒÆ’Ã‚Â¼stÃƒÆ’Ã‚Â¼ kÃƒâ€Ã‚Â±sayolu oluÃƒâ€¦Ã…Â¸tur
tr.CreateStartMenuIcon=BaÃƒâ€¦Ã…Â¸lat menÃƒÆ’Ã‚Â¼sÃƒÆ’Ã‚Â¼ kÃƒâ€Ã‚Â±sayolu oluÃƒâ€¦Ã…Â¸tur
tr.AutoStartWithWindows=Windows aÃƒÆ’Ã‚Â§Ãƒâ€Ã‚Â±lÃƒâ€Ã‚Â±Ãƒâ€¦Ã…Â¸Ãƒâ€Ã‚Â±nda ValGrid'i baÃƒâ€¦Ã…Â¸lat
tr.InstallWatcher=Valorant aÃƒÆ’Ã‚Â§Ãƒâ€Ã‚Â±ldÃƒâ€Ã‚Â±Ãƒâ€Ã…Â¸Ãƒâ€Ã‚Â±nda ValGrid'i otomatik baÃƒâ€¦Ã…Â¸lat (Arka plan servisi)
tr.DotNet6Missing=ValGrid uygulamasÃƒâ€Ã‚Â±nÃƒâ€Ã‚Â±n ÃƒÆ’Ã‚Â§alÃƒâ€Ã‚Â±Ãƒâ€¦Ã…Â¸abilmesi iÃƒÆ’Ã‚Â§in Microsoft .NET 6 Desktop Runtime gereklidir. Otomatik indirilip kurulsun mu?
tr.DotNet6Downloading=.NET 6 Desktop Runtime indiriliyor, lÃƒÆ’Ã‚Â¼tfen bekleyin...
tr.DotNet6Installing=.NET 6 Desktop Runtime kuruluyor...
tr.DotNet6Failed=ValGrid iÃƒÆ’Ã‚Â§in .NET 6 Desktop Runtime indirilemedi veya kurulamadÃƒâ€Ã‚Â±. LÃƒÆ’Ã‚Â¼tfen Microsoft sitesinden .NET 6 Desktop Runtime paketini kurun.
tr.UninstallCleanData=ValGrid maÃƒÆ’Ã‚Â§ geÃƒÆ’Ã‚Â§miÃƒâ€¦Ã…Â¸i, kariyer verileri ve ÃƒÆ’Ã‚Â¶zel notlarÃƒâ€Ã‚Â±nÃƒâ€Ã‚Â±z da silinsin mi?

en.CreateDesktopIcon=Create desktop shortcut
en.CreateStartMenuIcon=Create Start Menu shortcut
en.AutoStartWithWindows=Start ValGrid when Windows starts
en.InstallWatcher=Automatically start ValGrid when Valorant launches (Background service)
en.DotNet6Missing=Microsoft .NET 6 Desktop Runtime is required to run ValGrid. Would you like to download and install it automatically?
en.DotNet6Downloading=Downloading .NET 6 Desktop Runtime, please wait...
en.DotNet6Installing=Installing .NET 6 Desktop Runtime...
en.DotNet6Failed=Failed to download or install .NET 6 Desktop Runtime for ValGrid. Please install .NET 6 Desktop Runtime manually.
en.UninstallCleanData=Do you also want to delete ValGrid match history, career records, and user notes?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "startmenuicon"; Description: "{cm:CreateStartMenuIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "watcher"; Description: "{cm:InstallWatcher}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "autostart"; Description: "{cm:AutoStartWithWindows}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ValGridWatcher"; ValueData: """{app}\{#MyWatcherExeName}"""; Flags: uninsdeletevalue; Tasks: watcher

[Run]
Filename: "{app}\{#MyWatcherExeName}"; Description: "{cm:InstallWatcher}"; Flags: nowait postinstall skipifsilent; Tasks: watcher
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet6DesktopInstalled: Boolean;
var
  KeyPath: string;
  FindRec: TFindRec;
begin
  Result := False;

  // 1. Registry 64-bit
  KeyPath := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\fx\Microsoft.WindowsDesktop.App';
  if RegKeyExists(HKLM64, KeyPath) then
  begin
    Result := True;
    Exit;
  end;

  // 2. Registry WOW64
  KeyPath := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost\fx\Microsoft.WindowsDesktop.App';
  if RegKeyExists(HKLM, KeyPath) then
  begin
    Result := True;
    Exit;
  end;

  // 3. Shared Desktop App folder 64-bit
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\6.*'), FindRec) then
  begin
    FindClose(FindRec);
    Result := True;
    Exit;
  end;

  // 4. Shared Desktop App folder 32-bit / standard
  if FindFirst(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App\6.*'), FindRec) then
  begin
    FindClose(FindRec);
    Result := True;
    Exit;
  end;
end;

function DownloadAndInstallDotNet6: Boolean;
var
  DownloadUrl, TempFile: string;
  ResultCode: Integer;
  PowerShellCmd: string;
begin
  Result := False;
  DownloadUrl := 'https://aka.ms/dotnet/6.0/windowsdesktop-runtime-win-x64.exe';
  TempFile := ExpandConstant('{tmp}\windowsdesktop-runtime-6.0-x64.exe');

  PowerShellCmd := Format('-NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; (New-Object System.Net.WebClient).DownloadFile(''%s'', ''%s'')"', [DownloadUrl, TempFile]);

  WizardForm.StatusLabel.Caption := CustomMessage('DotNet6Downloading');
  if Exec('powershell.exe', PowerShellCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) and FileExists(TempFile) then
  begin
    WizardForm.StatusLabel.Caption := CustomMessage('DotNet6Installing');
    if Exec(TempFile, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
    begin
      Result := True;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not IsDotNet6DesktopInstalled then
  begin
    if MsgBox(CustomMessage('DotNet6Missing'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not DownloadAndInstallDotNet6 then
      begin
        Result := CustomMessage('DotNet6Failed');
      end;
    end
    else
    begin
      Result := CustomMessage('DotNet6Missing');
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  LangCode: string;
begin
  if CurStep = ssPostInstall then
  begin
    if ActiveLanguage = 'tr' then
      LangCode := 'tr'
    else
      LangCode := 'en';

    ForceDirectories(ExpandConstant('{localappdata}\ValGrid'));
    SaveStringToFile(ExpandConstant('{localappdata}\ValGrid\setup_lang.txt'), LangCode, False);
    SaveStringToFile(ExpandConstant('{app}\setup_lang.txt'), LangCode, False);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('taskkill.exe', '/F /IM ValGridWatcher.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('taskkill.exe', '/F /IM ValGrid.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

    if MsgBox(CustomMessage('UninstallCleanData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    begin
      DelTree(ExpandConstant('{localappdata}\ValGrid'), True, True, True);
    end;
  end;
end;








