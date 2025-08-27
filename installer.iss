#define MyAppName  "OQSDrug2"
#define MyAppExe   "OQSDrug.exe"

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{8B6E2B54-9D83-4E07-9E1F-6D6B6C1B3C11}}
AppName={#MyAppName}
AppVersion={#AppVersion}
DefaultDirName={pf32}\YUKI_ENT_CLINIC\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename={#MyAppName}_v{#AppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\OQSDrug.exe

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "OQSDrug\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成"; GroupDescription: "ショートカット:"; Flags: checkedonce

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "OQSDrug を起動する"; Flags: nowait postinstall skipifsilent
