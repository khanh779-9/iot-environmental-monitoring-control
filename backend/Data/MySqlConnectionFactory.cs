using MySqlConnector;

namespace Esp32Monitor.Api.Data;

public sealed class MySqlConnectionFactory(IConfiguration configuration)
{
    public MySqlConnection CreateConnection()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")
                   ?? configuration["Database:Host"]
                   ?? "localhost";
        var portText = Environment.GetEnvironmentVariable("DB_PORT")
                       ?? configuration["Database:Port"]
                       ?? "3306";
        var user = Environment.GetEnvironmentVariable("DB_USER")
                   ?? configuration["Database:User"]
                   ?? "root";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
                       ?? configuration["Database:Password"]
                       ?? string.Empty;
        var database = Environment.GetEnvironmentVariable("DB_NAME")
                       ?? configuration["Database:Name"]
                       ?? "esp32_monitor";

        _ = uint.TryParse(portText, out var port);
        if (port == 0) port = 3306;

        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = port,
            UserID = user,
            Password = password,
            Database = database,
            Pooling = true,
            MaximumPoolSize = 10
        }.ConnectionString;

        return new MySqlConnection(connectionString);
    }
}
