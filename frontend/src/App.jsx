import React, { useEffect, useState, useCallback } from 'react';
import GaugeCard from './components/GaugeCard.jsx';
import HistoryChart from './components/HistoryChart.jsx';
import ReadingsTable from './components/ReadingsTable.jsx';
import RelayToggle from './components/RelayToggle.jsx';
import { fetchLatest, fetchHistory, fetchDevice, setRelay } from './api.js';

const DEVICE_ID = import.meta.env.VITE_DEVICE_ID || 'esp32-01';
const POLL_INTERVAL_MS = 15000;

export default function App() {
  const [latest, setLatest] = useState(null);
  const [history, setHistory] = useState([]);
  const [device, setDevice] = useState(null);
  const [error, setError] = useState(null);

  const loadData = useCallback(async () => {
    try {
      const [latestData, historyData, deviceData] = await Promise.all([
        fetchLatest(DEVICE_ID),
        fetchHistory(DEVICE_ID, 100),
        fetchDevice(DEVICE_ID),
      ]);
      setLatest(latestData);
      setHistory(historyData);
      setDevice(deviceData);
      setError(null);
    } catch (err) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    loadData();
    const id = setInterval(loadData, POLL_INTERVAL_MS);
    return () => clearInterval(id);
  }, [loadData]);

  const isOnline = !!device?.online;

  const handleToggleRelay = async (nextState) => {
    await setRelay(DEVICE_ID, nextState);
    // Cập nhật lạc quan trên UI, rồi đồng bộ lại thật ở lần poll kế (ESP32 cần chút thời gian xác nhận qua MQTT)
    setDevice((d) => (d ? { ...d, relay_state: nextState ? 1 : 0 } : d));
    setTimeout(loadData, 1500);
  };

  return (
    <div className="app">
      <header className="app-header">
        <div>
          <span className="eyebrow">Trạm quan trắc &middot; {DEVICE_ID}</span>
          <h1>Nhiệt độ &amp; Độ ẩm môi trường</h1>
        </div>
        <span className="status-pill">
          <span className={`status-dot ${isOnline ? 'online' : ''}`} />
          {isOnline ? 'Thiết bị đang hoạt động' : 'Thiết bị offline'}
        </span>
      </header>

      {error && <div className="panel">Lỗi: {error}. Kiểm tra backend & MQTT broker đã chạy chưa.</div>}

      <div className="gauge-row">
        <GaugeCard
          label="Nhiệt độ"
          value={latest?.temperature}
          unit="°C"
          min={0}
          max={50}
          color="var(--accent-thermal)"
          timestamp={latest?.created_at}
        />
        <GaugeCard
          label="Độ ẩm"
          value={latest?.humidity}
          unit="%"
          min={0}
          max={100}
          color="var(--accent-humidity)"
          timestamp={latest?.created_at}
        />
      </div>

      <RelayToggle state={!!device?.relay_state} online={isOnline} onToggle={handleToggleRelay} />

      <div className="panel">
        <h2>Lịch sử 100 lần đo gần nhất</h2>
        <HistoryChart data={history} />
      </div>

      <div className="panel">
        <h2>Bản ghi gần đây</h2>
        <ReadingsTable data={history} />
      </div>
    </div>
  );
}
