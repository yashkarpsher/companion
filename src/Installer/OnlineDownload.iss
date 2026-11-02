var
  DownloadPage: TDownloadWizardPage;
  DownloadedVersion: String;
  DownloadReady: Boolean;

function GetInstallVersion(Param: String): String;
begin
  Result := DownloadedVersion;
  if Result = '' then Result := '0.0.0';
end;

function RunBootstrap(const Parameters, Report: String): Boolean;
var
  ExitCode: Integer;
begin
  DeleteFile(Report);
  Result := Exec(ExpandConstant('{tmp}\CompanionBootstrap.exe'), Parameters, ExpandConstant('{tmp}'), SW_HIDE, ewWaitUntilTerminated, ExitCode);
  Result := Result and (ExitCode = 0) and FileExists(Report);
end;

function Quoted(const Value: String): String;
begin
  Result := '"' + Value + '"';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Metadata, Archive, Payload, Report, Url, Digest, ErrorText, ApiUrl: String;
begin
  Result := True;
  if (CurPageID <> wpReady) or DownloadReady then Exit;
  DownloadPage.Clear;
  ApiUrl := 'https://api.github.com/repos/yashkarpsher/companion/releases/latest';
#ifdef SmokeTest
  ApiUrl := ExpandConstant('{param:TESTAPI|https://api.github.com/repos/yashkarpsher/companion/releases/latest}');
#endif
  DownloadPage.Add(ApiUrl, 'companion-latest.json', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.SetText('Ищем последнюю версию Companion', 'Проверяем релизы на GitHub');
      DownloadPage.Download;
      ExtractTemporaryFile('CompanionBootstrap.exe');
      Metadata := ExpandConstant('{tmp}\companion-latest.json');
      Report := ExpandConstant('{tmp}\companion-release.ini');
      if not RunBootstrap('resolve ' + Quoted(Metadata) + ' ' + Quoted(Report), Report) then
      begin
        ErrorText := GetIniString('Release', 'Error', 'Не удалось прочитать данные релиза.', Report);
        RaiseException(ErrorText);
      end;
      DownloadedVersion := GetIniString('Release', 'Version', '', Report);
      Url := GetIniString('Release', 'Url', '', Report);
      Digest := GetIniString('Release', 'SHA256', '', Report);
#ifdef SmokeTest
      if ExpandConstant('{param:TESTARCHIVE|}') <> '' then Url := ExpandConstant('{param:TESTARCHIVE|}');
#endif
      if (DownloadedVersion = '') or (Url = '') or (Length(Digest) <> 64) then
        RaiseException('GitHub не предоставил подходящий архив программы.');
      DownloadPage.Clear;
      DownloadPage.SetText('Скачиваем Companion ' + DownloadedVersion, 'После загрузки проверим и установим программу');
      DownloadPage.Add(Url, 'companion-latest.zip', Digest);
      DownloadPage.Download;
      Archive := ExpandConstant('{tmp}\companion-latest.zip');
      Payload := ExpandConstant('{tmp}\companion-payload');
      if DirExists(Payload) then DelTree(Payload, True, True, True);
      DownloadPage.SetText('Проверяем файлы Companion', 'Проверка архива и подготовка к установке');
      if not RunBootstrap('verify ' + Quoted(Archive) + ' ' + Quoted(Digest) + ' ' + Quoted(Payload) + ' ' + Quoted(DownloadedVersion) + ' ' + Quoted(Report), Report) then
      begin
        ErrorText := GetIniString('Release', 'Error', 'Не удалось проверить файлы программы.', Report);
        RaiseException(ErrorText);
      end;
      DownloadReady := True;
      Log('Prepared stable Companion release ' + DownloadedVersion);
    except
      Result := False;
      if not DownloadPage.AbortedByUser then
        SuppressibleMsgBox('Не удалось загрузить Companion.' + #13#10 + #13#10 + GetExceptionMessage + #13#10 + #13#10 + 'Проверь подключение к интернету и нажми «Установить» ещё раз. Установленные файлы и записи не изменены.', mbError, MB_OK, IDOK);
    end;
  finally
    DownloadPage.Hide;
  end;
end;
