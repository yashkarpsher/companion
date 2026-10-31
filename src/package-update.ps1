param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'releases'))
$ErrorActionPreference = 'Stop'
$versionText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'CompanionAssembly.cs'))
$match = [regex]::Match($versionText, 'AssemblyVersion\("(\d+\.\d+\.\d+)\.0"\)')
if (!$match.Success) { throw 'Set AssemblyVersion to MAJOR.MINOR.PATCH.0 first.' }
$version = $match.Groups[1].Value
$root = [IO.Path]::GetFullPath($OutputDirectory)
$payload = Join-Path $root ('Companion-' + $version)
if (Test-Path -LiteralPath $payload) { throw ('Output already exists: ' + $payload + '. Use a new output directory or a new version.') }
New-Item -ItemType Directory -Path $payload -Force | Out-Null
& (Join-Path $PSScriptRoot 'build.ps1') -OutputPath (Join-Path $payload 'Majestic Companion.exe')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CompanionReadme.txt') -Destination (Join-Path $payload 'Прочитай меня.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GitHub-обновления.md') -Destination $payload
$hashes = Get-ChildItem -LiteralPath $payload -File | ForEach-Object { $h = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256; $h.Hash.ToLowerInvariant() + '  ' + $_.Name }
[IO.File]::WriteAllLines((Join-Path $payload 'SHA256SUMS.txt'), [string[]]$hashes)
$zip = Join-Path $root ('Majestic-Companion-' + $version + '.zip')
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $zip -Force
$digest = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($zip + '.sha256'), $digest + '  ' + [IO.Path]::GetFileName($zip) + "`r`n", [Text.Encoding]::ASCII)
Write-Output ('Create GitHub Release v' + $version + ' in https://github.com/yashkarpsher/companion/releases/new')
Write-Output ('Attach: ' + $zip)
Write-Output ('Optional SHA-256 file: ' + $zip + '.sha256')
