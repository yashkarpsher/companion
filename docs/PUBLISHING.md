# Публикация обновления

Текущий релиз: **v0.16.2**. Архив для установки и автообновления: `Majestic-Companion-0.16.2.zip`.

## Как работает первая публикация

Workflow `.github/workflows/publish-release.yml` проверяет SHA-256 готового архива из `dist/0.16.2` и создаёт стабильный GitHub Release с тегом `v0.16.2`. Он прикрепляет ZIP и файл контрольной суммы. Личные данные в архив не входят.

Workflow использует временный `GITHUB_TOKEN` только в пределах задания. Отдельный персональный токен не нужен. Повторный запуск не перезаписывает уже существующий релиз.

## Следующая версия

1. Обнови `src/CompanionAssembly.cs` и `src/CompanionReadme.txt`.
2. Собери архив на Windows:

   ```powershell
   .\src\package-update.ps1
   ```

3. Создай релиз на странице https://github.com/yashkarpsher/companion/releases/new.
4. Для версии 0.15.1 используй тег `v0.15.1` и прикрепи `Majestic-Companion-0.15.1.zip`. Не включай Draft и Pre-release.
5. Добавь описание изменений и опубликуй релиз. GitHub рассчитает SHA-256 загруженного файла; программа использует эту сумму при проверке обновления.

Чтобы использовать workflow для следующего выпуска, обнови его VERSION, пути архива и файл заметок. Помести новый архив и `.sha256` в соответствующую папку `dist`.

## Правила пакета

В корне ZIP должны находиться:

- `Majestic Companion.exe`;
- `Companion Updater.exe`;
- `Microsoft.Web.WebView2.Core.dll`;
- `Microsoft.Web.WebView2.Wpf.dll`;
- `WebView2Loader.dll`.

Не добавляй `data`, профили пользователей, логи, сертификаты или тестовые программы. Скрипт `package-update.ps1` собирает пакет в новую папку из фиксированного набора файлов.

Имя архива, тег и версия EXE должны совпадать. Для автообновления не подходят автоматически создаваемые архивы GitHub Source code.

Официальная документация: [GitHub Releases API](https://docs.github.com/en/rest/releases/releases).
