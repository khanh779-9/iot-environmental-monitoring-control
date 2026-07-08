#pragma once
#include <WiFiClient.h>
#include <PubSubClient.h>
#include "TelemetryData.h"

class MqttManager {
public:
  using RelayCommandCallback = void (*)(bool state);

  MqttManager();

  void begin(const char* deviceId);
  void loop();
  bool ensureConnected();

  void publishTelemetry(const TelemetryData& data);
  void publishRelayState(bool state);
  void onRelayCommand(RelayCommandCallback callback);

private:
  WiFiClient _wifiClient;
  PubSubClient _client;

  String _deviceId;
  String _telemetryTopic;
  String _relaySetTopic;
  String _relayStateTopic;
  String _statusTopic;

  RelayCommandCallback _relayCallback = nullptr;

  static MqttManager* _instance; // để callback tĩnh của PubSubClient gọi lại được instance
  static void staticCallback(char* topic, byte* payload, unsigned int length);
  void handleMessage(char* topic, byte* payload, unsigned int length);
};
