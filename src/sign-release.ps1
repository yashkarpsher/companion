param(
 [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
 [string]$ReleaseDirectory = (Join-Path $PSScriptRoot '..'),
 [string]$SignToolPath = 'signtool.exe'
)
$ErrorActionPreference = 'Stop'
# Use a trusted code-signing certificate already installed by its owner.
# Do not export or upload the private key. Self-signed certificates are not suitable for public releases.
$thumbprint = $CertificateThumbprint.Replace(' ','')
if ($thumbprint -notmatch '^[0-9A-Fa-f]{40}$') { throw 'Provide the SHA-1 thumbprint of your code-signing certificate.' }
$certificate = Get-Item -LiteralPath ('Cert:\CurrentUser\My\' + $thumbprint)
if (!$certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date)) { throw 'A valid certificate with its private key is required.' }
if (!($certificate.EnhancedKeyUsageList | Where-Object {$_.ObjectId -eq '1.3.6.1.5.5.7.3.3'})) { throw 'Certificate is not intended for code signing.' }
$tool = Get-Command $SignToolPath -ErrorAction Stop
$release = (Resolve-Path -LiteralPath $ReleaseDirectory).Path
foreach ($name in @('Majestic Companion.exe','Companion Updater.exe')) {
 $program = Join-Path $release $name
 if (!(Test-Path -LiteralPath $program)) { throw ('Release executable not found: ' + $program) }
 & $tool.Source sign /sha1 $thumbprint /fd SHA256 /tr 'http://timestamp.digicert.com' /td SHA256 $program
 if ($LASTEXITCODE -ne 0) { throw ('Signing failed: ' + $name) }
 $signature = Get-AuthenticodeSignature -LiteralPath $program
 if ($signature.Status -ne 'Valid') { throw ('Signature validation failed: ' + $name + ': ' + $signature.Status) }
}
Get-ChildItem -LiteralPath $release -File | Where-Object {$_.Extension -in @('.exe','.dll')} | ForEach-Object {
 $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
 $hash.Hash.ToLowerInvariant() + '  ' + $_.Name
} | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding UTF8
Write-Output 'Signed and timestamped. Recreate the release ZIP now; do not rebuild or modify the signed executable.'
Write-Output 'A valid signature identifies the publisher. New releases can still trigger SmartScreen while reputation builds.'
