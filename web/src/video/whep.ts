/** Minimal WHEP (WebRTC-HTTP egress) client for the MediaMTX scene stream.
 *
 * Non-trickle: gathers ICE candidates locally, POSTs the complete SDP offer,
 * and applies the answer. Returns a handle that must be closed; connection
 * failures surface through onFailure so the caller can retry.
 */

export interface WhepSession {
  close: () => void;
}

export async function startWhep(
  url: string,
  video: HTMLVideoElement,
  onFailure: () => void,
): Promise<WhepSession> {
  const pc = new RTCPeerConnection();
  let closed = false;

  pc.addTransceiver("video", { direction: "recvonly" });
  pc.ontrack = (event) => {
    video.srcObject = event.streams[0];
  };
  pc.onconnectionstatechange = () => {
    if (closed) return;
    if (pc.connectionState === "failed" || pc.connectionState === "disconnected") {
      onFailure();
    }
  };

  try {
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    await iceGatheringComplete(pc, 2000);

    const response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/sdp" },
      body: pc.localDescription!.sdp,
    });
    if (!response.ok) {
      throw new Error(`WHEP endpoint returned ${response.status}`);
    }
    await pc.setRemoteDescription({ type: "answer", sdp: await response.text() });
  } catch (error) {
    pc.close();
    throw error;
  }

  return {
    close: () => {
      closed = true;
      video.srcObject = null;
      pc.close();
    },
  };
}

function iceGatheringComplete(pc: RTCPeerConnection, timeoutMs: number): Promise<void> {
  if (pc.iceGatheringState === "complete") return Promise.resolve();
  return new Promise((resolve) => {
    const timer = window.setTimeout(resolve, timeoutMs);
    pc.onicegatheringstatechange = () => {
      if (pc.iceGatheringState === "complete") {
        window.clearTimeout(timer);
        resolve();
      }
    };
  });
}
