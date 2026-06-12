import { useEffect, useMemo, useRef, useState } from "react";

import { BINDINGS, defaultOperatorId, defaultRosbridgeUrl } from "./config";
import { DriveInputEngine } from "./control/keyboard";
import { RosConsole, type ConnectionStatus } from "./ros/connection";
import type { UiState } from "./ros/messages";

import ChannelCard from "./components/ChannelCard";
import TopBar from "./components/TopBar";
import VideoPanel from "./components/VideoPanel";

export default function App() {
  const [status, setStatus] = useState<ConnectionStatus>("closed");
  const [uiState, setUiState] = useState<UiState | null>(null);
  const [heldKeys, setHeldKeys] = useState<Set<string>>(new Set());
  const [focused, setFocused] = useState(document.hasFocus());
  const [operatorId, setOperatorId] = useState(defaultOperatorId);
  const [url, setUrl] = useState(defaultRosbridgeUrl);

  const operatorIdRef = useRef(operatorId);
  operatorIdRef.current = operatorId;

  const ros = useMemo(() => new RosConsole(), []);
  const engine = useMemo(
    () => new DriveInputEngine(ros, () => operatorIdRef.current),
    [ros],
  );

  useEffect(() => {
    ros.onStatus = setStatus;
    ros.onUiState = setUiState;
    ros.connect(url);
    return () => ros.disconnect();
  }, [ros, url]);

  useEffect(() => {
    engine.onKeysChanged = setHeldKeys;
    engine.start();
    return () => engine.stop();
  }, [engine]);

  useEffect(() => {
    const onFocus = () => setFocused(true);
    const onBlur = () => setFocused(false);
    window.addEventListener("focus", onFocus);
    window.addEventListener("blur", onBlur);
    return () => {
      window.removeEventListener("focus", onFocus);
      window.removeEventListener("blur", onBlur);
    };
  }, []);

  const applyOperatorId = (id: string) => {
    const trimmed = id.trim();
    if (!trimmed) return;
    setOperatorId(trimmed);
    localStorage.setItem("ghost.operatorId", trimmed);
  };

  const applyUrl = (next: string) => {
    const trimmed = next.trim();
    if (!trimmed) return;
    setUrl(trimmed);
    localStorage.setItem("ghost.rosbridgeUrl", trimmed);
  };

  return (
    <div className="console">
      <TopBar
        status={status}
        url={url}
        operatorId={operatorId}
        onUrlChange={applyUrl}
        onOperatorIdChange={applyOperatorId}
      />

      <main className="console-main">
        <VideoPanel focused={focused} />

        <aside className="channel-stack">
          {BINDINGS.map((binding, index) => (
            <ChannelCard
              key={binding.channel}
              binding={binding}
              index={index}
              state={uiState?.channels.find((c) => c.channel === binding.channel)}
              heldKeys={heldKeys}
              selfId={operatorId}
            />
          ))}
        </aside>
      </main>

      <footer className="console-footer">
        <span>
          <b>W A S D</b> — SPOT&ensp;·&ensp;<b>▲ ◀ ▼ ▶</b> — SPOT2
        </span>
        <span className="footer-note">
          inputs zero on key release & window blur
        </span>
      </footer>
    </div>
  );
}
