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

/** Pressing this key (or clicking the on-screen DEL cap) e-stops both
 * robots: zero-twists through the aggregator plus each driver's gentle
 * e-stop service (latched until estop/release). */
export const STOP_KEY = "Delete";

/** Read a setting from the URL query (?ros= ?video= ?op=), persisting any
 * override to localStorage — the only configuration path now that the
 * settings UI is gone. */
function setting(param: string, storageKey: string, fallback: string): string {
  const fromQuery = new URLSearchParams(window.location.search).get(param);
  if (fromQuery) {
    localStorage.setItem(storageKey, fromQuery);
    return fromQuery;
  }
  return localStorage.getItem(storageKey) ?? fallback;
}

export interface ChannelBinding {
  channel: string;
  /** robot display name shown under the key cluster */
  label: string;
  /** key code -> twist contribution at full scale */
  keys: Record<string, { x?: number; yaw?: number }>;
  /** T-shaped cluster, in [top, bottom-left, bottom-middle, bottom-right]
   * order; img is a SimpleKeys (Classic/Light) cap in src/assets/keys */
  caps: { code: string; img: string }[];
}

// Robot display names assume the launch order in launch_multi_spot.sh
// (tusker first -> /spot, gouger second -> /spot2); fix here if swapped.
export const BINDINGS: ChannelBinding[] = [
  {
    channel: "spot/drive",
    label: "Tusker",
    keys: {
      KeyW: { x: +LINEAR_SPEED },
      KeyS: { x: -LINEAR_SPEED },
      KeyA: { yaw: +ANGULAR_SPEED },
      KeyD: { yaw: -ANGULAR_SPEED },
    },
    caps: [
      { code: "KeyW", img: "W.png" },
      { code: "KeyA", img: "A.png" },
      { code: "KeyS", img: "S.png" },
      { code: "KeyD", img: "D.png" },
    ],
  },
  {
    channel: "spot2/drive",
    label: "Gouger",
    keys: {
      ArrowUp: { x: +LINEAR_SPEED },
      ArrowDown: { x: -LINEAR_SPEED },
      ArrowLeft: { yaw: +ANGULAR_SPEED },
      ArrowRight: { yaw: -ANGULAR_SPEED },
    },
    caps: [
      { code: "ArrowUp", img: "UP.png" },
      { code: "ArrowLeft", img: "LEFT.png" },
      { code: "ArrowDown", img: "DOWN.png" },
      { code: "ArrowRight", img: "RIGHT.png" },
    ],
  },
];

export function defaultRosbridgeUrl(): string {
  return setting("ros", "ghost.rosbridgeUrl", `ws://${window.location.hostname}:9090`);
}

export function defaultWhepUrl(): string {
  return setting(
    "video",
    "ghost.whepUrl",
    `https://${window.location.hostname}:8889/scene/whep`,
  );
}

const CALLSIGNS = [
  "basalt", "cobalt", "juniper", "mesa", "onyx", "quartz",
  "saffron", "talon", "umber", "vector", "wren", "zenith",
];

export function defaultOperatorId(): string {
  const word = CALLSIGNS[Math.floor(Math.random() * CALLSIGNS.length)];
  const generated = `${word}-${Math.floor(Math.random() * 90 + 10)}`;
  const id = setting("op", "ghost.operatorId", generated);
  localStorage.setItem("ghost.operatorId", id);
  return id;
}
