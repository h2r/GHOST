import { useEffect, useState } from "react";

import type { ConnectionStatus } from "../ros/connection";

const STATUS_LABEL: Record<ConnectionStatus, string> = {
  connecting: "LINKING",
  connected: "LINKED",
  closed: "OFFLINE",
  error: "LINK ERROR",
};

interface Props {
  status: ConnectionStatus;
  url: string;
  whepUrl: string;
  operatorId: string;
  onUrlChange: (url: string) => void;
  onWhepUrlChange: (url: string) => void;
  onOperatorIdChange: (id: string) => void;
}

export default function TopBar({
  status,
  url,
  whepUrl,
  operatorId,
  onUrlChange,
  onWhepUrlChange,
  onOperatorIdChange,
}: Props) {
  return (
    <header className="topbar">
      <div className="wordmark">
        <span className="wordmark-ghost">GHOST</span>
        <span className="wordmark-sub">multi-operator console</span>
      </div>

      <div className="topbar-fields">
        <EditableField
          label="rosbridge"
          value={url}
          onCommit={onUrlChange}
          width={26}
        />
        <EditableField
          label="video"
          value={whepUrl}
          onCommit={onWhepUrlChange}
          width={30}
        />
        <EditableField
          label="operator"
          value={operatorId}
          onCommit={onOperatorIdChange}
          width={14}
        />
        <div className={`link-status link-${status}`}>
          <span className="link-dot" />
          {STATUS_LABEL[status]}
        </div>
      </div>
    </header>
  );
}

function EditableField({
  label,
  value,
  onCommit,
  width,
}: {
  label: string;
  value: string;
  onCommit: (value: string) => void;
  width: number;
}) {
  const [draft, setDraft] = useState(value);
  useEffect(() => setDraft(value), [value]);

  return (
    <label className="field">
      <span className="field-label">{label}</span>
      <input
        className="field-input"
        style={{ width: `${width}ch` }}
        value={draft}
        spellCheck={false}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={() => onCommit(draft)}
        onKeyDown={(e) => {
          if (e.key === "Enter") (e.target as HTMLInputElement).blur();
        }}
      />
    </label>
  );
}
