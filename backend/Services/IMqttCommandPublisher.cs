namespace Esp32Monitor.Api.Services;

public interface IMqttCommandPublisher
{
    Task PublishRelayCommandAsync(
        string deviceId,
        bool state,
        CancellationToken cancellationToken = default);
}
