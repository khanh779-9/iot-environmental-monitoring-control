import React, { useState } from 'react';

export default function RelayToggle({ state, online, onToggle }) {
  const [pending, setPending] = useState(false);

  const handleClick = async () => {
    setPending(true);
    try {
      await onToggle(!state);
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="panel relay-panel">
      <h2>Điều khiển Relay</h2>
      <div className="relay-row">
        <div>
          <div
            className="relay-state"
            style={{ color: state ? 'var(--accent-online)' : 'var(--text-muted)' }}
          >
            {state ? 'ĐANG BẬT' : 'ĐANG TẮT'}
          </div>
          <div className="timestamp">
            {online ? 'Thiết bị trực tuyến' : 'Thiết bị offline - không thể điều khiển'}
          </div>
        </div>
        <button
          className={`relay-switch ${state ? 'on' : ''}`}
          onClick={handleClick}
          disabled={pending || !online}
          aria-label="Bật/tắt relay"
        >
          <span className="knob" />
        </button>
      </div>
    </div>
  );
}
