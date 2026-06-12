import type { ChannelBinding } from "../config";

interface Props {
  binding: ChannelBinding;
  heldKeys: Set<string>;
  /** "a" (cyan) or "b" (amber) accent for the robot sublabel and key glow */
  accent: "a" | "b";
}

function keyImage(name: string): string {
  return new URL(`../assets/keys/${name}`, import.meta.url).href;
}

/** T-shaped keycap cluster (one top key over three bottom keys) with the
 * robot's name underneath. Caps light up while their key is held. */
export default function KeyCluster({ binding, heldKeys, accent }: Props) {
  const [top, left, middle, right] = binding.caps;
  return (
    <div className={`key-cluster accent-${accent}`}>
      <div className="key-grid">
        <Cap cap={top} held={heldKeys.has(top.code)} style={{ gridArea: "1 / 2" }} />
        <Cap cap={left} held={heldKeys.has(left.code)} style={{ gridArea: "2 / 1" }} />
        <Cap cap={middle} held={heldKeys.has(middle.code)} style={{ gridArea: "2 / 2" }} />
        <Cap cap={right} held={heldKeys.has(right.code)} style={{ gridArea: "2 / 3" }} />
      </div>
      <div className="key-cluster-label">{binding.label}</div>
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
      src={keyImage(cap.img)}
      alt={cap.code}
      draggable={false}
      className={held ? "keycap keycap-held" : "keycap"}
      style={style}
    />
  );
}
