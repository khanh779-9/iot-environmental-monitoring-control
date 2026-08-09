using System.Text.Json;
using Esp32Monitor.Api.Data;
using Esp32Monitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT")
           ?? builder.Configuration["Server:Port"]
           ?? "3000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Giữ JSON tương thích frontend cũ: device_id, relay_state, created_at...
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddSingleton<MySqlConnectionFactory>();
builder.Services.AddSingleton<MonitoringRepository>();

// MqttService vừa chạy nền để subscribe telemetry, vừa được inject để publish relay command.
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IMqttCommandPublisher>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var configuredOrigin = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN")
                               ?? builder.Configuration["Cors:FrontendOrigin"]
                               ?? "http://localhost:5173";

        if (configuredOrigin.Trim() == "*")
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        var origins = configuredOrigin
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Lỗi server không xác định" });
    });
});

app.UseCors("Frontend");
app.MapControllers();

app.Run();
