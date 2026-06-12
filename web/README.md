# GHOST operator console

Browser client for multi-operator teleoperation. The shared scene stream
(WebRTC/WHEP from MediaMTX — see `stream/` at the repo root) plays full
screen with keycap clusters overlaid; keyboard drive commands publish as
`ghost_msgs/OperatorInput` to `/operators/input` over rosbridge and flow
through the `ghost_aggregator` node (ROS 2 workspace), never to the robots
directly. Talks the rosbridge v2 JSON protocol directly over a WebSocket;
no ROS client library involved. Keycap art: SimpleKeys (Jumbo/Light).

## Controls

| input | action |
|---|---|
| `W A S D` | drive Tusker (`spot/drive`): forward/back, turn |
| `▲ ◀ ▼ ▶` | drive Gouger (`spot2/drive`) |
| `Delete` or the STOP cap | force stop: zero-twists both channels + calls `/spot/stop` and `/spot2/stop` |

Commands publish at 10 Hz while held; release (or window blur) sends
explicit zero-twists, then the channel goes silent. Operator id, rosbridge
URL, and stream URL are editable in the top bar and persist in localStorage.

## Running

```bash
npm install
npm run dev        # serves on the LAN (vite --host)
```

Requires rosbridge on `ws://<server>:9090` launched from a workspace where
`ghost_msgs` is built and sourced.
