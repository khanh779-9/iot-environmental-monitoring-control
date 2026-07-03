#pragma once
#include <Arduino.h>

class RelayController {
public:
  void begin(uint8_t pin, bool activeLow);
  void setState(bool on);
  bool getState() const;

private:
  uint8_t _pin = 0;
  bool _activeLow = true;
  bool _state = false;
};
