using Esp32Monitor.Api.Models;
using MySqlConnector;

namespace Esp32Monitor.Api.Data;

public sealed class MonitoringRepository(MySqlConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<ReadingDto>> GetReadingsAsync(
        string? deviceId,
        int limit,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            conditions.Add("device_id = @deviceId");
            command.Parameters.AddWithValue("@deviceId", deviceId);
        }

        if (from.HasValue)
        {
            conditions.Add("created_at >= @from");
            command.Parameters.AddWithValue("@from", from.Value);
        }

        if (to.HasValue)
        {
            conditions.Add("created_at <= @to");
            command.Parameters.AddWithValue("@to", to.Value);
        }

        var whereClause = conditions.Count > 0
            ? $"WHERE {string.Join(" AND ", conditions)}"
            : string.Empty;

        command.CommandText = $"""
            SELECT id, device_id, temperature, humidity, relay_state, created_at
            FROM readings
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @limit;
            """;
        command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 2000));

        var rows = new List<ReadingDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapReading(reader));
        }

        return rows;
    }

    public async Task<ReadingDto?> GetLatestReadingAsync(
        string? deviceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = string.IsNullOrWhiteSpace(deviceId)
            ? """
              SELECT id, device_id, temperature, humidity, relay_state, created_at
              FROM readings
              ORDER BY created_at DESC
              LIMIT 1;
              """
            : """
              SELECT id, device_id, temperature, humidity, relay_state, created_at
              FROM readings
              WHERE device_id = @deviceId
              ORDER BY created_at DESC
              LIMIT 1;
              """;

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            command.Parameters.AddWithValue("@deviceId", deviceId);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapReading(reader) : null;
    }

    public async Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT device_id, relay_state, online, last_seen
            FROM devices
            ORDER BY device_id;
            """;

        var rows = new List<DeviceDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapDevice(reader));
        }

        return rows;
    }

    public async Task<DeviceDto?> GetDeviceAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT device_id, relay_state, online, last_seen
            FROM devices
            WHERE device_id = @deviceId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@deviceId", deviceId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapDevice(reader) : null;
    }

    public async Task InsertTelemetryAsync(
        string deviceId,
        double temperature,
        double humidity,
        bool relayState,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO readings (device_id, temperature, humidity, relay_state)
                VALUES (@deviceId, @temperature, @humidity, @relayState);
                """;
            insert.Parameters.AddWithValue("@deviceId", deviceId);
            insert.Parameters.AddWithValue("@temperature", temperature);
            insert.Parameters.AddWithValue("@humidity", humidity);
            insert.Parameters.AddWithValue("@relayState", relayState ? 1 : 0);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await UpsertDeviceAsync(
            connection,
            transaction,
            deviceId,
            relayState: relayState,
            online: true,
            cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateRelayStateAsync(
        string deviceId,
        bool relayState,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await UpsertDeviceAsync(
            connection,
            transaction,
            deviceId,
            relayState: relayState,
            online: true,
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateOnlineStatusAsync(
        string deviceId,
        bool online,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO devices (device_id, online, last_seen)
            VALUES (@deviceId, @online, NOW())
            ON DUPLICATE KEY UPDATE
                online = VALUES(online),
                last_seen = VALUES(last_seen);
            """;
        command.Parameters.AddWithValue("@deviceId", deviceId);
        command.Parameters.AddWithValue("@online", online ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertDeviceAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string deviceId,
        bool relayState,
        bool online,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO devices (device_id, relay_state, online, last_seen)
            VALUES (@deviceId, @relayState, @online, NOW())
            ON DUPLICATE KEY UPDATE
                relay_state = VALUES(relay_state),
                online = VALUES(online),
                last_seen = VALUES(last_seen);
            """;
        command.Parameters.AddWithValue("@deviceId", deviceId);
        command.Parameters.AddWithValue("@relayState", relayState ? 1 : 0);
        command.Parameters.AddWithValue("@online", online ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ReadingDto MapReading(MySqlDataReader reader) => new(
        reader.GetInt32("id"),
        reader.GetString("device_id"),
        reader.GetDouble("temperature"),
        reader.GetDouble("humidity"),
        reader.GetInt32("relay_state"),
        reader.GetDateTime("created_at"));

    private static DeviceDto MapDevice(MySqlDataReader reader) => new(
        reader.GetString("device_id"),
        reader.GetInt32("relay_state"),
        reader.GetInt32("online"),
        reader.IsDBNull(reader.GetOrdinal("last_seen"))
            ? null
            : reader.GetDateTime("last_seen"));
}
