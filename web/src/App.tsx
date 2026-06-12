import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import {
  BINDINGS,
  STOP_KEY,
  defaultOperatorId,
  defaultRosbridgeUrl,
  defaultWhepUrl,
} from "./config";
import { DriveInputEngine } from "./control/keyboard";
import { RosConsole, type ConnectionStatus } from "./ros/connection";
import { stampNow, zeroTwist } from "./ros/messages";

import KeyCluster from "./components/KeyCluster";
import StopKey from "./components/StopKey";
import TopBar from "./components/TopBar";
import VideoPanel from "./components/VideoPanel";

export default function App() {
  const [status, setStatus] = useState<ConnectionStatus>("closed");
  const [heldKeys, setHeldKeys] = useState<Set<string>>(new Set());
  const [focused, setFocused] = useState(document.hasFocus());
  const [stopFlash, setStopFlash] = useState(false);
  const [operatorId, setOperatorId] = useState(defaultOperatorId);
  const [url, setUrl] = useState(defaultRosbridgeUrl);
  const [whepUrl, setWhepUrl] = useState(defaultWhepUrl);

  const operatorIdRef = useRef(operatorId);
  operatorIdRef.current = operatorId;

  const ros = useMemo(() => new RosConsole(), []);
  const engine = useMemo(
    () => new DriveInputEngine(ros, () => operatorIdRef.current),
    [ros],
  );

  useEffect(() => {
    ros.onStatus = setStatus;
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

  const forceStop = useCallback(() => {
    engine.releaseAll();
    for (const binding of BINDINGS) {
      ros.publishInput({
        header: stampNow(),
        operator_id: operatorIdRef.current,
        channel: binding.channel,
        twist: zeroTwist(),
      });
      const robot = binding.channel.split("/")[0];
      ros.callService(`/${robot}/stop`, "std_srvs/srv/Trigger");
    }
    setStopFlash(true);
    window.setTimeout(() => setStopFlash(false), 350);
  }, [ros, engine]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.code !== STOP_KEY) return;
      event.preventDefault();
      forceStop();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [forceStop]);

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

  const applyWhepUrl = (next: string) => {
    const trimmed = next.trim();
    if (!trimmed) return;
    setWhepUrl(trimmed);
    localStorage.setItem("ghost.whepUrl", trimmed);
  };

  return (
    <div className="console-fs">
      <VideoPanel whepUrl={whepUrl} focused={focused} />

      <TopBar
        status={status}
        url={url}
        whepUrl={whepUrl}
        operatorId={operatorId}
        onUrlChange={applyUrl}
        onWhepUrlChange={applyWhepUrl}
        onOperatorIdChange={applyOperatorId}
      />

      <StopKey flash={stopFlash} onActivate={forceStop} />

      <div className="cluster-left">
        <KeyCluster binding={BINDINGS[0]} heldKeys={heldKeys} accent="a" />
      </div>
      <div className="cluster-right">
        <KeyCluster binding={BINDINGS[1]} heldKeys={heldKeys} accent="b" />
      </div>
    </div>
  );
}
