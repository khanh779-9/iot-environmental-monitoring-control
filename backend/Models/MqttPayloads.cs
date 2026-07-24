namespace Esp32Monitor.Api.Models;

public sealed class TelemetryPayload
{
    public double? Temperature { get; init; }
    public double? Humidity { get; init; }
    public bool Relay { get; init; }
}

public sealed class RelayStatePayload
{
    public bool State { get; init; }
}
