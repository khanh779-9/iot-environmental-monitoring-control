#include "RelayController.h"

void RelayController::begin(uint8_t pin, bool activeLow) {
  _pin = pin;
  _activeLow = activeLow;
  pinMode(_pin, OUTPUT);
  setState(false); // mặc định TẮT khi khởi động, tránh relay bật ngẫu nhiên khi mất điện
}

void RelayController::setState(bool on) {
  _state = on;
  bool level = _activeLow ? !on : on;
  digitalWrite(_pin, level ? HIGH : LOW);
}

bool RelayController::getState() const {
  return _state;
}
