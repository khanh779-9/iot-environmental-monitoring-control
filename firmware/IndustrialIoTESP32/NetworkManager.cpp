#include "NetworkManager.h"
#include "AppConfig.h"
#include <WiFi.h>

void NetworkManager::begin(const char* ssid, const char* password) {
  _ssid = ssid;
  _password = password;
  WiFi.mode(WIFI_STA);
}

bool NetworkManager::ensureConnected() {
  if (WiFi.status() == WL_CONNECTED) return true;

  Serial.printf("Đang kết nối WiFi \"%s\"", _ssid);
  WiFi.begin(_ssid, _password);

  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - start < WIFI_CONNECT_TIMEOUT_MS) {
    delay(400);
    Serial.print(".");
  }
  Serial.println();

  if (WiFi.status() == WL_CONNECTED) {
    Serial.print("WiFi đã kết nối. IP: ");
    Serial.println(WiFi.localIP());
    return true;
  }

  Serial.println("Kết nối WiFi thất bại, sẽ thử lại ở vòng lặp sau.");
  return false;
}

bool NetworkManager::isConnected() const {
  return WiFi.status() == WL_CONNECTED;
}
