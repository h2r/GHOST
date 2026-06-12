import { useEffect, useRef, useState } from "react";

import { startWhep, type WhepSession } from "../video/whep";

type StreamStatus = "connecting" | "live" | "error";

const RETRY_DELAY_MS = 3000;

interface Props {
  whepUrl: string;
  focused: boolean;
}

/** The WebRTC scene stream (WHEP from MediaMTX), full screen with
 * auto-reconnect. While there's no stream the panel is just the plain
 * background. */
export default function VideoPanel({ whepUrl, focused }: Props) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [status, setStatus] = useState<StreamStatus>("connecting");

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    let cancelled = false;
    let session: WhepSession | null = null;
    let retryTimer: number | null = null;

    const retry = () => {
      if (cancelled || retryTimer !== null) return;
      session?.close();
      session = null;
      setStatus("error");
      retryTimer = window.setTimeout(() => {
        retryTimer = null;
        connect();
      }, RETRY_DELAY_MS);
    };

    const connect = async () => {
      if (cancelled) return;
      setStatus("connecting");
      try {
        session = await startWhep(whepUrl, video, retry);
      } catch {
        retry();
      }
    };

    const onPlaying = () => setStatus("live");
    video.addEventListener("playing", onPlaying);
    connect();

    return () => {
      cancelled = true;
      video.removeEventListener("playing", onPlaying);
      if (retryTimer !== null) window.clearTimeout(retryTimer);
      session?.close();
    };
  }, [whepUrl]);

  return (
    <section className="video-panel" aria-label="scene video">
      <video
        ref={videoRef}
        className="video-stream"
        style={{ opacity: status === "live" ? 1 : 0 }}
        autoPlay
        muted
        playsInline
      />
      {!focused && (
        <div className="video-blur-overlay">
          <span>INPUT SUSPENDED — CLICK TO RESUME</span>
        </div>
      )}
    </section>
  );
}
