#ifdef UNLOCKED
  #define AppTitle "KADAYA (All Unlocked)"
  #define AppFolder "KADAYA Unlocked"
  #define SetupName "setup-unlocked"
#else
  #define AppTitle "KADAYA"
  #define AppFolder "KADAYA"
  #define SetupName "setup"
#endif

[Setup]
AppName={#AppTitle}
AppVersion=1.0
AppPublisher=KyZer-HADOC
DefaultDirName={autopf}\{#AppFolder}
DefaultGroupName={#AppTitle}
OutputDir=output
OutputBaseFilename={#SetupName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=Content\UI\kadaya_icon.ico
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppTitle}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#AppTitle}"; Filename: "{app}\Kadaya.exe"
Name: "{autodesktop}\{#AppTitle}"; Filename: "{app}\Kadaya.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Kadaya.exe"; Description: "Launch KADAYA"; Flags: nowait postinstall skipifsilent
