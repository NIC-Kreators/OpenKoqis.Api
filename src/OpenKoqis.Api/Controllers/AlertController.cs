using Mediator;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Alerts.Commands;
using OpenKoqis.Application.Features.Alerts.Queries;

namespace OpenKoqis.Api.Controllers;

public class AlertsController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync() =>
        (await sender.Send(new GetAllAlertsQuery())).Match(Ok, Problem);

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAsync() =>
        (await sender.Send(new GetActiveAlertsQuery())).Match(Ok, Problem);

    [HttpGet("bin/{binId}")]
    public async Task<IActionResult> GetByBinAsync(string binId) =>
        (await sender.Send(new GetAlertsByBinIdQuery(binId))).Match(
            alerts => alerts.Count is 0
                ? NotFound(($"No alerts found for bin with ID {binId}"))
                : Ok(alerts),
            Problem);

    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> ResolveAsync(string id) =>
        (await sender.Send(new ResolveAlertCommand(id))).Match(
            _ => NoContent(),
            Problem);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id) =>
        (await sender.Send(new DeleteAlertCommand(id))).Match(
            _ => NoContent(),
            Problem);
}
