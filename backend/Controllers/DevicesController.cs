using Esp32Monitor.Api.Data;
using Esp32Monitor.Api.Models;
using Esp32Monitor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Esp32Monitor.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController(
    MonitoringRepository repository,
    IMqttCommandPublisher mqttPublisher) : ControllerBase
{
    // GET /api/devices
    [HttpGet]
    public async Task<IActionResult> GetDevices(CancellationToken cancellationToken)
    {
        var rows = await repository.GetDevicesAsync(cancellationToken);
        return Ok(rows);
    }

    // GET /api/devices/:deviceId
    [HttpGet("{deviceId}")]
    public async Task<IActionResult> GetDevice(
        string deviceId,
        CancellationToken cancellationToken)
    {
        var row = await repository.GetDeviceAsync(deviceId, cancellationToken);
        return new JsonResult(row);
    }

    // POST /api/devices/:deviceId/relay  body: { "state": true|false }
    [HttpPost("{deviceId}/relay")]
    public async Task<IActionResult> SetRelay(
        string deviceId,
        [FromBody] RelayRequest request,
        CancellationToken cancellationToken)
    {
        if (request.State is null)
        {
            return BadRequest(new { error = "\"state\" phải là true hoặc false" });
        }

        try
        {
            await mqttPublisher.PublishRelayCommandAsync(
                deviceId,
                request.State.Value,
                cancellationToken);

            return Ok(new { ok = true, sent = request.State.Value });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = ex.Message
            });
        }
    }
}
