import blankCap from "../assets/keys/BLANK.png";

interface Props {
  flash: boolean;
  onActivate: () => void;
}

/** Force-stop cap (blank SimpleKeys key labeled STOP). Click — or press
 * Delete — to zero both channels and call each driver's stop service. */
export default function StopKey({ flash, onActivate }: Props) {
  return (
    <button
      type="button"
      className={flash ? "stop-key stop-key-flash" : "stop-key"}
      onClick={onActivate}
      aria-label="force stop both robots"
    >
      <img src={blankCap} alt="" draggable={false} className="keycap stop-cap" />
      <span className="stop-text">STOP</span>
      <span className="stop-sublabel">del · force stop</span>
    </button>
  );
}
