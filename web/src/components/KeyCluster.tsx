import type { ChannelBinding } from "../config";

interface Props {
  binding: ChannelBinding;
  heldKeys: Set<string>;
}

function keyImage(name: string): string {
  return new URL(`../assets/keys/${name}`, import.meta.url).href;
}

/** T-shaped keycap cluster (one top key over three bottom keys) with the
 * robot's name underneath. Held keys swap to the pack's pressed frame. */
export default function KeyCluster({ binding, heldKeys }: Props) {
  const [top, left, middle, right] = binding.caps;
  return (
    <div className="key-cluster">
      <div className="key-grid">
        <Cap cap={top} held={heldKeys.has(top.code)} style={{ gridArea: "1 / 2" }} />
        <Cap cap={left} held={heldKeys.has(left.code)} style={{ gridArea: "2 / 1" }} />
        <Cap cap={middle} held={heldKeys.has(middle.code)} style={{ gridArea: "2 / 2" }} />
        <Cap cap={right} held={heldKeys.has(right.code)} style={{ gridArea: "2 / 3" }} />
      </div>
      <span className="key-label">{binding.label}</span>
    </div>
  );
}

function Cap({
  cap,
  held,
  style,
}: {
  cap: { code: string; img: string };
  held: boolean;
  style: React.CSSProperties;
}) {
  return (
    <img
      src={keyImage(held ? cap.img.replace(/\.png$/, ".pressed.png") : cap.img)}
      alt={cap.code}
      draggable={false}
      className="keycap"
      style={style}
    />
  );
}
