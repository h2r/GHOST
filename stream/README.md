# GHOST scene stream

One shared exocentric view of the completed scene, rendered once in Unity
and broadcast to every operator console over WebRTC:

```
Unity player, -spectator mode          (Assets/SpectatorStream/)
  fixed camera → RenderTexture → AsyncGPUReadback
    → raw RGBA over stdin → ffmpeg (H.264, low-latency)
      → RTSP push :8554/scene → MediaMTX → WHEP (WebRTC) :8889/scene/whep
                                              → operator consoles (web/)
```

## Server setup (the machine running Unity)

```bash
./get_mediamtx.sh        # linux | windows — downloads the binary into bin/
./gen_certs.sh           # self-signed TLS for the WHEP endpoint (LAN)
./run_server.sh          # MediaMTX on :8554 (RTSP in) and :8889 (WebRTC out)
```

ffmpeg must be on PATH (or pass `-spectator-ffmpeg`). On Windows use a
full build (e.g. gyan.dev) so `h264_nvenc` is available.

Then launch the GHOST player in spectator mode:

```
GHOST.exe -spectator -spectator-encoder nvenc
```

No scene or build settings change: `-spectator` shuts down XR at startup,
spawns a fixed camera, and starts streaming (see
`Assets/SpectatorStream/SpectatorBootstrap.cs` for `-spectator-pose`,
`-spectator-size`, `-spectator-fps`, `-spectator-rtsp` overrides). Without
the flag the VR path is untouched.

Each console's video panel connects to `https://<server>:8889/scene/whep`
(editable in the top bar). Browsers must trust the self-signed cert once:
open `https://<server>:8889/scene` and accept the warning.

## Testing without Unity

`tools/test_stream.sh` impersonates the Unity producer — same raw-RGBA pipe,
same encoder invocation, ffmpeg test pattern as content:

```bash
./tools/test_stream.sh openh264          # or nvenc / x264, plus size/fps/url args
```

If the test pattern reaches the console but the Unity stream doesn't, the
problem is on the Unity side of the pipe; if it doesn't, the problem is
server/network/console-side.

## Troubleshooting

- **Stream upside down** — graphics APIs disagree on readback row order;
  toggle `flipVertically` on `SpectatorStreamer` (drop `-vf vflip`).
- **Choppy or stalling with NVENC** — the encoder shares the GPU with the
  depth-completion CUDA/ONNX work; try `-spectator-encoder x264` (CPU) or
  lower `-spectator-size`.
- **Console says "stream unreachable"** — check the publish is live
  (MediaMTX log shows `is publishing to path 'scene'`), then the cert trust
  step above.
- **Total fallback** — OBS game-capture of the Unity window, streaming WHIP
  to `http://<server>:8889/scene/whip`, replaces the whole Unity→ffmpeg seam
  with zero code; everything downstream is unchanged.

Note: the point-cloud *content* updates at the RGB-D ingest rate (~4 Hz over
robot Wi-Fi); the stream itself is smooth at the configured framerate.
