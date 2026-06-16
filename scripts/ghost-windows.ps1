# One-command launch of the GHOST stack from the Windows machine.
# Opens labelled Windows Terminal tabs:
#   Video  - MediaMTX (the WebRTC relay)
#   Web    - the operator console (vite dev server)
#   ROS    - SSHes into the server and runs the labelled tmux stack
# Unity is the one manual step: open GHOST and press Play (spectator mode).
#
# Prereqs: Windows Terminal (wt) + OpenSSH client (both ship with Win 11),
#          MediaMTX downloaded (stream\get_mediamtx.ps1 / get_mediamtx.sh),
#          web deps installed (cd web; npm install), and on the server the
#          ghost packages built (colcon build ... ghost_msgs ghost_aggregator).
#
# Usage:  pwsh .\scripts\ghost-windows.ps1

# --- config: adjust to your setup ---
$GhostRoot    = Split-Path -Parent $PSScriptRoot     # repo root (this script lives in scripts\)
$StreamDir    = Join-Path $GhostRoot 'stream'
$WebDir       = Join-Path $GhostRoot 'web'
$RosUser      = 'vr-teleop'
$RosHost      = '128.148.138.132'
$ServerScript = '/ros2_ws/launch_ghost.sh'
$OperatorId   = $env:USERNAME
# ------------------------------------

if (-not (Get-Command wt -ErrorAction SilentlyContinue)) {
    Write-Error "Windows Terminal (wt) not found. Install it, or start the three tabs by hand (see scripts\README.md)."
    exit 1
}

# The server tab: ssh in, drop into the container, run the labelled tmux stack.
$rosInner = "docker exec -it ros2_ws bash -lc 'bash $ServerScript'"

# Open one Windows Terminal window with three named tabs.
wt new-tab  --title 'Video' -d "$StreamDir" pwsh -NoExit -Command '.\bin\mediamtx.exe mediamtx.yml' `
   `; new-tab --title 'Web'   -d "$WebDir"    pwsh -NoExit -Command 'npm run dev' `
   `; new-tab --title 'ROS'   ssh -t "$RosUser@$RosHost" "$rosInner"

$consoleUrl = "http://localhost:5173/?ros=ws://$RosHost`:9090&video=http://localhost:8889/scene/whep&op=$OperatorId"

Write-Host ''
Write-Host 'Launched tabs: Video (MediaMTX), Web (console), ROS (server stack).' -ForegroundColor Green
Write-Host ''
Write-Host 'Two manual steps remain:' -ForegroundColor Yellow
Write-Host '  1. Open GHOST in Unity and press Play (spectator mode: GHOST_SPECTATOR=1).'
Write-Host '  2. Open the operator console:'
Write-Host "       $consoleUrl"
Write-Host ''
Write-Host 'Localization runs automatically; if a robot says "not localized", use the' -ForegroundColor DarkGray
Write-Host 'Localize tmux window on the server (point cameras at a fiducial, run: localize).' -ForegroundColor DarkGray
