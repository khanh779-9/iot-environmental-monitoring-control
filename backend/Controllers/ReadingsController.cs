using Esp32Monitor.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Esp32Monitor.Api.Controllers;

[ApiController]
[Route("api/readings")]
public sealed class ReadingsController(MonitoringRepository repository) : ControllerBase
{
    // GET /api/readings?device_id=&limit=&from=&to=
    [HttpGet]
    public async Task<IActionResult> GetReadings(
        [FromQuery(Name = "device_id")] string? deviceId,
        [FromQuery] int limit = 200,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetReadingsAsync(
            deviceId,
            Math.Clamp(limit, 1, 2000),
            from,
            to,
            cancellationToken);

        return Ok(rows);
    }

    // GET /api/readings/latest?device_id=
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest(
        [FromQuery(Name = "device_id")] string? deviceId,
        CancellationToken cancellationToken = default)
    {
        var row = await repository.GetLatestReadingAsync(deviceId, cancellationToken);
        return new JsonResult(row);
    }
}
