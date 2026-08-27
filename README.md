# IndustrialIoTESP32 - Giám sát Nhiệt độ/Độ ẩm & Điều khiển Relay

Kiến trúc:

```
ESP32 (DHT + Relay) --MQTT publish telemetry--> Broker (Mosquitto) --subscribe--> Backend --SQL--> MySQL
ESP32               <--MQTT relay/set-- Broker <--publish-- Backend <--REST POST-- Frontend (nút bật/tắt)
```

ESP32 và Backend đều là **client** của MQTT broker, không gọi trực tiếp lẫn nhau — broker đứng giữa.

## 0. Cài MQTT Broker (Mosquitto)

Cần 1 broker chạy trên máy chủ (có thể chung máy với backend). Cách nhanh nhất với Docker:

```bash
docker run -d --name mosquitto -p 1883:1883 eclipse-mosquitto
```

Hoặc cài trực tiếp (Ubuntu/Debian):

```bash
sudo apt install mosquitto mosquitto-clients
sudo systemctl enable --now mosquitto
```

Mặc định Mosquitto không yêu cầu auth (chỉ nên dùng vậy trong mạng LAN tin cậy). Muốn bật user/password thì cấu hình `password_file` trong `mosquitto.conf`, rồi điền `MQTT_USER`/`MQTT_PASSWORD` ở cả backend `.env` và firmware `secrets.h`.

## 1. Database (MySQL)

```bash
mysql -u root -p < database/schema.sql
```

Tạo database `esp32_monitor` với 2 bảng:

- `readings` — lịch sử từng lần đo (temperature, humidity, relay_state, created_at)
- `devices` — trạng thái hiện tại từng thiết bị (relay_state, online, last_seen)

## 2. Backend

```bash
cd backend
cp .env.example .env    # sửa DB_*, MQTT_HOST cho đúng môi trường
npm install
npm run dev
```

Backend vừa chạy REST API (cho frontend) vừa là MQTT client (nhận data từ ESP32, gửi lệnh relay).

| Method | Endpoint                   | Mô tả                                                          |
| ------ | -------------------------- | ---------------------------------------------------------------- |
| GET    | `/api/readings`          | Lịch sử đo (query:`device_id`, `limit`, `from`, `to`) |
| GET    | `/api/readings/latest`   | Bản ghi mới nhất                                              |
| GET    | `/api/devices`           | Danh sách thiết bị + trạng thái                             |
| GET    | `/api/devices/:id`       | Trạng thái 1 thiết bị (relay, online, last_seen)             |
| POST   | `/api/devices/:id/relay` | Gửi lệnh bật/tắt relay — body`{"state": true}`            |

## 3. Frontend (pnpm + React)

```bash
cd frontend
cp .env.example .env
pnpm install
pnpm dev
```

Mở `http://localhost:5173`. Dashboard gồm: 2 đồng hồ đo (nhiệt độ/độ ẩm), nút bật/tắt relay, biểu đồ lịch sử, bảng dữ liệu — tự động polling mỗi 15 giây.

## 4. Firmware ESP32 (Arduino IDE)

Thư viện cần cài qua Library Manager:

- **DHT sensor library** (Adafruit) + **Adafruit Unified Sensor**
- **PubSubClient** (Nick O'Leary)
- **ArduinoJson** (Benoit Blanchon)

Các bước:

1. Mở thư mục `firmware/IndustrialIoTESP32/` bằng Arduino IDE (mở file `.ino`, các file `.h`/`.cpp` cùng thư mục sẽ tự load thành các tab).
2. Copy `secrets.example.h` → `secrets.h`, điền `WIFI_SSID`, `WIFI_PASSWORD`, `MQTT_HOST` (IP máy chạy Mosquitto/backend).
3. Mở `AppConfig.h`, kiểm tra/sửa: `DEVICE_ID`, `DHT_PIN`, `DHT_TYPE` (đổi `DHT22` → `DHT11` nếu cần), `RELAY_PIN`, `RELAY_ACTIVE_LOW`.
4. Chọn board **ESP32 Dev Module**, đúng cổng COM, nhấn Upload.
5. Mở Serial Monitor (baud 115200) để xem log kết nối WiFi/MQTT và log đo/gửi dữ liệu.

### Đấu nối

| Linh kiện   | Chân | ESP32                                                            |
| ------------ | ----- | ---------------------------------------------------------------- |
| DHT11/DHT22  | VCC   | 3.3V                                                             |
| DHT11/DHT22  | GND   | GND                                                              |
| DHT11/DHT22  | DATA  | GPIO4 (theo`DHT_PIN`)                                          |
| Module Relay | VCC   | 5V (hầu hết module relay cần 5V để hoạt động ổn định) |
| Module Relay | GND   | GND                                                              |
| Module Relay | IN    | GPIO26 (theo`RELAY_PIN`)                                       |

DHT trần (không breakout) cần điện trở pull-up 10kΩ giữa VCC-DATA. Module relay 1 kênh phổ biến kích ở mức thấp (LOW = đóng) — đã set `RELAY_ACTIVE_LOW = true` trong `AppConfig.h`, đổi thành `false` nếu module bạn ngược lại.

## 5. Luồng dữ liệu / MQTT topic

Với `DEVICE_ID = esp32-01`, `MQTT_TOPIC_PREFIX = industrialiot`:

| Topic                                  | Ai publish  | Ai subscribe | Payload                                                |
| -------------------------------------- | ----------- | ------------ | ------------------------------------------------------ |
| `industrialiot/esp32-01/telemetry`   | ESP32       | Backend      | `{"temperature":28.5,"humidity":65.2,"relay":false}` |
| `industrialiot/esp32-01/relay/set`   | Backend     | ESP32        | `{"state":true}`                                     |
| `industrialiot/esp32-01/relay/state` | ESP32       | Backend      | `{"state":true}` (retained)                          |
| `industrialiot/esp32-01/status`      | ESP32 (LWT) | Backend      | `"online"` / `"offline"` (retained)                |

`status` dùng MQTT **Last Will and Testament**: nếu ESP32 mất kết nối đột ngột (rớt mạng, mất điện), broker tự động publish `"offline"` thay ESP32 — nhờ vậy backend luôn biết chính xác thiết bị còn sống hay không mà không cần polling.

## Bảo mật / mở rộng 

- Bật username/password cho Mosquitto khi dùng ngoài LAN tin cậy; cân nhắc TLS (port 8883) nếu ESP32 kết nối qua Internet.
- Nhiều thiết bị: mỗi board đặt `DEVICE_ID` khác nhau — mọi query/route đã hỗ trợ sẵn `device_id`, chỉ cần thêm dropdown chọn thiết bị ở frontend.
- Muốn cảnh báo tự động (SMS/email/Telegram khi nhiệt độ vượt ngưỡng): thêm logic kiểm tra trong `handleTelemetry()` ở `backend/src/mqtt.js`.
