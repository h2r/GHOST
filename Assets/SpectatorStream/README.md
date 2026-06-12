# SpectatorStream

Headless spectator launch mode: `-spectator` (or `GHOST_SPECTATOR=1`) shuts
down XR, spawns a fixed camera, and streams the rendered scene to MediaMTX
as H.264/RTSP for WebRTC distribution to operator consoles.

Self-contained — nothing in any scene references these scripts; the rig is
created at runtime by `SpectatorBootstrap` only when the flag is present.

Server config, launch arguments, testing tools, and troubleshooting live in
`stream/README.md` at the repo root.
