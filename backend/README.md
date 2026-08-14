# Backend ASP.NET Core

Backend này thay thế hoàn toàn backend Express cũ nhưng giữ nguyên REST API để frontend React không cần sửa.

## Công nghệ

- ASP.NET Core Web API (.NET 10 LTS)
- MySqlConnector
- MQTTnet
- MySQL
- Mosquitto MQTT broker

## Chạy local

Yêu cầu: .NET 10 SDK, MySQL và Mosquitto.

1. Tạo DB bằng `../database/schema.sql`.
2. Sửa `appsettings.json` nếu MySQL/MQTT không chạy ở localhost.
3. Chạy:

```bash
dotnet restore
dotnet run
```

API mặc định chạy tại `http://localhost:3000`.

## Biến môi trường tương thích backend cũ

Có thể override cấu hình bằng các biến:

- `PORT`
- `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME`
- `MQTT_HOST`, `MQTT_PORT`, `MQTT_USER`, `MQTT_PASSWORD`, `MQTT_TOPIC_PREFIX`
- `FRONTEND_ORIGIN`

Lưu ý: ASP.NET Core không tự đọc file `.env`; hãy đặt biến môi trường trong terminal/Docker/IDE hoặc sửa `appsettings.json`.

## API

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/health` | Health check |
| GET | `/api/readings` | Lịch sử; query `device_id`, `limit`, `from`, `to` |
| GET | `/api/readings/latest` | Bản ghi mới nhất |
| GET | `/api/devices` | Danh sách thiết bị |
| GET | `/api/devices/{deviceId}` | Trạng thái thiết bị |
| POST | `/api/devices/{deviceId}/relay` | Gửi lệnh relay qua MQTT |

Body relay:

```json
{ "state": true }
```
