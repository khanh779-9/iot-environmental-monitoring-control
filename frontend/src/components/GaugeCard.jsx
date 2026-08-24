import React from 'react';

// Vẽ 1 cung tròn (arc) SVG từ startAngle đến endAngle (độ), bán kính r, tâm (cx, cy)
function describeArc(cx, cy, r, startAngle, endAngle) {
  const toRad = (deg) => ((deg - 90) * Math.PI) / 180;
  const start = {
    x: cx + r * Math.cos(toRad(endAngle)),
    y: cy + r * Math.sin(toRad(endAngle)),
  };
  const end = {
    x: cx + r * Math.cos(toRad(startAngle)),
    y: cy + r * Math.sin(toRad(startAngle)),
  };
  const largeArcFlag = endAngle - startAngle <= 180 ? '0' : '1';
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArcFlag} 0 ${end.x} ${end.y}`;
}

/**
 * label: nhãn (VD "Nhiệt độ")
 * value: giá trị hiện tại
 * unit: đơn vị (VD "°C")
 * min/max: khoảng giá trị để tính cung tròn
 * color: màu accent
 * timestamp: thời điểm đo (ISO string) hoặc null
 */
export default function GaugeCard({ label, value, unit, min, max, color, timestamp }) {
  const hasValue = value !== null && value !== undefined && !Number.isNaN(value);
  const clamped = hasValue ? Math.min(max, Math.max(min, value)) : min;
  const ratio = (clamped - min) / (max - min);
  const sweepAngle = 270; // cung tròn 270 độ (3/4 vòng), giống mặt đồng hồ đo
  const startAngle = -135;
  const endAngle = startAngle + sweepAngle * ratio;

  const r = 46;
  const cx = 55;
  const cy = 55;

  return (
    <div className="gauge-card">
      <svg width="110" height="110" viewBox="0 0 110 110">
        <path
          d={describeArc(cx, cy, r, startAngle, startAngle + sweepAngle)}
          fill="none"
          stroke="var(--hairline)"
          strokeWidth="8"
          strokeLinecap="round"
        />
        {hasValue && (
          <path
            d={describeArc(cx, cy, r, startAngle, endAngle)}
            fill="none"
            stroke={color}
            strokeWidth="8"
            strokeLinecap="round"
          />
        )}
      </svg>
      <div>
        <span className="label">{label}</span>
        <div className="value" style={{ color }}>
          {hasValue ? value.toFixed(1) : '--'}
          <span style={{ fontSize: 16, marginLeft: 4, color: 'var(--text-muted)' }}>{unit}</span>
        </div>
        <div className="timestamp">
          {timestamp ? `Cập nhật: ${new Date(timestamp).toLocaleString('vi-VN')}` : 'Chưa có dữ liệu'}
        </div>
      </div>
    </div>
  );
}
