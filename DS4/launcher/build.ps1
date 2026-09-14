$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $compiler)) { throw 'Compilatore .NET Framework non trovato.' }
$output = Join-Path $PSScriptRoot '..\..\Utilities\Artifacts'
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /resource:"$PSScriptRoot\Ethernet.ps1",DS4.Ethernet.ps1 /resource:"$PSScriptRoot\pi_support.py",DS4.Pi.py /resource:"$PSScriptRoot\pi_discovery.sh",AIutante.PiDiscovery.sh /win32icon:"$PSScriptRoot\..\..\Utilities\AIUTANTE_icon.ico" /out:"$output\AIutante.exe" "$PSScriptRoot\DS4Launcher.cs" "$PSScriptRoot\LauncherAdvanced.cs" "$PSScriptRoot\LauncherModels.cs" "$PSScriptRoot\EthernetSetup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilazione non riuscita' }
$launcherHash = (Get-FileHash "$output\AIutante.exe" -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content "$output\AIutante.sha256" "$launcherHash  AIutante.exe" -Encoding ascii
Write-Output "$output\AIutante.exe"
