param([string]$OutputPath = (Join-Path $PSScriptRoot 'Majestic Companion.exe'))
$ErrorActionPreference = 'Stop'
$neonBackground = Join-Path $PSScriptRoot 'artwork\neon-city.png'
if (!(Test-Path -LiteralPath $neonBackground)) {
 $neonParts = @(Get-ChildItem -LiteralPath (Split-Path -Parent $neonBackground) -Filter 'neon-city.png.part*' -File | Sort-Object Name)
 if ($neonParts.Count -eq 0) { throw 'Missing Companion wallpaper.' }
 $neonStream = [IO.File]::Create($neonBackground)
 try { foreach ($neonPart in $neonParts) { $partInput = [IO.File]::OpenRead($neonPart.FullName); try { $partInput.CopyTo($neonStream) } finally { $partInput.Dispose() } } } finally { $neonStream.Dispose() }
}
if (Test-Path -LiteralPath ($neonBackground + '.sha256')) {
 $neonExpected = ([IO.File]::ReadAllText($neonBackground + '.sha256')).Trim().Split(' ')[0]
 if ((Get-FileHash -LiteralPath $neonBackground -Algorithm SHA256).Hash -ne $neonExpected) { throw 'Companion wallpaper checksum mismatch.' }
}
$runnerSource = Join-Path $PSScriptRoot 'Games\SubwaySaintPetersburg'
if (Test-Path -LiteralPath $runnerSource) {
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 foreach ($firstPart in @(Get-ChildItem -LiteralPath $runnerSource -Filter '*.part000' -File -Recurse)) {
  $assetPath = $firstPart.FullName.Substring(0,$firstPart.FullName.Length - 8)
  if (!(Test-Path -LiteralPath $assetPath)) {
   $assetParts = @(Get-ChildItem -LiteralPath $firstPart.DirectoryName -Filter (([IO.Path]::GetFileName($assetPath)) + '.part*') -File | Sort-Object Name)
   $assetStream = [IO.File]::Create($assetPath)
   try { foreach ($assetPart in $assetParts) { $partStream = [IO.File]::OpenRead($assetPart.FullName); try { $partStream.CopyTo($assetStream) } finally { $partStream.Dispose() } } } finally { $assetStream.Dispose() }
  }
  $hashPath = $assetPath + '.sha256'
  if (!(Test-Path -LiteralPath $hashPath)) { throw ('Missing asset checksum: ' + $hashPath) }
  $expectedHash = ([IO.File]::ReadAllText($hashPath)).Trim().Split(' ')[0]
  if ((Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash -ne $expectedHash) { throw ('Asset checksum mismatch: ' + $assetPath) }
 }
 $runnerZip = Join-Path $PSScriptRoot 'Assets\Runner3D.zip'
 $temporaryRunnerZip = Join-Path ([IO.Path]::GetTempPath()) ('CompanionRunner-' + [Guid]::NewGuid().ToString('N') + '.zip')
 try {
  $gameZip = [IO.Compression.ZipFile]::Open($temporaryRunnerZip,[IO.Compression.ZipArchiveMode]::Create)
  try { foreach ($gameFile in @('index.html','companion.js','4399.js','loaders/v2/unity/static/UnityLoader.2019.2.js','Build/StPetersburg.json','Build/StPetersburg.data.unityweb','Build/StPetersburg.wasm.code.unityweb','Build/StPetersburg.wasm.framework.unityweb')) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($gameZip,(Join-Path $runnerSource $gameFile),$gameFile,[IO.Compression.CompressionLevel]::Optimal) | Out-Null } } finally { $gameZip.Dispose() }
  Copy-Item -LiteralPath $temporaryRunnerZip -Destination $runnerZip -Force
 } finally { if (Test-Path -LiteralPath $temporaryRunnerZip) { Remove-Item -LiteralPath $temporaryRunnerZip } }
}
$gameAsset = Join-Path $PSScriptRoot 'Assets\MisterSvin.zip'
if (!(Test-Path -LiteralPath $gameAsset)) {
 $parts = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Assets') -Filter 'MisterSvin.zip.part*' | Sort-Object Name)
 if ($parts.Count -eq 0) { throw 'Missing embedded game archive' }
 $stream = [IO.File]::Create($gameAsset)
 try { foreach ($part in $parts) { $inputStream = [IO.File]::OpenRead($part.FullName); try { $inputStream.CopyTo($stream) } finally { $inputStream.Dispose() } } } finally { $stream.Dispose() }
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$references = @('PresentationFramework','PresentationCore','WindowsBase') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_ + '.dll')) }
$buildArguments = @('/nologo','/codepage:65001','/target:winexe','/platform:x64','/optimize+',('/out:' + $OutputPath))
$sdk = Join-Path $PSScriptRoot 'vendor\WebView2'
$managedSdk = Join-Path $sdk 'lib\net462'
foreach ($library in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll')) {
 $libraryPath = Join-Path $managedSdk $library
 if (!(Test-Path -LiteralPath $libraryPath)) { throw ('Missing WebView2 SDK: ' + $libraryPath) }
 $buildArguments += '/reference:' + $libraryPath
}
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'companion.ico')) { $buildArguments += '/win32icon:' + (Join-Path $PSScriptRoot 'companion.ico') }
$buildArguments += $references
$buildArguments += @('/reference:System.Xaml.dll','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll',(Join-Path $PSScriptRoot 'Companion.cs'))
$buildArguments += (Join-Path $PSScriptRoot 'CompanionMiniHub.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionV2.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionOrbit.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCalculator.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionVisual.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowser.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserWindow.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserChrome.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserLibrary.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserGuide.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserAudio.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionOverlay.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionPersonalization.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionDashboard.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionLibraryArt.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWorkspace.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWorkspaceTimers.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWindowDrag.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSplash.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSectionOrder.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionGameOrder.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSectionDialog.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWidgets.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionTimerSchedule.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionArcade.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSavings.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionGameVolume.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionFlightVisual.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionRunner3D.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionPig.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionHelp.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCreator.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionUpdates.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionUpdateCore.cs')
$buildArguments += @('/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCardModels.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCardGames.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWallpaperPlacement.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionNeon.cs')
$buildArguments += '/resource:' + (Join-Path $PSScriptRoot 'artwork\neon-city.png') + ',CompanionNeonWallpaper'
$buildArguments += (Join-Path $PSScriptRoot 'CompanionAssembly.cs')
$buildArguments += ('/resource:' + (Join-Path $PSScriptRoot 'CompanionTheme.xaml') + ',CompanionTheme')
$buildArguments += ('/resource:' + (Join-Path $PSScriptRoot 'CompanionReadme.txt') + ',CompanionReadme')
$tutorial = Join-Path $PSScriptRoot 'tutorial'
foreach ($name in @('header','palette','opacity','wallpaper','browser','hotkeys','quicknote','widgets')) {
 $picture = Join-Path $tutorial ($name + '.png')
 if (!(Test-Path -LiteralPath $picture)) { throw ('Missing tutorial screenshot: ' + $picture) }
 $buildArguments += '/resource:' + $picture + ',Tutorial.' + $name + '.png'
}
$buildArguments += '/resource:' + (Join-Path $PSScriptRoot 'Assets\MisterSvin.zip') + ',CompanionPigAssets'
$buildArguments += '/resource:' + (Join-Path $PSScriptRoot 'Assets\Runner3D.zip') + ',CompanionRunner3DAssets'
foreach ($cover in @('flight','runner','durak','blackjack','svin')) {
 $coverPath = Join-Path $PSScriptRoot ('game-covers\' + $cover + '.png')
 if (!(Test-Path -LiteralPath $coverPath)) { throw ('Missing game cover: ' + $cover) }
 $buildArguments += '/resource:' + $coverPath + ',GameCover.' + $cover
}
& $compiler @buildArguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
$destination = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
foreach ($library in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Wpf.dll')) { Copy-Item -LiteralPath (Join-Path $managedSdk $library) -Destination $destination -Force }
Copy-Item -LiteralPath (Join-Path $sdk 'runtimes\win-x64\native\WebView2Loader.dll') -Destination $destination -Force
Write-Output ('Built: ' + $OutputPath)

$helperArguments = @('/nologo','/codepage:65001','/target:winexe','/platform:x64','/optimize+',('/out:' + (Join-Path $destination 'Companion Updater.exe')),'/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll',(Join-Path $PSScriptRoot 'CompanionUpdateCore.cs'),(Join-Path $PSScriptRoot 'CompanionUpdater.cs'),(Join-Path $PSScriptRoot 'CompanionAssembly.cs'))
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'companion.ico')) { $helperArguments += '/win32icon:' + (Join-Path $PSScriptRoot 'companion.ico') }
& $compiler @helperArguments
if ($LASTEXITCODE -ne 0) { throw 'Updater compilation failed' }
