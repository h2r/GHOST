# GHOST operator console

Browser client for multi-operator teleoperation. The shared scene stream
(WebRTC/WHEP from MediaMTX — see `stream/` at the repo root) plays full
screen with keycap clusters overlaid; keyboard drive commands publish as
`ghost_msgs/OperatorInput` to `/operators/input` over rosbridge and flow
through the `ghost_aggregator` node (ROS 2 workspace), never to the robots
directly. Talks the rosbridge v2 JSON protocol directly over a WebSocket;
no ROS client library involved. Keycap art: SimpleKeys (Classic/Light); the
DEL cap is composited from the pack's own glyphs (it ships none).

## Controls

| input | action |
|---|---|
| `W A S D` | drive Tusker (`spot/drive`): forward/back, turn |
| `▲ ◀ ▼ ▶` | drive Gouger (`spot2/drive`) |
| `Delete` or the DEL cap | e-stop: zero-twists both channels + asserts `/spot/estop/gentle` and `/spot2/estop/gentle` |

The gentle e-stop is **latched robot-side** — robots sit and stay stopped
until released, e.g. `ros2 service call /spot/estop/release
std_srvs/srv/Trigger`.

Commands publish at 10 Hz while held; release (or window blur) sends
explicit zero-twists, then the channel goes silent.

## Configuration

There is deliberately no settings UI. Operator id, rosbridge URL, and
stream URL come from query parameters, persisted to localStorage:

```
http://<host>:5173/?ros=ws://192.168.1.38:9090&video=https://<winpc>:8889/scene/whep&op=alice
```

Defaults assume rosbridge and MediaMTX run on the host serving the page.

## Running

```bash
npm install
npm run dev        # serves on the LAN (vite --host)
```

Requires rosbridge on `ws://<server>:9090` launched from a workspace where
`ghost_msgs` is built and sourced.
