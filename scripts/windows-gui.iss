#ifndef AppVersion
  #error AppVersion must be supplied by the packaging script
#endif
#ifndef PackageDir
  #error PackageDir must point to the published application
#endif
#ifndef OutputDir
  #error OutputDir must be supplied
#endif

[Setup]
AppId={{D138DD09-6709-4D55-BA91-FF35F7E3B9D4}
AppName=ZonyLrcTools X
AppVersion={#AppVersion}
AppPublisher=ZonyLrcToolsX-GUI
AppPublisherURL=https://github.com/Lovest20018/ZonyLrcToolsX-GUI
DefaultDirName={localappdata}\Programs\ZonyLrcToolsX
DefaultGroupName=ZonyLrcTools X
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=ZonyLrcTools-Windows-GUI-x64-Setup-{#AppVersion}
SetupIconFile=..\src\ZonyLrcTools.Desktop\Assets\zony.ico
UninstallDisplayIcon={app}\ZonyLrcTools.Desktop.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=ZonyLrcTools.Desktop.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PackageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\ZonyLrcTools X"; Filename: "{app}\ZonyLrcTools.Desktop.exe"
Name: "{autodesktop}\ZonyLrcTools X"; Filename: "{app}\ZonyLrcTools.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ZonyLrcTools.Desktop.exe"; Description: "{cm:LaunchProgram,ZonyLrcTools X}"; Flags: nowait postinstall skipifsilent
