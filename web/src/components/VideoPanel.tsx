import { useEffect, useRef, useState } from "react";

import { startWhep, type WhepSession } from "../video/whep";

type StreamStatus = "connecting" | "live" | "error";

const RETRY_DELAY_MS = 3000;

interface Props {
  whepUrl: string;
  focused: boolean;
}

/** The WebRTC scene stream (WHEP from MediaMTX), with auto-reconnect. */
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

  const live = status === "live";

  return (
    <section className="video-panel" aria-label="scene video">
      <video
        ref={videoRef}
        className="video-stream"
        style={{ opacity: live ? 1 : 0 }}
        autoPlay
        muted
        playsInline
      />
      {!live && (
        <>
          <div className="video-noise" />
          <div className="video-center">
            <div className="video-nosignal">NO SIGNAL</div>
            <div className="video-pending">
              {status === "connecting" ? "connecting to scene stream" : "stream unreachable — retrying"}
            </div>
            {status === "error" && (
              <div className="video-hint">
                if this persists, open {whepUrl.replace(/\/whep$/, "")} once to
                trust the stream certificate
              </div>
            )}
          </div>
        </>
      )}
      {live && <span className="video-live">LIVE</span>}
      {!focused && (
        <div className="video-blur-overlay">
          <span>INPUT SUSPENDED — CLICK TO RESUME</span>
        </div>
      )}
    </section>
  );
}
