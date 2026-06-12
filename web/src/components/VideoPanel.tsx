interface Props {
  focused: boolean;
}

/** Placeholder for the WebRTC scene stream (milestone 3). */
export default function VideoPanel({ focused }: Props) {
  return (
    <section className="video-panel corners" aria-label="scene video">
      <div className="video-noise" />
      <div className="video-center">
        <div className="video-nosignal">NO SIGNAL</div>
        <div className="video-pending">awaiting scene video link</div>
      </div>
      {!focused && (
        <div className="video-blur-overlay">
          <span>INPUT SUSPENDED — CLICK TO RESUME</span>
        </div>
      )}
    </section>
  );
}
