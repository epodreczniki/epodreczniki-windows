#define Nazwa "ePodr�czniki"

#ifndef Wersja
#define Wersja "1.0.0.6"
#endif


#define Producent "Pozna�skie Centrum Superkomputerowo-Sieciowe"
#define ProducentShort "PCSS"
#define ProducentUrl "http://www.man.poznan.pl/"
#define WsparcieTelefon "+48 61 XXX XX XX"
#define WsparcieUrl "http://www.man.poznan.pl/"
#define UpdateUrl "http://www.man.poznan.pl/"
#define MyAppExeName "ePodrecznikiDesktop.exe"

[Setup]
AllowCancelDuringInstall=yes
AllowNetworkDrive=yes
AllowNoIcons=yes
AllowUNCPath=yes
AppComments=ePodr�czniki
AppContact={#WsparcieUrl}
AppCopyright=Copyright (C) 2012 {#Producent}
AppId={{E9035AB4-EEEE-4B93-90CC-0DD77646C2CF}
AppName={#Nazwa}
AppPublisher={#Producent}
AppPublisherURL={#ProducentUrl}
AppSupportPhone={#WsparcieTelefon}
AppSupportURL={#WsparcieUrl}
AppUpdatesURL={#UpdateUrl}
AppVerName={cm:NameAndVersion}
AppVersion={#Wersja}
ArchitecturesInstallIn64BitMode=x64
BackColor=clBlack
BackColor2=clBlack
BackSolid=yes
BackColorDirection=lefttoright
;mozna dopisa� /ultra b�dzie bardziej pakowa�
Compression=lzma2
DefaultDirName={pf}\{#Nazwa}
DefaultGroupName={#Nazwa}
DirExistsWarning=auto
FlatComponentsList=no
OutputBaseFilename=EP-{#Wersja}
SetupIconFile=icon.ico
SolidCompression=yes
ShowLanguageDialog=no
PrivilegesRequired=none
VersionInfoCompany=PCSS
VersionInfoVersion={#Wersja}
WizardImageFile=BigImage.bmp
WizardSmallImageFile=WizardSmallImageFile.bmp
WizardImageStretch=yes
SlicesPerDisk=2
DiskSliceSize=2100000000
DiskSpanning=no

[CustomMessages]
NameAndVersion={#Nazwa} (wersja {#Wersja})
UninstallProgram=Odinstaluj
[Languages]
Name: polish; MessagesFile:.\Polish.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
; .NET
Source: "msi\dotNetFx40_Full_x86_x64.exe"; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFramework; Check: FrameworkIsNotInstalled
; IIS Express 
Source: "msi\iisexpress_1_11_x86_en-US.msi"; DestDir: {tmp}; Flags: deleteafterinstall ignoreversion;  AfterInstall: InstallIIS

;nazwa pliku uruchomieniowego
Source: install\ePodrecznikiDesktop.exe; DestDir: {app}; Flags: ignoreversion
;katalog z plikami do instalacji
Source: install\*; DestDir: {app}; Flags: ignoreversion;
;Content
Source: content\*; DestDir: {sd}\ProgramData\ePodr�czniki; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: users-modify


[InstallDelete]
Type: files; Name: "{group}"

[Icons]
Name: "{group}\{#Nazwa}"; Filename: {app}\{#MyAppExeName}
Name: {group}\{cm:UninstallProgram}; Filename: {uninstallexe}
Name: {commondesktop}\{#Nazwa}; Filename: {app}\{#MyAppExeName}; Tasks: desktopicon


[Code]
function FrameworkIsNotInstalled: Boolean;
begin
  Result :=  not RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full');
end;

procedure InstallFramework;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Instalowanie �rodowiska .NET 4.0...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not Exec(ExpandConstant('{tmp}\dotNetFx40_Full_x86_x64.exe'), '/q /noreboot', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('Instalacja �rodowiska .NET nieudana, kod b��du: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;


procedure InstallIIS;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Instalowanie us�ugi IIS Express...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not Exec('msiexec.exe', ExpandConstant('/i {tmp}\iisexpress_1_11_x86_en-US.msi /q'), '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('Instalacja IIS Express nieudana, kod b��du: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;


[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Uruchom aplikacj� ePodr�czniki"; Flags: postinstall nowait skipifsilent unchecked
