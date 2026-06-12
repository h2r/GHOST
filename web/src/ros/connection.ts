import {
  INPUT_TOPIC,
  INPUT_TYPE,
  UI_STATE_TOPIC,
  UI_STATE_TYPE,
} from "../config";
import type { OperatorInput, UiState } from "./messages";

export type ConnectionStatus = "connecting" | "connected" | "closed" | "error";

const RECONNECT_DELAY_MS = 2000;

/** One rosbridge connection: OperatorInput up, UiState down.
 *
 * Speaks the rosbridge v2 JSON protocol directly (advertise / publish /
 * subscribe ops over a WebSocket) — small enough that a client library
 * isn't worth its weight.
 */
export class RosConsole {
  private socket: WebSocket | null = null;
  private reconnectTimer: number | null = null;
  private closedByUser = false;

  status: ConnectionStatus = "closed";
  onStatus: (status: ConnectionStatus) => void = () => {};
  onUiState: (state: UiState) => void = () => {};

  connect(url: string) {
    this.disconnect();
    this.closedByUser = false;
    this.setStatus("connecting");

    let socket: WebSocket;
    try {
      socket = new WebSocket(url);
    } catch {
      this.setStatus("error");
      this.scheduleReconnect(url);
      return;
    }
    this.socket = socket;

    socket.onopen = () => {
      this.send({ op: "advertise", topic: INPUT_TOPIC, type: INPUT_TYPE });
      this.send({ op: "subscribe", topic: UI_STATE_TOPIC, type: UI_STATE_TYPE });
      this.setStatus("connected");
    };
    socket.onmessage = (event) => {
      const message = JSON.parse(event.data);
      if (message.op === "publish" && message.topic === UI_STATE_TOPIC) {
        this.onUiState(message.msg as UiState);
      }
    };
    socket.onerror = () => {
      if (this.socket === socket) this.setStatus("error");
    };
    socket.onclose = () => {
      if (this.socket !== socket) return;
      this.socket = null;
      if (!this.closedByUser) {
        this.setStatus("connecting");
        this.scheduleReconnect(url);
      } else {
        this.setStatus("closed");
      }
    };
  }

  disconnect() {
    this.closedByUser = true;
    if (this.reconnectTimer !== null) {
      window.clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    if (this.socket) {
      const socket = this.socket;
      this.socket = null;
      socket.close();
    }
  }

  publishInput(input: OperatorInput) {
    if (this.status !== "connected") return;
    this.send({ op: "publish", topic: INPUT_TOPIC, msg: input });
  }

  /** Fire-and-forget ROS service call (e.g. the drivers' stop Triggers). */
  callService(service: string, type: string) {
    if (this.status !== "connected") return;
    this.send({ op: "call_service", service, type, args: {} });
  }

  private send(message: object) {
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    }
  }

  private scheduleReconnect(url: string) {
    if (this.reconnectTimer !== null) return;
    this.reconnectTimer = window.setTimeout(() => {
      this.reconnectTimer = null;
      if (!this.closedByUser) this.connect(url);
    }, RECONNECT_DELAY_MS);
  }

  private setStatus(status: ConnectionStatus) {
    this.status = status;
    this.onStatus(status);
  }
}
