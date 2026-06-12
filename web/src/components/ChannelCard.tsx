import { ANGULAR_SPEED, LINEAR_SPEED, type ChannelBinding } from "../config";
import type { ChannelState } from "../ros/messages";

interface Props {
  binding: ChannelBinding;
  index: number;
  state: ChannelState | undefined;
  heldKeys: Set<string>;
  selfId: string;
}

export default function ChannelCard({
  binding,
  index,
  state,
  heldKeys,
  selfId,
}: Props) {
  const fused = state?.fused;
  const operators = state?.operators ?? [];

  return (
    <section
      className={`channel-card corners robot-${index}`}
      style={{ animationDelay: `${120 + index * 90}ms` }}
    >
      <header className="channel-head">
        <span className="channel-name">{binding.label}</span>
        <span className="channel-topic">{binding.channel}</span>
        <span className="keycaps">
          {binding.caps.map((cap) => (
            <kbd
              key={cap.code}
              className={heldKeys.has(cap.code) ? "cap cap-held" : "cap"}
            >
              {cap.glyph}
            </kbd>
          ))}
        </span>
      </header>

      <div className="fused">
        <CommandBar label="VX" value={fused?.linear.x ?? 0} max={LINEAR_SPEED} unit="m/s" />
        <CommandBar label="ΩZ" value={fused?.angular.z ?? 0} max={ANGULAR_SPEED} unit="rad/s" />
      </div>

      <div className="operators">
        {operators.length === 0 && (
          <div className="operators-empty">no operators on channel</div>
        )}
        {operators.map((op) => (
          <div
            key={op.operator_id}
            className={op.active ? "op-row" : "op-row op-stale"}
          >
            <span className="op-id">
              {op.operator_id}
              {op.operator_id === selfId && <em className="op-you">you</em>}
            </span>
            <span className="op-weight">
              <span
                className="op-weight-fill"
                style={{ width: `${Math.round(op.weight * 100)}%` }}
              />
            </span>
            <span className="op-age">
              {op.active ? `${Math.round(op.age * 1000)}ms` : "stale"}
            </span>
          </div>
        ))}
      </div>
    </section>
  );
}

function CommandBar({
  label,
  value,
  max,
  unit,
}: {
  label: string;
  value: number;
  max: number;
  unit: string;
}) {
  const frac = Math.max(-1, Math.min(1, value / max));
  const pct = Math.abs(frac) * 50;
  return (
    <div className="cmdbar">
      <span className="cmdbar-label">{label}</span>
      <div className="cmdbar-track">
        <span className="cmdbar-zero" />
        <span
          className="cmdbar-fill"
          style={
            frac >= 0
              ? { left: "50%", width: `${pct}%` }
              : { left: `${50 - pct}%`, width: `${pct}%` }
          }
        />
      </div>
      <span className="cmdbar-value">
        {value >= 0 ? "+" : "−"}
        {Math.abs(value).toFixed(2)} {unit}
      </span>
    </div>
  );
}
