# Launch the GHOST stack from Windows.
#
# In the server-centric architecture, Windows runs ONLY Unity — everything
# else (ROS + video + web) lives in one flat tmux on the server. This script
# updates the local repo, launches Unity in spectator mode (with the env vars
# set so it pushes video to the server), then SSHes in and brings up the
# 6-pane server session (this window becomes that flat view). Press Play once
# the Unity editor opens.
#
# Set $env:GHOST_NO_PULL=1 to skip the local pull (offline / local edits).

$GhostRoot  = Split-Path -Parent $PSScriptRoot
$RosUser    = 'vr-teleop'
$RosHost    = '128.148.138.132'
$OperatorId = $env:USERNAME
# Dedicated server-side checkout for this branch (keep the lab's shared
# spot_ros2_multi_ws clean on main). Clone many-humans here once; see notes.
$ServerRepo = '~/many-humans'

if (-not $env:GHOST_NO_PULL) {
    Write-Host 'Updating local GHOST (for Unity)...' -ForegroundColor DarkGray
    git -C "$GhostRoot" fetch --quiet
    git -C "$GhostRoot" checkout --quiet many-humans
    git -C "$GhostRoot" pull --ff-only
}

# Launch Unity in spectator mode. Setting the env vars HERE and starting Unity
# as a child of this script guarantees it inherits them — no permanent vars,
# no Hub-restart dance. (You still press Play once the editor opens.)
# Close any open Unity first; set $env:GHOST_NO_UNITY=1 to skip this.
if (-not $env:GHOST_NO_UNITY) {
    $env:GHOST_SPECTATOR = '1'
    $env:GHOST_SPECTATOR_RTSP = "rtsp://$RosHost`:8554/scene"

    $UnityExe = $env:GHOST_UNITY_EXE   # override if your install path differs
    if (-not $UnityExe) {
        $ver = ((@(Select-String -Path "$GhostRoot\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion:')[0].Line) -split ':')[1].Trim()
        $UnityExe = "C:\Program Files\Unity\Hub\Editor\$ver\Editor\Unity.exe"
    }
    if (Test-Path $UnityExe) {
        Write-Host 'Launching Unity in spectator mode (press Play when it opens)...' -ForegroundColor DarkGray
        Start-Process $UnityExe -ArgumentList "-projectPath `"$GhostRoot`""
    } else {
        Write-Host "Unity not found at: $UnityExe" -ForegroundColor Yellow
        Write-Host "Set `$env:GHOST_UNITY_EXE to your Unity.exe, or launch Unity from THIS window (env vars are set here)." -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host 'Operators (any device on the network) open:' -ForegroundColor Green
Write-Host "  http://$RosHost`:5173/?ros=ws://$RosHost`:9090&video=http://$RosHost`:8889/scene/whep&op=$OperatorId"
Write-Host ''
Write-Host 'Bringing up the server stack (one flat 6-pane tmux)...' -ForegroundColor DarkGray

# Pull the server workspace and launch the flat session, attaching here.
# Single ssh call (no Windows Terminal), so the '&&' chain is safe.
ssh -t "$RosUser@$RosHost" "cd $ServerRepo && git fetch -q && git checkout -q many-humans && git pull --ff-only && bash scripts/ghost-up.sh"
