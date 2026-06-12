import delCap from "../assets/keys/DEL.png";

interface Props {
  flash: boolean;
  onActivate: () => void;
}

/** E-stop cap. Click — or press Delete — to zero both channels and assert
 * each driver's gentle e-stop (latched robot-side until estop/release). */
export default function StopKey({ flash, onActivate }: Props) {
  return (
    <button
      type="button"
      className={flash ? "stop-key stop-key-flash" : "stop-key"}
      onClick={onActivate}
      aria-label="e-stop both robots"
    >
      <img src={delCap} alt="" draggable={false} className="keycap stop-cap" />
      <span className="key-label">Stop</span>
    </button>
  );
}
