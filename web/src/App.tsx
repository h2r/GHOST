import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import {
  BINDINGS,
  STOP_KEY,
  defaultOperatorId,
  defaultRosbridgeUrl,
  defaultWhepUrl,
} from "./config";
import { DriveInputEngine } from "./control/keyboard";
import { RosConsole } from "./ros/connection";
import { stampNow, zeroTwist } from "./ros/messages";

import KeyCluster from "./components/KeyCluster";
import StopKey from "./components/StopKey";
import VideoPanel from "./components/VideoPanel";

export default function App() {
  const [heldKeys, setHeldKeys] = useState<Set<string>>(new Set());
  const [focused, setFocused] = useState(document.hasFocus());
  const [stopFlash, setStopFlash] = useState(false);

  // No settings UI for now — these come from localStorage / ?ros= ?video=
  // ?op= query parameters (see config.ts).
  const operatorId = useMemo(defaultOperatorId, []);
  const rosbridgeUrl = useMemo(defaultRosbridgeUrl, []);
  const whepUrl = useMemo(defaultWhepUrl, []);

  const operatorIdRef = useRef(operatorId);
  operatorIdRef.current = operatorId;

  const ros = useMemo(() => new RosConsole(), []);
  const engine = useMemo(
    () => new DriveInputEngine(ros, () => operatorIdRef.current),
    [ros],
  );

  useEffect(() => {
    ros.connect(rosbridgeUrl);
    return () => ros.disconnect();
  }, [ros, rosbridgeUrl]);

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

  const eStop = useCallback(() => {
    engine.releaseAll();
    for (const binding of BINDINGS) {
      ros.publishInput({
        header: stampNow(),
        operator_id: operatorIdRef.current,
        channel: binding.channel,
        twist: zeroTwist(),
      });
      // Latched robot-side; resume requires /<robot>/estop/release.
      const robot = binding.channel.split("/")[0];
      ros.callService(`/${robot}/estop/gentle`, "std_srvs/srv/Trigger");
    }
    setStopFlash(true);
    window.setTimeout(() => setStopFlash(false), 350);
  }, [ros, engine]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.code !== STOP_KEY) return;
      event.preventDefault();
      eStop();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [eStop]);

  return (
    <div className="console-fs">
      <VideoPanel whepUrl={whepUrl} focused={focused} />

      <StopKey flash={stopFlash} onActivate={eStop} />

      <div className="cluster-left">
        <KeyCluster binding={BINDINGS[0]} heldKeys={heldKeys} />
      </div>
      <div className="cluster-right">
        <KeyCluster binding={BINDINGS[1]} heldKeys={heldKeys} />
      </div>
    </div>
  );
}
