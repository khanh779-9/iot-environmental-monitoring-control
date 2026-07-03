#pragma once
#include <Arduino.h>

class NetworkManager {
public:
  void begin(const char* ssid, const char* password);
  bool ensureConnected();
  bool isConnected() const;

private:
  const char* _ssid = nullptr;
  const char* _password = nullptr;
};
