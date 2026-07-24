namespace Esp32Monitor.Api.Models;

public sealed record ReadingDto(
    int Id,
    string DeviceId,
    double Temperature,
    double Humidity,
    int RelayState,
    DateTime CreatedAt);
