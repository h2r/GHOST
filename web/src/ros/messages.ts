// TypeScript mirrors of the ghost_msgs definitions (and the std/geometry
// types they embed), as they appear in rosbridge JSON.

export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface Twist {
  linear: Vector3;
  angular: Vector3;
}

export interface Header {
  stamp: { sec: number; nanosec: number };
  frame_id: string;
}

export interface OperatorInput {
  header: Header;
  operator_id: string;
  channel: string;
  twist: Twist;
}

export interface OperatorState {
  operator_id: string;
  twist: Twist;
  weight: number;
  age: number;
  active: boolean;
}

export interface ChannelState {
  channel: string;
  fused: Twist;
  operators: OperatorState[];
}

export interface UiState {
  header: Header;
  channels: ChannelState[];
}

export function zeroTwist(): Twist {
  return {
    linear: { x: 0, y: 0, z: 0 },
    angular: { x: 0, y: 0, z: 0 },
  };
}

export function stampNow(): Header {
  const ms = Date.now();
  return {
    stamp: { sec: Math.floor(ms / 1000), nanosec: (ms % 1000) * 1e6 },
    frame_id: "",
  };
}
