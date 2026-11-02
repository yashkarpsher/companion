param(
 [string]$CompilerPath,
 [string]$PayloadDirectory,
 [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\..\..\outputs'),
 [switch]$SmokeTest,
 [switch]$Online
)
$ErrorActionPreference = 'Stop'
if (!$CompilerPath) {
 $compilerCandidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 7\ISCC.exe")
 $CompilerPath = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$CompilerPath -or !(Test-Path -LiteralPath $CompilerPath)) { throw 'Install Inno Setup 6.7 or newer, or pass -CompilerPath pointing to ISCC.exe.' }
$sourceDirectory = Split-Path -Parent $PSScriptRoot
$version = [regex]::Match([IO.File]::ReadAllText((Join-Path $sourceDirectory 'CompanionAssembly.cs')), 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)').Groups[1].Value
if (!$version) { throw 'Missing assembly version.' }
if (!$Online -and !$PayloadDirectory) {
 $PayloadDirectory = Join-Path $OutputDirectory ('installer-payload-' + $version)
 New-Item -ItemType Directory -Path $PayloadDirectory -Force | Out-Null
 & (Join-Path $sourceDirectory 'build.ps1') -OutputPath (Join-Path $PayloadDirectory 'Majestic Companion.exe')
 Copy-Item -LiteralPath (Join-Path $sourceDirectory 'CompanionReadme.txt') -Destination (Join-Path $PayloadDirectory 'Прочитай меня.txt')
}
if (!$Online) { $PayloadDirectory = [IO.Path]::GetFullPath($PayloadDirectory) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (!$Online) {
$actualVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $PayloadDirectory 'Majestic Companion.exe')).Version.ToString(3)
if ($actualVersion -ne $version) { throw 'Payload version does not match the source.' }
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$compileArguments = @('/Qp', ('/DAppVersion=' + $version), ('/DPayloadDir=' + $PayloadDirectory), ('/DOutputDir=' + $OutputDirectory))
if ($SmokeTest) { $compileArguments += '/DSmokeTest=1' }
if ($Online) {
 $helperDirectory = Join-Path $OutputDirectory '.installer-tools'
 New-Item -ItemType Directory -Path $helperDirectory -Force | Out-Null
 $helper = Join-Path $helperDirectory 'CompanionBootstrap.exe'
 $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 $helperArguments = @('/nologo','/target:exe','/platform:x64',('/out:'+$helper),'/reference:System.Web.Extensions.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll',(Join-Path $PSScriptRoot 'CompanionBootstrap.cs'),(Join-Path $sourceDirectory 'CompanionUpdateCore.cs'))
 & $compiler @helperArguments
 if ($LASTEXITCODE -ne 0) { throw 'Bootstrap helper compilation failed.' }
 $compileArguments += ('/DBootstrapHelper=' + $helper)
 $compileArguments += (Join-Path $PSScriptRoot 'CompanionOnline.iss')
} else { $compileArguments += (Join-Path $PSScriptRoot 'Companion.iss') }
& $CompilerPath @compileArguments
if ($LASTEXITCODE -ne 0) { throw ('Installer compilation failed: ' + $LASTEXITCODE) }
$installerName = if ($Online) { 'Companion-Setup.exe' } else { 'Companion-Setup-' + $version + '.exe' }
$installer = Join-Path $OutputDirectory $installerName
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($installer + '.sha256'), $hash + '  ' + [IO.Path]::GetFileName($installer) + "`r`n", [Text.Encoding]::ASCII)
Get-Item -LiteralPath $installer | Select-Object FullName,Length
