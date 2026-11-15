; Inno Setup script for Possum Operational ScreenSaver (POSS)
;
; Build (from the repository root):
;   dotnet publish -c Release -o dist
;   iscc installer\PocketPossum.iss
; Output: installer\Output\PocketPossum-Setup-<version>.exe
;
; Override the version with:  iscc installer\PocketPossum.iss /DAppVersion=1.2.3

#ifndef AppVersion
  #define AppVersion "1.5.0"
#endif
#define AppName      "Possum Operational ScreenSaver (POSS)"
#define AppPublisher "TPandaGames"
#define AppUrl       "https://github.com/TPandaGames/possum-live-wallpaper"

[Setup]
; Unique id for this app (do not change between releases, it is how upgrades are recognised)
AppId={{A8B23774-3227-46B7-BCB5-3B765B80A947}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}

; The .scr must live in System32 to appear in the Screen Saver dialog, so installing needs admin.
PrivilegesRequired=admin
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DefaultDirName={autopf}\Possum Operational ScreenSaver
DefaultGroupName=Possum Operational ScreenSaver
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\PocketPossum.exe

OutputDir=Output
OutputBaseFilename=PocketPossum-Setup-{#AppVersion}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "trayautostart"; Description: "Run in the system tray when Windows starts (starts the screensaver after your idle time)"; GroupDescription: "Startup:"

[Files]
; The tray app / settings (also used by the tray to launch the screensaver)
Source: "..\dist\PocketPossum.exe"; DestDir: "{app}"; Flags: ignoreversion
; The screensaver itself: a renamed copy of the same program, in System32 so Windows lists it
Source: "..\dist\PocketPossum.scr"; DestDir: "{sys}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Pocket Possum (run in tray)"; Filename: "{app}\PocketPossum.exe"; Parameters: "/t"
Name: "{group}\Pocket Possum Settings"; Filename: "{app}\PocketPossum.exe"; Parameters: "/c"
Name: "{group}\Uninstall Possum Operational ScreenSaver"; Filename: "{uninstallexe}"

[Registry]
; Optional tray autostart for all users
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PocketPossum"; \
  ValueData: """{app}\PocketPossum.exe"" /t"; Flags: uninsdeletevalue; Tasks: trayautostart

[Run]
; Prerequisites first (downloaded in PrepareToInstall only if missing)
Filename: "{tmp}\windowsdesktop-runtime-win-x64.exe"; Parameters: "/install /quiet /norestart"; \
  StatusMsg: "Installing the .NET 10 Desktop Runtime..."; Check: RunDotNetInstaller; Flags: waituntilterminated
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
  StatusMsg: "Installing the WebView2 Runtime..."; Check: RunWebView2Installer; Flags: waituntilterminated

; Offered on the final page
Filename: "{app}\PocketPossum.exe"; Parameters: "/t"; Description: "Start Pocket Possum in the system tray now"; \
  Flags: nowait postinstall skipifsilent runasoriginaluser
Filename: "{sys}\rundll32.exe"; Parameters: "desk.cpl,InstallScreenSaver {sys}\PocketPossum.scr"; \
  Description: "Choose Pocket Possum as my screen saver (opens Screen Saver Settings)"; \
  Flags: nowait postinstall skipifsilent runasoriginaluser unchecked

[Code]
const
  DotNetUrl    = 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe';
  DotNetFile   = 'windowsdesktop-runtime-win-x64.exe';
  WebView2Url  = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';
  WebView2File = 'MicrosoftEdgeWebview2Setup.exe';
  WebView2Guid = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

var
  NeedDotNet: Boolean;
  NeedWebView2: Boolean;
  DownloadPage: TDownloadWizardPage;

{ --- prerequisite detection --- }

function DotNetDesktopPresent: Boolean;
var
  FindRec: TFindRec;
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;

  { Default install location: ...\dotnet\shared\Microsoft.WindowsDesktop.App\10.x.y }
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;

  { Registry fallback (written by the standalone runtime installers) }
  if not Result then
    if RegGetValueNames(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Names) then
      for I := 0 to GetArrayLength(Names) - 1 do
        if Copy(Names[I], 1, 3) = '10.' then
          Result := True;
end;

function WebView2Present: Boolean;
var
  V: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2Guid, 'pv', V) then
    Result := (V <> '') and (V <> '0.0.0.0');
  if not Result then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\' + WebView2Guid, 'pv', V) then
      Result := (V <> '') and (V <> '0.0.0.0');
end;

{ --- [Run] checks --- }

function RunDotNetInstaller: Boolean;
begin
  Result := NeedDotNet and FileExists(ExpandConstant('{tmp}\' + DotNetFile));
end;

function RunWebView2Installer: Boolean;
begin
  Result := NeedWebView2 and FileExists(ExpandConstant('{tmp}\' + WebView2File));
end;

{ --- helpers --- }

procedure StopRunningCopies;
var
  ResultCode: Integer;
begin
  { Free the files before overwriting/removing them (tray app and any running screensaver) }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM PocketPossum.exe /IM PocketPossum.scr', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

{ --- wizard --- }

procedure InitializeWizard;
begin
  NeedDotNet := not DotNetDesktopPresent;
  NeedWebView2 := not WebView2Present;
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := '';
  if MemoDirInfo <> '' then S := S + MemoDirInfo + NewLine + NewLine;
  if MemoGroupInfo <> '' then S := S + MemoGroupInfo + NewLine + NewLine;
  if MemoTasksInfo <> '' then S := S + MemoTasksInfo + NewLine + NewLine;

  if NeedDotNet or NeedWebView2 then
  begin
    S := S + 'Required components that are missing and will be downloaded from Microsoft:' + NewLine;
    if NeedDotNet then S := S + Space + '.NET 10 Desktop Runtime (x64)' + NewLine;
    if NeedWebView2 then S := S + Space + 'WebView2 Runtime' + NewLine;
  end;
  Result := S;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  StopRunningCopies;

  if not (NeedDotNet or NeedWebView2) then Exit;

  try
    if WizardSilent then
    begin
      if NeedDotNet then DownloadTemporaryFile(DotNetUrl, DotNetFile, '', nil);
      if NeedWebView2 then DownloadTemporaryFile(WebView2Url, WebView2File, '', nil);
    end
    else
    begin
      DownloadPage.Clear;
      if NeedDotNet then DownloadPage.Add(DotNetUrl, DotNetFile, '');
      if NeedWebView2 then DownloadPage.Add(WebView2Url, WebView2File, '');
      DownloadPage.Show;
      try
        DownloadPage.Download;
      finally
        DownloadPage.Hide;
      end;
    end;
  except
    if (not WizardSilent) and DownloadPage.AbortedByUser then
      Result := 'The download of a required component was cancelled.'
    else
      Result := 'Could not download a required component (' + GetExceptionMessage + '). ' +
                'Check your internet connection, or install the .NET 10 Desktop Runtime and the WebView2 ' +
                'Runtime manually, then run this setup again.';
  end;
end;

{ --- uninstall --- }

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Current: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    StopRunningCopies;

    { Per-user startup entry that the app's own Settings dialog may have created }
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PocketPossum');

    { If this was the active screensaver, clear it so Windows doesn't point at a missing file }
    if RegQueryStringValue(HKCU, 'Control Panel\Desktop', 'SCRNSAVE.EXE', Current) then
      if CompareText(ExtractFileName(Current), 'PocketPossum.scr') = 0 then
        RegDeleteValue(HKCU, 'Control Panel\Desktop', 'SCRNSAVE.EXE');
  end;
end;
