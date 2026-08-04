using System.Text.Json;
using Esp32Monitor.Api.Data;
using Esp32Monitor.Api.Models;
using MQTTnet;

namespace Esp32Monitor.Api.Services;

public sealed class MqttService : BackgroundService, IMqttCommandPublisher
{
    private readonly MonitoringRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _client;
    private readonly MqttClientFactory _factory = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private string TopicPrefix =>
        Environment.GetEnvironmentVariable("MQTT_TOPIC_PREFIX")
        ?? _configuration["Mqtt:TopicPrefix"]
        ?? "industrialiot";

    public MqttService(
        MonitoringRepository repository,
        IConfiguration configuration,
        ILogger<MqttService> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
        _client = _factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += HandleMessageAsync;
        _client.DisconnectedAsync += e =>
        {
            if (e.Exception is null)
            {
                _logger.LogWarning("MQTT đã ngắt kết nối");
            }
            else
            {
                _logger.LogWarning(e.Exception, "MQTT bị ngắt kết nối");
            }

            return Task.CompletedTask;
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await ConnectAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không kết nối được MQTT broker, sẽ thử lại");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task PublishRelayCommandAsync(
        string deviceId,
        bool state,
        CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            throw new InvalidOperationException("MQTT client chưa kết nối tới broker");
        }

        var topic = $"{TopicPrefix}/{deviceId}/relay/set";
        var payload = JsonSerializer.Serialize(new { state }, _jsonOptions);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
        {
            try
            {
                var disconnectOptions = _factory.CreateClientDisconnectOptionsBuilder().Build();
                await _client.DisconnectAsync(disconnectOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi ngắt kết nối MQTT lúc shutdown");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _connectLock.Dispose();
        _client.Dispose();
        base.Dispose();
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_client.IsConnected) return;

            var host = Environment.GetEnvironmentVariable("MQTT_HOST")
                       ?? _configuration["Mqtt:Host"]
                       ?? "localhost";
            var portText = Environment.GetEnvironmentVariable("MQTT_PORT")
                           ?? _configuration["Mqtt:Port"]
                           ?? "1883";
            _ = int.TryParse(portText, out var port);
            if (port <= 0) port = 1883;

            var user = Environment.GetEnvironmentVariable("MQTT_USER")
                       ?? _configuration["Mqtt:Username"];
            var password = Environment.GetEnvironmentVariable("MQTT_PASSWORD")
                           ?? _configuration["Mqtt:Password"];

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId($"esp32-monitor-api-{Guid.NewGuid():N}")
                .WithTcpServer(host, port);

            if (!string.IsNullOrWhiteSpace(user))
            {
                optionsBuilder.WithCredentials(user, password ?? string.Empty);
            }

            await _client.ConnectAsync(optionsBuilder.Build(), cancellationToken);
            _logger.LogInformation("Đã kết nối MQTT broker tại {Host}:{Port}", host, port);
            await SubscribeAsync(cancellationToken);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task SubscribeAsync(CancellationToken cancellationToken)
    {
        var options = _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter($"{TopicPrefix}/+/telemetry")
            .WithTopicFilter($"{TopicPrefix}/+/relay/state")
            .WithTopicFilter($"{TopicPrefix}/+/status")
            .Build();

        await _client.SubscribeAsync(options, cancellationToken);
        _logger.LogInformation("Đã subscribe các topic dưới prefix {TopicPrefix}", TopicPrefix);
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Dạng topic: <prefix>/<deviceId>/telemetry | relay/state | status
        if (parts.Length < 3 || !string.Equals(parts[0], TopicPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var deviceId = parts[1];
        var subtopic = string.Join('/', parts.Skip(2));

        try
        {
            switch (subtopic)
            {
                case "status":
                    await _repository.UpdateOnlineStatusAsync(
                        deviceId,
                        string.Equals(payload.Trim(), "online", StringComparison.OrdinalIgnoreCase));
                    break;

                case "telemetry":
                {
                    var data = JsonSerializer.Deserialize<TelemetryPayload>(payload, _jsonOptions);
                    if (data?.Temperature is not double temperature ||
                        data.Humidity is not double humidity)
                    {
                        _logger.LogWarning("Telemetry không hợp lệ từ {DeviceId}: {Payload}", deviceId, payload);
                        return;
                    }

                    await _repository.InsertTelemetryAsync(
                        deviceId,
                        temperature,
                        humidity,
                        data.Relay);
                    break;
                }

                case "relay/state":
                {
                    var data = JsonSerializer.Deserialize<RelayStatePayload>(payload, _jsonOptions);
                    if (data is not null)
                    {
                        await _repository.UpdateRelayStateAsync(deviceId, data.State);
                    }

                    break;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Payload MQTT JSON không hợp lệ ({Topic}): {Payload}", topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý MQTT message {Topic}", topic);
        }
    }
}
