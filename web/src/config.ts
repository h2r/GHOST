// Speeds sent while a key is held. The aggregator clamps to the same caps
// server-side; these are the full-scale values, not limits.
export const LINEAR_SPEED = 0.3; // m/s
export const ANGULAR_SPEED = 0.2; // rad/s

export const PUBLISH_RATE_HZ = 10;
// Explicit zero-twists sent after the last key is released, before going
// silent. Faster stop than waiting out the aggregator's staleness window.
export const RELEASE_ZERO_TICKS = 3;

export const INPUT_TOPIC = "/operators/input";
export const INPUT_TYPE = "ghost_msgs/msg/OperatorInput";
export const UI_STATE_TOPIC = "/ui_state";
export const UI_STATE_TYPE = "ghost_msgs/msg/UiState";

export interface ChannelBinding {
  channel: string;
  label: string;
  /** key code -> twist contribution at full scale */
  keys: Record<string, { x?: number; yaw?: number }>;
  /** keycap hints rendered in the channel card, in display order */
  caps: { code: string; glyph: string }[];
}

export const BINDINGS: ChannelBinding[] = [
  {
    channel: "spot/drive",
    label: "SPOT",
    keys: {
      KeyW: { x: +LINEAR_SPEED },
      KeyS: { x: -LINEAR_SPEED },
      KeyA: { yaw: +ANGULAR_SPEED },
      KeyD: { yaw: -ANGULAR_SPEED },
    },
    caps: [
      { code: "KeyW", glyph: "W" },
      { code: "KeyA", glyph: "A" },
      { code: "KeyS", glyph: "S" },
      { code: "KeyD", glyph: "D" },
    ],
  },
  {
    channel: "spot2/drive",
    label: "SPOT2",
    keys: {
      ArrowUp: { x: +LINEAR_SPEED },
      ArrowDown: { x: -LINEAR_SPEED },
      ArrowLeft: { yaw: +ANGULAR_SPEED },
      ArrowRight: { yaw: -ANGULAR_SPEED },
    },
    caps: [
      { code: "ArrowUp", glyph: "▲" },
      { code: "ArrowLeft", glyph: "◀" },
      { code: "ArrowDown", glyph: "▼" },
      { code: "ArrowRight", glyph: "▶" },
    ],
  },
];

export function defaultRosbridgeUrl(): string {
  const stored = localStorage.getItem("ghost.rosbridgeUrl");
  if (stored) return stored;
  return `ws://${window.location.hostname}:9090`;
}

export function defaultWhepUrl(): string {
  const stored = localStorage.getItem("ghost.whepUrl");
  if (stored) return stored;
  return `https://${window.location.hostname}:8889/scene/whep`;
}

const CALLSIGNS = [
  "basalt", "cobalt", "juniper", "mesa", "onyx", "quartz",
  "saffron", "talon", "umber", "vector", "wren", "zenith",
];

export function defaultOperatorId(): string {
  const stored = localStorage.getItem("ghost.operatorId");
  if (stored) return stored;
  const word = CALLSIGNS[Math.floor(Math.random() * CALLSIGNS.length)];
  const id = `${word}-${Math.floor(Math.random() * 90 + 10)}`;
  localStorage.setItem("ghost.operatorId", id);
  return id;
}
