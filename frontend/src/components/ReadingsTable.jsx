import React from 'react';

export default function ReadingsTable({ data }) {
  if (!data || data.length === 0) {
    return <div className="empty-state">Chưa có bản ghi nào.</div>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Thời gian</th>
          <th>Thiết bị</th>
          <th>Nhiệt độ</th>
          <th>Độ ẩm</th>
          <th>Relay</th>
        </tr>
      </thead>
      <tbody>
        {data.slice(0, 20).map((r) => (
          <tr key={r.id}>
            <td>{new Date(r.created_at).toLocaleString('vi-VN')}</td>
            <td>{r.device_id}</td>
            <td className="numeric">{r.temperature.toFixed(1)} °C</td>
            <td className="numeric">{r.humidity.toFixed(1)} %</td>
            <td className="numeric">{r.relay_state ? 'BẬT' : 'TẮT'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
