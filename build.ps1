param([string]$OutputDirectory = 'dist',[switch]$CodeOnly,[string]$TestVersion = '')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$outDir = Join-Path $root $OutputDirectory
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = Split-Path $compiler
$wpf = Join-Path $framework 'WPF'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$refs = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Xml.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$refs += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll','System.Speech.dll') | ForEach-Object { '/r:' + (Join-Path $wpf $_) }
$sources = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
if ($TestVersion) {
 $parsedVersion = [version]$TestVersion
 $testInfoPath = Join-Path $root 'test-output/AppInfo-test.cs'
 $testInfo = [IO.File]::ReadAllText((Join-Path $root 'src/AppInfo.cs')).Replace('Version="0.4.0"',('Version="'+$parsedVersion.ToString()+'"'))
 [IO.File]::WriteAllText($testInfoPath,$testInfo)
 $sources = @($sources | Where-Object { [IO.Path]::GetFileName($_) -ne 'AppInfo.cs' }) + $testInfoPath
}
$icon = Join-Path $root 'assets\LittleNotebook.ico'
$iconArgs = @()
if (Test-Path -LiteralPath $icon) { $iconArgs += '/win32icon:' + $icon }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output ('/win32manifest:' + (Join-Path $root 'app.manifest')) ('/out:' + (Join-Path $outDir 'LittleNotebook.exe')) @iconArgs @refs @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
if (Test-Path -LiteralPath (Join-Path $root 'updater/Program.cs')) {
 & $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output ('/out:' + (Join-Path $outDir 'LittleNotebook.Update.exe')) @refs (Join-Path $root 'updater/Program.cs') (Join-Path $root 'src/AppInfo.cs') (Join-Path $root 'src/UpdateContracts.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Updater compilation failed' }
}
if ($CodeOnly) { Write-Output ('Built code: ' + (Join-Path $outDir 'LittleNotebook.exe')); return }
Copy-Item -LiteralPath (Join-Path $root 'assets') -Destination $outDir -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'content') -Destination $outDir -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'app.config') -Destination (Join-Path $outDir 'LittleNotebook.exe.config') -Force
$speechRuntime = Join-Path $root 'tools\speech-runtime'
if (Test-Path -LiteralPath $speechRuntime) {
 Copy-Item -LiteralPath $speechRuntime -Destination $outDir -Recurse -Force
 Copy-Item -LiteralPath (Join-Path $root 'tools\speech_worker.py') -Destination (Join-Path $outDir 'speech-runtime\worker.py') -Force
}
Write-Output ('Built ' + (Join-Path $outDir 'LittleNotebook.exe'))
