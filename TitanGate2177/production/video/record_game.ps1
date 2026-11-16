# Plays the built game automatically and records the game's own frames (Assets/Scripts/Game/VideoRecorder.cs).
# The desktop is never captured, so the PC can be used meanwhile. Do not minimise the game window.
# Usage: powershell -File production\video\record_game.ps1 -Name play -GameArgs "-autoshot -play -english" -MaxMinutes 40
param(
    [string]$Name = "play",
    [string]$GameArgs = "-autoshot -play -english",
    [int]$MaxMinutes = 40
)
$root = Resolve-Path "$PSScriptRoot\..\.."
$exe = Join-Path $root "Builds\Win\TitanGate2177.exe"
$out = Join-Path $PSScriptRoot "footage"
New-Item -ItemType Directory -Force $out | Out-Null
$ff = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter ffmpeg.exe | Select-Object -First 1 -ExpandProperty FullName
$file = Join-Path $out "$Name.mp4"
$a = "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 $GameArgs -recordvideo `"$file`" -ffmpeg `"$ff`" -recordfps 30"
$t0 = Get-Date
$game = Start-Process -FilePath $exe -ArgumentList $a -PassThru
if (-not $game.WaitForExit($MaxMinutes * 60000)) { Stop-Process -Id $game.Id -Force }
Start-Sleep -Seconds 3
"recorded $file  minutes=$([math]::Round(((Get-Date) - $t0).TotalMinutes, 1))"
