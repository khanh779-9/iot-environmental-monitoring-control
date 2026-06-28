#pragma once
// Copy file này thành secrets.h rồi điền giá trị thật.
// secrets.h phải nằm trong .gitignore, KHÔNG commit lên git.

#define WIFI_SSID       "TEN_WIFI"
#define WIFI_PASSWORD   "MAT_KHAU_WIFI"

#define MQTT_HOST       "192.168.1.100"   // IP máy chạy Mosquitto/backend
#define MQTT_PORT       1883
#define MQTT_USER       ""                // để trống nếu broker không yêu cầu auth
#define MQTT_PASSWORD   ""
