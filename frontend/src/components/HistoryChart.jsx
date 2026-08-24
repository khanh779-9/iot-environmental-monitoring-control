import React from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';

export default function HistoryChart({ data }) {
  if (!data || data.length === 0) {
    return <div className="empty-state">Chưa có dữ liệu lịch sử để vẽ biểu đồ.</div>;
  }

  // API trả mới nhất trước -> đảo lại để trục thời gian tăng dần trái sang phải
  const chartData = [...data].reverse().map((r) => ({
    time: new Date(r.created_at).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
    'Nhiệt độ (°C)': r.temperature,
    'Độ ẩm (%)': r.humidity,
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <LineChart data={chartData} margin={{ top: 4, right: 8, left: -12, bottom: 0 }}>
        <CartesianGrid stroke="#262e38" strokeDasharray="3 3" />
        <XAxis dataKey="time" stroke="#7d8a99" fontSize={11} tickMargin={8} />
        <YAxis stroke="#7d8a99" fontSize={11} />
        <Tooltip
          contentStyle={{
            background: '#141a22',
            border: '1px solid #262e38',
            borderRadius: 8,
            fontSize: 12,
          }}
          labelStyle={{ color: '#7d8a99' }}
        />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        <Line
          type="monotone"
          dataKey="Nhiệt độ (°C)"
          stroke="#f4a261"
          strokeWidth={2}
          dot={false}
        />
        <Line
          type="monotone"
          dataKey="Độ ẩm (%)"
          stroke="#4cc9f0"
          strokeWidth={2}
          dot={false}
        />
      </LineChart>
    </ResponsiveContainer>
  );
}
