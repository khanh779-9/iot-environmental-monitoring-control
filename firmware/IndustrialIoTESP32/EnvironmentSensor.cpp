#include "EnvironmentSensor.h"

void EnvironmentSensor::begin(uint8_t pin, uint8_t type) {
  _dht = new DHT(pin, type);
  _dht->begin();
}

bool EnvironmentSensor::read(TelemetryData& outData) {
  if (!_dht) return false;

  float humidity = _dht->readHumidity();
  float temperature = _dht->readTemperature(); // độ C

  if (isnan(humidity) || isnan(temperature)) {
    return false;
  }

  outData.temperature = temperature;
  outData.humidity = humidity;
  return true;
}
