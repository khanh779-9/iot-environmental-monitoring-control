#pragma once
#include <DHT.h>
#include "TelemetryData.h"

class EnvironmentSensor {
public:
  void begin(uint8_t pin, uint8_t type);
  // Đọc nhiệt độ/độ ẩm vào outData, trả về false nếu đọc lỗi (không chạm relayState)
  bool read(TelemetryData& outData);

private:
  DHT* _dht = nullptr;
};
