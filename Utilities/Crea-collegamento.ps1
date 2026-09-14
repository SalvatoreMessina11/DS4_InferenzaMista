$ErrorActionPreference = 'Stop'
$executable = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'Artifacts\AIutante.exe')).Path
$desktop = [Environment]::GetFolderPath('Desktop')
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $desktop 'AIutante.lnk'))
$shortcut.TargetPath = $executable
$shortcut.WorkingDirectory = Split-Path -Parent $executable
$shortcut.IconLocation = "$executable,0"
$shortcut.Description = 'AIutante - DeepSeek e Qwen, chat locale o su due PC'
$shortcut.Save()
Write-Output (Join-Path $desktop 'AIutante.lnk')
