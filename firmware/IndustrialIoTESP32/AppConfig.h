#pragma once

// ==== Thiết bị ====
#define DEVICE_ID              "esp32-01"

// ==== Chân GPIO ====
#define DHT_PIN                 4
#define DHT_TYPE                DHT22   // đổi thành DHT11 nếu bạn dùng DHT11
#define RELAY_PIN                26
#define RELAY_ACTIVE_LOW        true     // đa số module relay 1 kênh kích ở mức thấp (LOW = đóng)

// ==== Thời gian ====
#define SENSOR_READ_INTERVAL_MS  30000   // đọc & gửi telemetry mỗi 30 giây
#define WIFI_CONNECT_TIMEOUT_MS  15000
#define MQTT_RECONNECT_DELAY_MS  5000

// ==== MQTT ====
// Topic thực tế được build từ prefix này + DEVICE_ID, không cần sửa tay
#define MQTT_TOPIC_PREFIX       "industrialiot"
