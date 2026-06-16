#!/bin/bash
# The whole GHOST stack as ONE flat 6-pane tmux, on the server. Operators reach
# everything here (the always-reachable host), which also solves multi-device
# access. Windows runs ONLY Unity, pushing its video stream to this machine.
#
#   ┌────────────┬────────────┬──────────────┐
#   │ Drivers    │ Aggregator │  Video       │   The right column (Video/Web)
#   ├────────────┼────────────┤──────────────┤   runs on THIS host; the four
#   │ Bridge     │ Coord      │  Web         │   ROS panes run in the ros2_ws
#   └────────────┴────────────┴──────────────┘   container via `docker exec`.
#
# Run this on the SERVER (where GHOST is cloned). One-time setup first — see
# scripts/README.md: build the web console (scripts/build-web.sh), fetch the
# Linux MediaMTX (cd stream && ./get_mediamtx.sh linux), build the ros
# packages in the container, and have tmux installed on the host.
#
# Usage: bash scripts/ghost-up.sh        (creates + attaches the 'ghost' session)

set -e

SESSION="ghost"
CONTAINER="ros2_ws"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ENTER="docker exec -it $CONTAINER bash"
SRC="source /opt/ros/humble/setup.bash && source /ros2_ws/install/setup.bash"

if tmux has-session -t "$SESSION" 2>/dev/null; then
    echo "Session '$SESSION' already running — attaching. (tmux kill-session -t $SESSION to reset.)"
    exec tmux attach -t "$SESSION"
fi

# Geometry: left 2x2 (ROS), right column split in two (content).
tmux new-session -d -s "$SESSION"
tmux split-window -h -p 34 -t "$SESSION":0     # pane 1 = right column (34% wide)
tmux split-window -v       -t "$SESSION":0.0   # pane 2 = left-bottom
tmux split-window -h       -t "$SESSION":0.0   # pane 3 = left-top-right
tmux split-window -h       -t "$SESSION":0.2   # pane 4 = left-bottom-right
tmux split-window -v       -t "$SESSION":0.1   # pane 5 = right-bottom
tmux set-option -t "$SESSION" pane-border-status top
tmux set-option -t "$SESSION" mouse on

# Title a pane, enter the container, run a sourced command. Two send-keys
# (enter shell, then run) avoids nested quoting through docker exec.
ros_pane() {  # $1 pane index   $2 title   $3 command
    tmux select-pane -t "$SESSION:0.$1" -T "$2"
    tmux send-keys    -t "$SESSION:0.$1" "$ENTER" C-m
    tmux send-keys    -t "$SESSION:0.$1" "$SRC && $3" C-m
}

# Left 2x2 — ROS, inside the container.
ros_pane 0 "Drivers"    "ros2 launch spot_driver spot_driver.launch.py config_file:=\$HOME/spot_configs/spot_tusker.yaml & ros2 launch spot_driver spot_driver.launch.py config_file:=\$HOME/spot_configs/spot_gouger.yaml"
ros_pane 3 "Aggregator" "ros2 run ghost_aggregator operator_aggregator.py"
ros_pane 2 "Bridge"     "ros2 launch file_server2 ros_sharp_communication.launch.py"
ros_pane 4 "Coord"      "ros2 launch spot_multi spot_multi.launch.py"

# Right column — content, on THIS host.
tmux select-pane -t "$SESSION:0.1" -T "Video (MediaMTX)"
tmux send-keys    -t "$SESSION:0.1" "cd '$ROOT/stream' && ./bin/mediamtx mediamtx.yml" C-m

tmux select-pane -t "$SESSION:0.5" -T "Web (console)"
tmux send-keys    -t "$SESSION:0.5" "cd '$ROOT/web/dist' && python3 -m http.server 5173" C-m

tmux select-pane -t "$SESSION:0.0"
echo "Up. Detach with Ctrl-b d; kill everything with: tmux kill-session -t $SESSION"
exec tmux attach -t "$SESSION"
