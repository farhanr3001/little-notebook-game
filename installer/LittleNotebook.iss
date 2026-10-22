#ifndef AppVersion
 #define AppVersion "0.5.1"
#endif
#ifndef PayloadDir
 #define PayloadDir "..\releases\payload"
#endif
[Setup]
AppId={code:ApplicationId}
AppName=Little Notebook
AppVersion={#AppVersion}
AppPublisher=Farhan
AppPublisherURL=https://github.com/farhanr3001/little-notebook-game
AppUpdatesURL=https://github.com/farhanr3001/little-notebook-game/releases/latest
DefaultDirName={localappdata}\Programs\Little Notebook
DefaultGroupName=Little Notebook
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousLanguage=no
OutputDir=..\releases
OutputBaseFilename=LittleNotebook-Setup-{#AppVersion}
SetupIconFile=..\assets\LittleNotebook.ico
UninstallDisplayIcon={app}\LittleNotebook.exe
WizardStyle=modern
WizardBackColor=$00E9F8FF
WizardSmallImageBackColor=$00E9F8FF
WizardImageBackColor=$00E9F8FF
Compression=lzma2/fast
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
AppMutex=Local\LittleNotebook-5C35749C-14DE-49BC-AED7-E462359A73A4
UninstallLogMode=append
MinVersion=10.0
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked; Check: NotTestInstall
[Languages]
Name: english; MessagesFile: "compiler:Default.isl"
Name: arabic; MessagesFile: "compiler:Languages\Arabic.isl"
[Files]
Source: "..\tools\install-build\prerequisites\dotnet48.exe"; Flags: dontcopy
Source: "..\tools\install-build\prerequisites\vc_redist.x64.exe"; Flags: dontcopy
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "__pycache__\*,*.pyc,installed.ini"
[Icons]
Name: "{userprograms}\Little Notebook"; Filename: "{app}\LittleNotebook.exe"; WorkingDir: "{app}"; AppUserModelID: "Farhan.LittleNotebook"; Check: NotTestInstall
Name: "{userdesktop}\Little Notebook"; Filename: "{app}\LittleNotebook.exe"; WorkingDir: "{app}"; AppUserModelID: "Farhan.LittleNotebook"; Tasks: desktopicon; Check: NotTestInstall
[Registry]
Root: HKCU; Subkey: "Software\LittleNotebook"; ValueType: string; ValueName: "InstallDirectory"; ValueData: "{app}"; Flags: uninsdeletekey; Check: NotTestInstall
[Run]
Filename: "{app}\LittleNotebook.exe"; Description: "Open Little Notebook"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser; Check: CanLaunchGame
[UninstallRun]
Filename: "{app}\LittleNotebook.Update.exe"; Parameters: "--uninstall-clean ""{app}"""; Flags: runhidden waituntilterminated; RunOnceId: "CleanManagedLittleNotebookFiles"
[UninstallDelete]
Type: filesandordirs; Name: "{code:DataDirectory}"; Check: OwnedInstall
Type: filesandordirs; Name: "{app}\data"; Check: OwnedInstall
Type: filesandordirs; Name: "{app}\assets"; Check: OwnedInstall
Type: filesandordirs; Name: "{app}\content"; Check: OwnedInstall
Type: filesandordirs; Name: "{app}\speech-runtime"; Check: OwnedInstall
Type: files; Name: "{app}\payload-manifest.json"; Check: OwnedInstall
Type: files; Name: "{app}\installed.ini"; Check: OwnedInstall
Type: dirifempty; Name: "{app}"
[Code]
const ProductId = '5C35749C-14DE-49BC-AED7-E462359A73A4';
var PrerequisiteRestart: Boolean;
function TestInstall: Boolean;
begin Result := ExpandConstant('{param:TESTINSTALL|0}') = '1'; end;
function NotTestInstall: Boolean;
begin Result := not TestInstall; end;
function CanLaunchGame: Boolean;
begin Result := NotTestInstall and (not PrerequisiteRestart); end;
function DotNetReady: Boolean;
var ReleaseValue: Cardinal;
begin Result := RegQueryDWordValue(HKLM64,'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full','Release',ReleaseValue) and (ReleaseValue >= 528040); end;
function VisualCppReady: Boolean;
var InstalledValue, MajorValue, MinorValue: Cardinal;
begin
 Result := RegQueryDWordValue(HKLM64,'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64','Installed',InstalledValue) and (InstalledValue=1) and RegQueryDWordValue(HKLM64,'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64','Major',MajorValue) and RegQueryDWordValue(HKLM64,'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64','Minor',MinorValue) and ((MajorValue>14) or ((MajorValue=14) and (MinorValue>=40)));
end;
function InstallPrerequisite(FileName, Arguments, DisplayName: String): String;
var ExitCode: Integer;
begin
 Result := ''; ExtractTemporaryFile(FileName);
 Log('Installing missing prerequisite: '+DisplayName);
 if not ShellExec('runas',ExpandConstant('{tmp}\')+FileName,Arguments,'',SW_HIDE,ewWaitUntilTerminated,ExitCode) then Result := 'Could not install '+DisplayName+'. Allow the Microsoft installer when Windows asks, then retry.'
 else if (ExitCode=3010) or (ExitCode=1641) then PrerequisiteRestart := True
 else if (ExitCode<>0) and (ExitCode<>1638) then Result := DisplayName+' installation failed (code '+IntToStr(ExitCode)+'). Restart Windows and try again.';
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
 Result := '';
 if DotNetReady then Log('Prerequisite OK: .NET Framework 4.8 or newer') else begin
  Result := InstallPrerequisite('dotnet48.exe','/q /norestart','.NET Framework 4.8');
  if (Result='') and (not PrerequisiteRestart) and (not DotNetReady) then Result := '.NET Framework installation could not be verified.';
 end;
 if (Result='') then begin
  if VisualCppReady then Log('Prerequisite OK: Microsoft Visual C++ x64') else begin
   Result := InstallPrerequisite('vc_redist.x64.exe','/install /quiet /norestart','Microsoft Visual C++ x64');
   if (Result='') and (not PrerequisiteRestart) and (not VisualCppReady) then Result := 'Visual C++ runtime installation could not be verified.';
  end;
 end;
end;
function NeedRestart: Boolean;
begin Result := PrerequisiteRestart; end;
function ApplicationId(Param: String): String;
begin if TestInstall then Result := ProductId + '-Test' else Result := ProductId; end;
function OwnedInstall: Boolean;
begin
 { UninstallDelete checks are recorded during installation, before ssPostInstall creates the marker. }
 Result := (not IsUninstaller) or (GetIniString('Install','ProductId','',ExpandConstant('{app}\installed.ini')) = ProductId);
end;
function InitializeUninstall: Boolean;
begin
 Result := GetIniString('Install','ProductId','',ExpandConstant('{app}\installed.ini')) = ProductId;
 if not Result then Log('Little Notebook installation ownership could not be verified.');
end;
function DataDirectory(Param: String): String;
var Saved, Normal, Isolated: String;
begin
 Normal := ExpandConstant('{localappdata}\LittleNotebook');
 Isolated := ExpandConstant('{app}\user-data-test');
 Saved := GetIniString('Install','DataDirectory','',ExpandConstant('{app}\installed.ini'));
 if CompareText(Saved,Isolated)=0 then Result := Isolated
 else if TestInstall then Result := Isolated else Result := Normal;
end;
procedure CopyData(Source, Dest: String);
var Find: TFindRec;
begin
 ForceDirectories(Dest);
 if FindFirst(AddBackslash(Source)+'*',Find) then begin
  try repeat
   if (Find.Name<>'.') and (Find.Name<>'..') and (Find.Name<>'updates') and (Find.Name<>'logs') then begin
    if Find.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then CopyData(AddBackslash(Source)+Find.Name,AddBackslash(Dest)+Find.Name)
    else FileCopy(AddBackslash(Source)+Find.Name,AddBackslash(Dest)+Find.Name,True);
   end;
  until not FindNext(Find); finally FindClose(Find); end;
 end;
end;
procedure CurStepChanged(CurStep: TSetupStep);
var Data, Legacy, Marker: String;
begin
 if CurStep=ssPostInstall then begin
  Marker := ExpandConstant('{app}\installed.ini'); Data := DataDirectory('');
  ForceDirectories(Data);
  Legacy := ExpandConstant('{param:MIGRATEFROM|}');
  if (Legacy<>'') and (CompareText(Legacy,Data)<>0) and FileExists(AddBackslash(Legacy)+'profile.json') then CopyData(Legacy,Data);
  SetIniString('Install','ProductId',ProductId,Marker);
  SetIniString('Install','Version','{#AppVersion}',Marker);
  SetIniString('Install','DataDirectory',Data,Marker);
 end;
end;
