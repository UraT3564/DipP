[Setup]
AppName=Управление отчетностью КДЦ
AppVersion=1.0
DefaultDirName={pf}\Управление отчетностью КДЦ
DefaultGroupName=Управление отчетностью КДЦ
OutputDir=Deploy
OutputBaseFilename=Управление_отчетностью_КДЦ_Setup
Compression=lzma
SolidCompression=yes

[Files]
; Основной исполняемый файл
Source: "bin\Release\ДипП.exe"; DestDir: "{app}"
; Библиотеки
Source: "bin\Release\Spire.Doc.dll"; DestDir: "{app}"
Source: "bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"
; Папка Res и всё её содержимое
Source: "bin\Release\Res\*"; DestDir: "{app}\Res"; Flags: recursesubdirs

[Icons]
Name: "{group}\Управление отчетностью КДЦ"; Filename: "{app}\ДипП.exe"
Name: "{group}\Удалить"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Управление отчетностью СДК"; Filename: "{app}\ДипП.exe"

[Run]
Filename: "{app}\ДипП.exe"; Description: "Запустить приложение"; Flags: postinstall nowait skipifsilent