[Setup]
AppName=KADAYA
AppVersion=1.0
AppPublisher=KyZer-HADOC
DefaultDirName={autopf}\KADAYA
DefaultGroupName=KADAYA
OutputDir=output
OutputBaseFilename=setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName=KADAYA

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\KADAYA"; Filename: "{app}\Kadaya.exe"
Name: "{autodesktop}\KADAYA"; Filename: "{app}\Kadaya.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Kadaya.exe"; Description: "Launch KADAYA"; Flags: nowait postinstall skipifsilent
