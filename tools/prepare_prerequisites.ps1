$ErrorActionPreference = 'Stop'
$prerequisiteFolder = Join-Path $PSScriptRoot 'install-build\prerequisites'
New-Item -ItemType Directory -Force -Path $prerequisiteFolder | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$packages = @(
 @{ Name='dotnet48.exe'; Url='https://go.microsoft.com/fwlink/?linkid=2088631' },
 @{ Name='vc_redist.x64.exe'; Url='https://aka.ms/vc14/vc_redist.x64.exe' }
)
foreach ($package in $packages) {
 $packagePath = Join-Path $prerequisiteFolder $package.Name
 if (-not (Test-Path -LiteralPath $packagePath)) { Invoke-WebRequest -Uri $package.Url -OutFile $packagePath }
 $signature = Get-AuthenticodeSignature -LiteralPath $packagePath
 if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw "Untrusted Microsoft prerequisite: $($package.Name)" }
 Write-Output "Verified Microsoft signature: $($package.Name)"
}
