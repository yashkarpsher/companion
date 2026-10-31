param([string]$OutputPath = (Join-Path $PSScriptRoot 'Majestic Companion.exe'))
$ErrorActionPreference = 'Stop'
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
$buildArguments += (Join-Path $PSScriptRoot 'CompanionV2.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionOrbit.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCalculator.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionVisual.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowser.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionBrowserAudio.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionOverlay.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionPersonalization.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionDashboard.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWindowDrag.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSplash.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSectionOrder.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionSectionDialog.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionWidgets.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionTimerSchedule.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionArcade.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionHelp.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCreator.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionUpdates.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionUpdateCore.cs')
$buildArguments += @('/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCardModels.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionCardGames.cs')
$buildArguments += (Join-Path $PSScriptRoot 'CompanionAssembly.cs')
$buildArguments += ('/resource:' + (Join-Path $PSScriptRoot 'CompanionTheme.xaml') + ',CompanionTheme')
$buildArguments += ('/resource:' + (Join-Path $PSScriptRoot 'CompanionReadme.txt') + ',CompanionReadme')
$tutorial = Join-Path $PSScriptRoot 'tutorial'
foreach ($name in @('header','palette','opacity','wallpaper','browser','hotkeys','quicknote','widgets')) {
 $picture = Join-Path $tutorial ($name + '.png')
 if (!(Test-Path -LiteralPath $picture)) { throw ('Missing tutorial screenshot: ' + $picture) }
 $buildArguments += '/resource:' + $picture + ',Tutorial.' + $name + '.png'
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