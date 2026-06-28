#pragma once

// Struct dữ liệu đo được, dùng chung giữa EnvironmentSensor, RelayController và MqttManager
struct TelemetryData {
  float temperature = 0;
  float humidity = 0;
  bool relayState = false;
};
