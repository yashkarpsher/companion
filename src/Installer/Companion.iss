#ifndef AppVersion
  #define AppVersion "0.17.20"
#endif
#ifndef OnlineInstaller
#ifndef PayloadDir
  #error PayloadDir is required
#endif
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif
#ifdef SmokeTest
  #define ProductName "Companion Installer Test"
  #define ProductId "Companion.Installer.Test"
  #define ProductMutex "Local\CompanionInstallerTestV1"
#else
  #define ProductName "Companion"
  #define ProductId "Companion.Yashkarp"
  #define ProductMutex "Local\MajesticDailyCompanionV1"
#endif

[Setup]
AppId={#ProductId}
AppName={#ProductName}
#ifdef OnlineInstaller
AppVersion={code:GetInstallVersion}
AppVerName=Companion
#else
AppVersion={#AppVersion}
#endif
AppPublisher=Яша
AppPublisherURL=https://github.com/yashkarpsher/companion
AppSupportURL=https://github.com/yashkarpsher/companion/issues
AppUpdatesURL=https://github.com/yashkarpsher/companion/releases/latest
DefaultDirName={localappdata}\Programs\Компаньон
DefaultGroupName={#ProductName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableDirPage=no
DisableProgramGroupPage=yes
DisableWelcomePage=no
AllowNoIcons=yes
UsePreviousAppDir=yes
AppMutex={#ProductMutex}
SetupMutex=Local\CompanionInstallerSetup
CloseApplications=no
RestartApplications=no
UninstallDisplayIcon={app}\Majestic Companion.exe
OutputDir={#OutputDir}
#ifdef OnlineInstaller
OutputBaseFilename=Companion-Setup
#else
OutputBaseFilename=Companion-Setup-{#AppVersion}
#endif
SetupIconFile=..\companion.ico
WizardStyle=modern dark
WizardImageFile=wizard.bmp
WizardSmallImageFile=wizard-small.bmp
WizardSizePercent=115
Compression=lzma2
SolidCompression=yes
VersionInfoVersion={#AppVersion}.0
VersionInfoDescription=Установка Companion
VersionInfoProductName=Companion
UninstallDisplayName={#ProductName}
UninstallFilesDir={app}\uninstall
#ifdef SmokeTest
CreateUninstallRegKey=no
#else
CreateUninstallRegKey=yes
#endif

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
russian.WelcomeLabel1=Companion рядом, пока ты в игре
#ifdef OnlineInstaller
russian.WelcomeLabel2=Браузер, таймеры, дела, заметки и цели в одном помощнике.%n%nУстановщик загрузит последнюю версию с GitHub. Выбери папку установки. Записи и настройки сохраняются при переустановке и удалении программы.%n%nДля окон поверх игры включи в самой игре оконный режим или окно без рамки.
#else
russian.WelcomeLabel2=Браузер, таймеры, дела, заметки и цели в одном помощнике.%n%nВыбери папку установки. Записи и настройки сохраняются при переустановке и удалении программы.%n%nДля окон поверх игры включи в самой игре оконный режим или окно без рамки.
#endif
russian.FinishedLabel=Companion установлен.%n%nЧтобы видеть помощник поверх игры, включи в игре оконный режим или окно без рамки.%n%nДля браузера и некоторых мини-игр нужен Microsoft Edge WebView2 Runtime. Если его нет, Companion подскажет, как установить.

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Ярлыки:"

[Files]
#ifdef OnlineInstaller
Source: "{#BootstrapHelper}"; DestName: "CompanionBootstrap.exe"; Flags: dontcopy
Source: "{tmp}\companion-payload\Majestic Companion.exe"; DestDir: "{app}"; Flags: external ignoreversion
Source: "{tmp}\companion-payload\Companion Updater.exe"; DestDir: "{app}"; Flags: external ignoreversion
Source: "{tmp}\companion-payload\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: external ignoreversion
Source: "{tmp}\companion-payload\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: external ignoreversion
Source: "{tmp}\companion-payload\WebView2Loader.dll"; DestDir: "{app}"; Flags: external ignoreversion
Source: "{tmp}\companion-payload\Прочитай меня.txt"; DestDir: "{app}"; Flags: external ignoreversion skipifsourcedoesntexist
#else
Source: "{#PayloadDir}\Majestic Companion.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Companion Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Прочитай меня.txt"; DestDir: "{app}"; Flags: ignoreversion

#endif

[Icons]
#ifdef SmokeTest
Name: "{app}\test-shortcuts\Companion"; Filename: "{app}\Majestic Companion.exe"; WorkingDir: "{app}"
#else
Name: "{group}\{#ProductName}"; Filename: "{app}\Majestic Companion.exe"; WorkingDir: "{app}"; AppUserModelID: "Companion.Yashkarp"
Name: "{autodesktop}\{#ProductName}"; Filename: "{app}\Majestic Companion.exe"; WorkingDir: "{app}"; Tasks: desktopicon; AppUserModelID: "Companion.Yashkarp"

#endif

[Run]
Filename: "{app}\Majestic Companion.exe"; WorkingDir: "{app}"; Description: "Запустить Companion"; Flags: nowait postinstall skipifsilent

[Code]
#ifdef OnlineInstaller
#include "OnlineDownload.iss"
#else
function GetInstallVersion(Param: String): String;
begin Result := '{#AppVersion}'; end;
#endif
var
  ImportPage: TWizardPage;
  ImportFolder: TNewEdit;

procedure BrowseOldFolder(Sender: TObject);
var
  Folder: String;
begin
  Folder := ImportFolder.Text;
  if BrowseForFolder('Выбери старую папку Companion', Folder, False) then
    ImportFolder.Text := Folder;
end;

procedure InitializeWizard;
var
  Instructions, Caption: TNewStaticText;
  Browse: TNewButton;
begin
#ifdef OnlineInstaller
  DownloadPage := CreateDownloadPage('Загрузка Companion', 'Последняя стабильная версия с GitHub', nil);
#endif
  ImportPage := CreateCustomPage(wpSelectDir, 'Перенос записей и настроек', 'Уже пользуешься версией из ZIP?');
  Instructions := TNewStaticText.Create(ImportPage);
  Instructions.Parent := ImportPage.Surface;
  Instructions.SetBounds(0, 0, ImportPage.SurfaceWidth, ScaleY(84));
  Instructions.AutoSize := False;
  Instructions.WordWrap := True;
  Instructions.Caption := 'Для переноса данных выбери старую папку Companion, где находятся программа и папка data. Старые файлы останутся на месте.' + #13#10 + #13#10 + 'Для первой установки или установки поверх прежней версии оставь поле пустым и нажми «Далее».';
  Caption := TNewStaticText.Create(ImportPage);
  Caption.Parent := ImportPage.Surface;
  Caption.SetBounds(0, ScaleY(94), ImportPage.SurfaceWidth, ScaleY(20));
  Caption.Caption := 'Старая папка Companion (необязательно):';
  ImportFolder := TNewEdit.Create(ImportPage);
  ImportFolder.Parent := ImportPage.Surface;
  ImportFolder.SetBounds(0, ScaleY(116), ImportPage.SurfaceWidth - ScaleX(95), ScaleY(24));
  ImportFolder.Text := ExpandConstant('{param:IMPORTFROM|}');
  Browse := TNewButton.Create(ImportPage);
  Browse.Parent := ImportPage.Surface;
  Browse.SetBounds(ImportPage.SurfaceWidth - ScaleX(85), ScaleY(115), ScaleX(85), ScaleY(26));
  Browse.Caption := 'Обзор...';
  Browse.OnClick := @BrowseOldFolder;
end;

function CopyUserData(const Source, Dest: String): Boolean;
var
  Entry: TFindRec;
  FromPath, ToPath: String;
begin
  Result := ForceDirectories(Dest);
  if not Result then Exit;
  if FindFirst(AddBackslash(Source) + '*', Entry) then
  begin
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then
        begin
          if (Entry.Attributes and $400) <> 0 then
          begin
            Result := False;
            Exit;
          end;
          FromPath := AddBackslash(Source) + Entry.Name;
          ToPath := AddBackslash(Dest) + Entry.Name;
          if (Entry.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
            Result := CopyUserData(FromPath, ToPath)
          else
            Result := FileCopy(FromPath, ToPath, True);
          if not Result then Exit;
        end;
      until not FindNext(Entry);
    finally
      FindClose(Entry);
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  OldRoot, OldData, NewData, Stage: String;
  ExistingVersion, SetupVersion: Int64;
begin
  Result := '';
  if GetPackedVersion(ExpandConstant('{app}\Majestic Companion.exe'), ExistingVersion) and StrToVersion(GetInstallVersion(''), SetupVersion) then
  if ComparePackedVersion(ExistingVersion, SetupVersion) > 0 then
  begin
    Result := 'В выбранной папке уже установлена более новая версия Companion. Используй её или выбери другую папку.';
    Exit;
  end;
  OldRoot := Trim(ImportFolder.Text);
  if OldRoot = '' then Exit;
  OldRoot := RemoveBackslashUnlessRoot(ExpandFileName(OldRoot));
  OldData := AddBackslash(OldRoot) + 'data';
  NewData := ExpandConstant('{app}\data');
  if CompareText(OldData, NewData) = 0 then Exit;
  if not FileExists(AddBackslash(OldRoot) + 'Majestic Companion.exe') or not DirExists(OldData) then
  begin
    Result := 'В выбранной старой папке не найдены программа и папка data. Выбери папку Companion или очисти поле переноса.';
    Exit;
  end;
  if Pos(Lowercase(AddBackslash(OldData)), Lowercase(AddBackslash(NewData))) = 1 then
begin
Result := 'Нельзя устанавливать программу внутрь переносимой папки data. Выбери другую папку установки.';
Exit;
end;
  if DirExists(NewData) then
  begin
    Result := 'В папке установки уже есть data. Чтобы сохранить эти записи, очисти поле переноса. Для переноса других данных выбери новую папку установки.';
    Exit;
  end;
  Stage := ExpandConstant('{app}\.companion-import-') + GetDateTimeString('yyyymmddhhnnss', '-', ':');
  if DirExists(Stage) then
  begin
    Result := 'Не удалось подготовить перенос. Повтори установку.';
    Exit;
  end;
  if not CopyUserData(OldData, Stage) then
  begin
    DelTree(Stage, True, True, True);
    Result := 'Не удалось скопировать данные. Закрой старую программу и браузер Companion. Проверь доступ к файлам: папки со ссылками не поддерживаются. Старые данные не изменены.';
    Exit;
  end;
  if not RenameFile(Stage, NewData) then
  begin
    DelTree(Stage, True, True, True);
    Result := 'Не удалось завершить перенос данных. Старые данные не изменены.';
  end;
end;
