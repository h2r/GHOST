import { BINDINGS, PUBLISH_RATE_HZ, RELEASE_ZERO_TICKS } from "../config";
import type { RosConsole } from "../ros/connection";
import { stampNow, zeroTwist, type Twist } from "../ros/messages";

/** Keyboard state -> per-channel OperatorInput stream.
 *
 * While any bound key is held the channel publishes its twist at
 * PUBLISH_RATE_HZ; on release it publishes RELEASE_ZERO_TICKS explicit
 * zero-twists (immediate stop, instead of waiting out the aggregator's
 * staleness window) and then goes silent. Window blur releases everything.
 */
export class DriveInputEngine {
  private held = new Set<string>();
  private zerosLeft = new Map<string, number>();
  private timer: number | null = null;

  /** notified whenever the held-key set changes, for keycap display */
  onKeysChanged: (held: Set<string>) => void = () => {};

  constructor(
    private ros: RosConsole,
    private getOperatorId: () => string,
  ) {}

  start() {
    window.addEventListener("keydown", this.onKeyDown);
    window.addEventListener("keyup", this.onKeyUp);
    window.addEventListener("blur", this.releaseAll);
    this.timer = window.setInterval(this.tick, 1000 / PUBLISH_RATE_HZ);
  }

  stop() {
    window.removeEventListener("keydown", this.onKeyDown);
    window.removeEventListener("keyup", this.onKeyUp);
    window.removeEventListener("blur", this.releaseAll);
    if (this.timer !== null) window.clearInterval(this.timer);
    this.timer = null;
    this.held.clear();
    this.onKeysChanged(this.held);
  }

  private bound(code: string): boolean {
    return BINDINGS.some((b) => code in b.keys);
  }

  private onKeyDown = (event: KeyboardEvent) => {
    if (isTyping(event)) return;
    if (!this.bound(event.code)) return;
    event.preventDefault();
    if (event.repeat) return;
    this.held.add(event.code);
    this.onKeysChanged(new Set(this.held));
  };

  private onKeyUp = (event: KeyboardEvent) => {
    if (!this.bound(event.code)) return;
    if (this.held.delete(event.code)) {
      this.onKeysChanged(new Set(this.held));
    }
  };

  private releaseAll = () => {
    if (this.held.size === 0) return;
    this.held.clear();
    this.onKeysChanged(this.held);
  };

  private tick = () => {
    for (const binding of BINDINGS) {
      const twist = zeroTwist();
      let engaged = false;
      for (const [code, effect] of Object.entries(binding.keys)) {
        if (!this.held.has(code)) continue;
        engaged = true;
        twist.linear.x += effect.x ?? 0;
        twist.angular.z += effect.yaw ?? 0;
      }

      if (engaged) {
        this.zerosLeft.set(binding.channel, RELEASE_ZERO_TICKS);
        this.publish(binding.channel, twist);
      } else {
        const zeros = this.zerosLeft.get(binding.channel) ?? 0;
        if (zeros > 0) {
          this.zerosLeft.set(binding.channel, zeros - 1);
          this.publish(binding.channel, zeroTwist());
        }
      }
    }
  };

  private publish(channel: string, twist: Twist) {
    this.ros.publishInput({
      header: stampNow(),
      operator_id: this.getOperatorId(),
      channel,
      twist,
    });
  }
}

function isTyping(event: KeyboardEvent): boolean {
  const target = event.target as HTMLElement | null;
  if (!target) return false;
  return (
    target.tagName === "INPUT" ||
    target.tagName === "TEXTAREA" ||
    target.isContentEditable
  );
}
