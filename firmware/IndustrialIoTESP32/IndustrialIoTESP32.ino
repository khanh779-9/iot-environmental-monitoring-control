/*
 * IndustrialIoTESP32
 * Đọc nhiệt độ/độ ẩm (DHT11/DHT22), publish qua MQTT, nhận lệnh điều khiển relay qua MQTT.
 *
 * Thư viện cần cài (Arduino IDE > Library Manager):
 *   - DHT sensor library (Adafruit) + Adafruit Unified Sensor
 *   - PubSubClient (Nick O'Leary)
 *   - ArduinoJson (Benoit Blanchon)
 *
 * Trước khi upload: copy secrets.example.h -> secrets.h và điền WiFi/MQTT thật.
 */

#include "AppConfig.h"
#include "secrets.h"
#include "TelemetryData.h"
#include "NetworkManager.h"
#include "MqttManager.h"
#include "EnvironmentSensor.h"
#include "RelayController.h"

NetworkManager networkManager;
MqttManager mqttManager;
EnvironmentSensor environmentSensor;
RelayController relayController;

unsigned long lastReadTime = 0;

// Được MqttManager gọi khi nhận lệnh từ topic industrialiot/<device>/relay/set
void onRelayCommand(bool state) {
  relayController.setState(state);
  mqttManager.publishRelayState(state); // xác nhận lại trạng thái mới cho backend
  Serial.printf("Relay -> %s (theo lệnh từ backend)\n", state ? "BẬT" : "TẮT");
}

void setup() {
  Serial.begin(115200);
  delay(500);

  relayController.begin(RELAY_PIN, RELAY_ACTIVE_LOW);
  environmentSensor.begin(DHT_PIN, DHT_TYPE);

  networkManager.begin(WIFI_SSID, WIFI_PASSWORD);
  networkManager.ensureConnected();

  mqttManager.onRelayCommand(onRelayCommand);
  mqttManager.begin(DEVICE_ID);
  mqttManager.ensureConnected();
  mqttManager.publishRelayState(relayController.getState()); // báo trạng thái relay ban đầu
}

void loop() {
  networkManager.ensureConnected();

  if (networkManager.isConnected()) {
    mqttManager.ensureConnected();
    mqttManager.loop();
  }

  unsigned long now = millis();
  if (now - lastReadTime >= SENSOR_READ_INTERVAL_MS || lastReadTime == 0) {
    lastReadTime = now;

    TelemetryData data;
    if (environmentSensor.read(data)) {
      data.relayState = relayController.getState();
      Serial.printf("Nhiệt độ: %.2f°C | Độ ẩm: %.2f%% | Relay: %s\n",
                     data.temperature, data.humidity, data.relayState ? "BẬT" : "TẮT");
      mqttManager.publishTelemetry(data);
    } else {
      Serial.println("Lỗi đọc cảm biến DHT, bỏ qua lần này.");
    }
  }
}
