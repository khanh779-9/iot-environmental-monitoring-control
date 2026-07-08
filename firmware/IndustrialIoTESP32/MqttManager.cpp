#include "MqttManager.h"
#include "AppConfig.h"
#include "secrets.h"
#include <ArduinoJson.h>

MqttManager* MqttManager::_instance = nullptr;

MqttManager::MqttManager() : _client(_wifiClient) {
  _instance = this;
}

void MqttManager::begin(const char* deviceId) {
  _deviceId = deviceId;
  _telemetryTopic  = String(MQTT_TOPIC_PREFIX) + "/" + _deviceId + "/telemetry";
  _relaySetTopic   = String(MQTT_TOPIC_PREFIX) + "/" + _deviceId + "/relay/set";
  _relayStateTopic = String(MQTT_TOPIC_PREFIX) + "/" + _deviceId + "/relay/state";
  _statusTopic     = String(MQTT_TOPIC_PREFIX) + "/" + _deviceId + "/status";

  _client.setServer(MQTT_HOST, MQTT_PORT);
  _client.setCallback(staticCallback);
}

bool MqttManager::ensureConnected() {
  if (_client.connected()) return true;

  Serial.print("Đang kết nối MQTT broker...");
  String clientId = String("esp32-") + _deviceId;

  // LWT: nếu ESP32 mất kết nối đột ngột, broker tự publish "offline" (retained) cho backend biết
  bool ok;
  if (strlen(MQTT_USER) > 0) {
    ok = _client.connect(clientId.c_str(), MQTT_USER, MQTT_PASSWORD,
                          _statusTopic.c_str(), 1, true, "offline");
  } else {
    ok = _client.connect(clientId.c_str(), nullptr, nullptr,
                          _statusTopic.c_str(), 1, true, "offline");
  }

  if (ok) {
    Serial.println(" thành công.");
    _client.publish(_statusTopic.c_str(), "online", true);
    _client.subscribe(_relaySetTopic.c_str());
  } else {
    Serial.printf(" thất bại, rc=%d. Thử lại sau.\n", _client.state());
  }
  return ok;
}

void MqttManager::loop() {
  _client.loop();
}

void MqttManager::publishTelemetry(const TelemetryData& data) {
  StaticJsonDocument<128> doc;
  doc["temperature"] = data.temperature;
  doc["humidity"] = data.humidity;
  doc["relay"] = data.relayState;

  char buffer[128];
  size_t n = serializeJson(doc, buffer);
  _client.publish(_telemetryTopic.c_str(), buffer, n);
}

void MqttManager::publishRelayState(bool state) {
  StaticJsonDocument<64> doc;
  doc["state"] = state;

  char buffer[64];
  size_t n = serializeJson(doc, buffer);
  _client.publish(_relayStateTopic.c_str(), buffer, n, true); // retained để backend đọc được state cuối khi vừa subscribe
}

void MqttManager::onRelayCommand(RelayCommandCallback callback) {
  _relayCallback = callback;
}

void MqttManager::staticCallback(char* topic, byte* payload, unsigned int length) {
  if (_instance) {
    _instance->handleMessage(topic, payload, length);
  }
}

void MqttManager::handleMessage(char* topic, byte* payload, unsigned int length) {
  if (_relaySetTopic != topic) return;

  StaticJsonDocument<64> doc;
  DeserializationError err = deserializeJson(doc, payload, length);
  if (err) {
    Serial.println("Lỗi parse JSON lệnh relay.");
    return;
  }

  bool state = doc["state"] | false;
  if (_relayCallback) {
    _relayCallback(state);
  }
}
