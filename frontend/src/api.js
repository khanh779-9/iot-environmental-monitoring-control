const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:3000/api';

export async function fetchLatest(deviceId) {
  const url = new URL(`${API_URL}/readings/latest`);
  if (deviceId) url.searchParams.set('device_id', deviceId);
  const res = await fetch(url);
  if (!res.ok) throw new Error('Không lấy được dữ liệu mới nhất');
  return res.json();
}

export async function fetchHistory(deviceId, limit = 100) {
  const url = new URL(`${API_URL}/readings`);
  if (deviceId) url.searchParams.set('device_id', deviceId);
  url.searchParams.set('limit', limit);
  const res = await fetch(url);
  if (!res.ok) throw new Error('Không lấy được lịch sử dữ liệu');
  return res.json();
}

export async function fetchDevice(deviceId) {
  const res = await fetch(`${API_URL}/devices/${deviceId}`);
  if (!res.ok) throw new Error('Không lấy được trạng thái thiết bị');
  return res.json();
}

export async function setRelay(deviceId, state) {
  const res = await fetch(`${API_URL}/devices/${deviceId}/relay`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ state }),
  });
  if (!res.ok) throw new Error('Không gửi được lệnh điều khiển relay');
  return res.json();
}
