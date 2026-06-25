-- Database & bảng lưu dữ liệu nhiệt độ / độ ẩm / relay từ ESP32 (qua MQTT)
CREATE DATABASE IF NOT EXISTS esp32_monitor
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE esp32_monitor;

-- Lịch sử từng lần đo (telemetry)
CREATE TABLE IF NOT EXISTS readings (
  id          INT AUTO_INCREMENT PRIMARY KEY,
  device_id   VARCHAR(50)  NOT NULL DEFAULT 'esp32-01',
  temperature FLOAT        NOT NULL,
  humidity    FLOAT        NOT NULL,
  relay_state TINYINT(1)   NOT NULL DEFAULT 0,
  created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_device_time (device_id, created_at)
) ENGINE=InnoDB;

-- Trạng thái hiện tại của từng thiết bị (relay, online/offline) - cập nhật liên tục qua MQTT
CREATE TABLE IF NOT EXISTS devices (
  device_id   VARCHAR(50) PRIMARY KEY,
  relay_state TINYINT(1)  NOT NULL DEFAULT 0,
  online      TINYINT(1)  NOT NULL DEFAULT 0,
  last_seen   DATETIME    NULL
) ENGINE=InnoDB;

-- (Tuỳ chọn) user riêng cho app thay vì dùng root
-- CREATE USER 'esp32_app'@'%' IDENTIFIED BY 'doi_mat_khau_nay';
-- GRANT SELECT, INSERT, UPDATE ON esp32_monitor.* TO 'esp32_app'@'%';
-- FLUSH PRIVILEGES;
