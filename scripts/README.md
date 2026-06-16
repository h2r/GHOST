# GHOST launch protocol

**Server-centric:** everything operators touch — the web console, the video
relay, and ROS — runs on the always-reachable **server**, shown as one flat
6-pane tmux. **Windows runs only Unity**, which produces the scene video and
pushes it to the server. This is also why multi-device "just works": every
operator hits one reachable host instead of the Windows workstation behind its
firewall.

```
Windows (producer)                    Server 128.148.138.132 — one tmux "ghost"
─────────────────                     ┌────────────┬────────────┬───────────┐
Unity -spectator                      │ Drivers    │ Aggregator │  Video    │  MediaMTX
  SpotObserver depth (GPU)            ├────────────┼────────────┤───────────┤
  render + NVENC ── RTSP ──────────►  │ Bridge     │ Coord      │  Web      │  console
                  to :8554            └────────────┴────────────┴───────────┘
                                       left 2×2 = ROS (in the ros2_ws container)
operators' browsers ───────────────►  right = Video/Web (on the host)
   page :5173 · video :8889 · ros :9090
```

## One-time setup

**Server** (the web console, stream config, and launcher now live in the
workspace repo — no GHOST clone needed here):
```bash
cd ~/spot_ros2_multi_ws && git checkout many-humans && git pull
bash scripts/build-web.sh          # builds web/dist via a throwaway node container
cd stream && ./get_mediamtx.sh linux && cd ..
sudo apt-get install -y tmux       # if the host lacks it
```
In the `ros2_ws` container (once): `colcon build --packages-select ghost_msgs ghost_aggregator`.
Make sure `stream/mediamtx.yml` has `webrtcEncryption: no` (plain-http LAN).

**Windows:** clone GHOST, checkout `many-humans`, install the SpotObserver
DLLs (`Assets\Plugins\x86_64\`) and depth model (`Assets\onnx\promptda\`), and
set `GHOST_SPECTATOR=1` + `GHOST_SPECTATOR_RTSP=rtsp://128.148.138.132:8554/scene`
(so editor-Play streams to the server).

## Every session

On Windows:
```powershell
pwsh .\scripts\ghost-windows.ps1
```
It updates the repo, prints the Unity launch line and the operator URL, then
SSHes in and brings up the flat 6-pane tmux (this window becomes that view).
Then:
1. **Unity:** Play in spectator mode (pushes video to the server).
2. **Operators:** open
   `http://128.148.138.132:5173/?ros=ws://128.148.138.132:9090&video=http://128.148.138.132:8889/scene/whep&op=<you>`
   — from **any** device on the network.

Drive: `WASD` → Tusker, arrows → Gouger, `Del` → e-stop both.

## Localization

`spot_multi` (the **Coord** pane) attempts GraphNav fiducial localization at
startup. If a robot reports "not localized", point its cameras at a fiducial
and, in the **Coord** pane (or any container shell), run:
```bash
bash /ros2_ws/ghost-localize.sh
```

## Good to know

- **Single commander, with a caveat.** The aggregator is the intended sole
  publisher to `/spot*/cmd_vel`. `spot_multi`'s sync-drive is also wired there
  but stays dormant unless something publishes `/multi_spot/cmd_vel` — joint
  control stays available; don't drive both at once. Check: `ros2 topic info /spot/cmd_vel`.
- **Lease:** bring the robot up and hand its control to ROS (not the BD tablet)
  before commands move it; keep the tablet's e-stop in reserve.
- **Firewall:** the server must allow inbound 5173 (page), 8889/TCP + 8189/UDP
  (WebRTC), 9090 (rosbridge). 8554 must accept Unity's RTSP push.
- **Tear down:** `tmux kill-session -t ghost` stops everything (ROS panes,
  MediaMTX, web server).
- **The older `spot_ros2_multi_ws/launch_ghost.sh`** (ROS-only, runs inside the
  container) is superseded by this but still works if you only want the ROS
  half.

## Untested — verify on first run

This server-centric launcher hasn't been run end-to-end yet. Likely first-run
snags: the host may lack `tmux`; `tmux split-window -p` may need `-l 34%` on
newer tmux; the **Drivers** pane runs both robots with `&` (interleaved logs);
MediaMTX WebRTC may need the server LAN IP in `webrtcAdditionalHosts` for
cross-device video. Paste whatever the first run shows and we tune it.
