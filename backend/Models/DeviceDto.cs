namespace Esp32Monitor.Api.Models;

public sealed record DeviceDto(
    string DeviceId,
    int RelayState,
    int Online,
    DateTime? LastSeen);
