# GHOST launch protocol

The system spans two machines. One command on the Windows box brings up the
whole stack in labelled terminals; two manual steps remain (Unity Play, open
the console).

```
Windows machine                         ROS server (128.148.138.132, in docker)
─────────────────                       ───────────────────────────────────────
ghost-windows.ps1 ── wt tabs ──┐
  Video : MediaMTX              │  ssh   ┌─ tmux session "ghost" ──────────────┐
  Web   : vite console          ├──────► │ Bridge     rosbridge + file_server  │
  ROS   : ssh → launch_ghost.sh ┘        │ Drivers    spot tusker + gouger     │
                                         │ Coord      spot_multi (localization)│
Unity (manual): press Play               │ Aggregator operator_aggregator      │
                                         │ Localize   re-runnable fiducial fix │
                                         └─────────────────────────────────────┘
```

## One-time setup

**Server** (in the container, `docker compose exec ros2_ws bash`):
```bash
cd /ros2_ws && git checkout many-humans
colcon build --packages-select ghost_msgs ghost_aggregator
```

**Windows:**
- `git checkout many-humans`, then in Unity install the SpotObserver DLLs
  (`Assets\Plugins\x86_64\`) and the depth model
  (`Assets\onnx\promptda\...onnx`) — these are Git-LFS / vendor files.
- `cd stream` → download MediaMTX (`get_mediamtx.sh windows`) and create
  `mediamtx.yml` certs *or* run with `webrtcEncryption: no` for plain-http LAN.
- `cd web && npm install`.
- Set the `GHOST_SPECTATOR=1` env var (so Unity Play enters spectator mode).

## Every session

On the Windows machine:
```powershell
pwsh .\scripts\ghost-windows.ps1
```
That **auto-updates both repos** (Windows pulls GHOST; the ROS tab pulls the
workspace on the server and rebuilds the ghost packages), then opens the
**Video**, **Web**, and **ROS** tabs — so you never have to fetch/pull by
hand. Then:

1. **Unity:** open GHOST, press **Play** (spectator mode streams the scene).
2. **Console:** open the URL the script prints —
   `http://localhost:5173/?ros=ws://<server>:9090&video=http://localhost:8889/scene/whep&op=<you>`

Drive: `WASD` → Tusker, arrows → Gouger, `Del` → e-stop both.

## Localization

`spot_multi` (the **Coord** window) attempts GraphNav fiducial localization at
startup. If a robot couldn't see a fiducial then and reports "not localized",
point its cameras at a marker and, in the **Localize** window, run:
```
localize
```
(equivalently `bash /ros2_ws/ghost-localize.sh`). Re-run any time localization
is lost.

## Good to know

- **Single commander, with a caveat.** The aggregator is the intended sole
  publisher to `/spot*/cmd_vel`. `spot_multi`'s sync-drive node is also wired
  there but stays **dormant** unless something publishes `/multi_spot/cmd_vel`
  — so joint control is available later, but don't drive both paths at once.
  Sanity check: `ros2 topic info /spot/cmd_vel`.
- **Lease:** the robot must be brought up (powered/standing) and its control
  handed to ROS (not held by the BD tablet) before commands move it; keep the
  tablet's e-stop in reserve.
- **Video is plain http** on the LAN (`webrtcEncryption: no`), so the console
  `video=` URL is `http://…:8889/scene/whep`, not https.
- **Skip drivers:** `START_DRIVERS=false bash /ros2_ws/launch_ghost.sh` brings
  up everything except the robot drivers.
- **Auto-update toggles:** the launcher pulls both repos and rebuilds on every
  run. Set `$env:GHOST_NO_PULL=1` (Windows) to skip the GHOST pull, or
  `SKIP_BUILD=true` on the server to skip the colcon rebuild — useful offline
  or when you have local edits you don't want a `--ff-only` pull to trip over.
- **Tear down:** detach a tmux window with `Ctrl-b d`; kill the whole server
  stack with `tmux kill-session -t ghost`. Close the Windows tabs to stop
  MediaMTX / vite.
