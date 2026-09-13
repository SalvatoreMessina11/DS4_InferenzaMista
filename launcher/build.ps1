$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { throw 'Compilatore .NET Framework non trovato.' }
$output = Join-Path $PSScriptRoot '..\dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /out:"$output\DS4-Launcher.exe" "$PSScriptRoot\DS4Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione non riuscita' }
$launcherHash = (Get-FileHash "$output\DS4-Launcher.exe" -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content "$output\DS4-Launcher.sha256" "$launcherHash  DS4-Launcher.exe" -Encoding ascii
Write-Output "$output\DS4-Launcher.exe"
