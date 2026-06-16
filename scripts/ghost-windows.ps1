# Launch the GHOST stack from Windows.
#
# In the server-centric architecture, Windows runs ONLY Unity — everything
# else (ROS + video + web) lives in one flat tmux on the server. This script
# updates the local repo (for Unity), reminds you how to launch Unity so it
# pushes video to the server, then SSHes in and brings up + attaches the
# 6-pane server session (this window becomes that flat view).
#
# Set $env:GHOST_NO_PULL=1 to skip the local pull (offline / local edits).

$GhostRoot = Split-Path -Parent $PSScriptRoot
$RosUser   = 'vr-teleop'
$RosHost   = '128.148.138.132'
$OperatorId = $env:USERNAME

if (-not $env:GHOST_NO_PULL) {
    Write-Host 'Updating local GHOST (for Unity)...' -ForegroundColor DarkGray
    git -C "$GhostRoot" fetch --quiet
    git -C "$GhostRoot" checkout --quiet many-humans
    git -C "$GhostRoot" pull --ff-only
}

Write-Host ''
Write-Host 'Start Unity (spectator mode, pushing video to the server):' -ForegroundColor Yellow
Write-Host "  built player:  GHOST.exe -spectator -spectator-rtsp rtsp://$RosHost`:8554/scene"
Write-Host "  editor:        set GHOST_SPECTATOR=1 and GHOST_SPECTATOR_RTSP=rtsp://$RosHost`:8554/scene, then Play"
Write-Host ''
Write-Host 'Operators (any device on the network) open:' -ForegroundColor Green
Write-Host "  http://$RosHost`:5173/?ros=ws://$RosHost`:9090&video=http://$RosHost`:8889/scene/whep&op=$OperatorId"
Write-Host ''
Write-Host 'Bringing up the server stack (one flat 6-pane tmux)...' -ForegroundColor DarkGray

# Pull the server workspace and launch the flat session, attaching here.
# Single ssh call (no Windows Terminal), so the '&&' chain is safe.
ssh -t "$RosUser@$RosHost" "cd ~/spot_ros2_multi_ws && git fetch -q && git checkout -q many-humans && git pull --ff-only && bash scripts/ghost-up.sh"
