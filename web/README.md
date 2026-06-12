# GHOST operator console

Browser client for multi-operator teleoperation. Publishes keyboard drive
commands as `ghost_msgs/OperatorInput` to `/operators/input` over rosbridge
and renders `/ui_state` — each channel's operators, their aggregation
weights, and the fused command actually sent to the robots (see
`ghost_aggregator` in the ROS 2 workspace). The center panel is reserved for
the WebRTC scene stream. Talks the rosbridge v2 JSON protocol directly over
a WebSocket; no ROS client library involved.

## Controls

| keys | channel |
|---|---|
| `W A S D` | `spot/drive` (forward/back, turn) |
| `▲ ◀ ▼ ▶` | `spot2/drive` |

Commands publish at 10 Hz while held; release (or window blur) sends
explicit zero-twists, then the channel goes silent. Operator id and the
rosbridge URL are editable in the top bar and persist in localStorage.

## Running

```bash
npm install
npm run dev        # serves on the LAN (vite --host)
```

Requires rosbridge on `ws://<server>:9090` launched from a workspace where
`ghost_msgs` is built and sourced.
