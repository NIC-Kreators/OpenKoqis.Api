using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Alerts.Commands;
using OpenKoqis.Application.Features.Alerts.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController(ISender sender, ILogger<AlertsController> logger) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        logger.LogInformation("Request received: GetAll alerts");

        var result = await sender.Send(new GetAllAlertsQuery());

        return result.Match(Ok, HandleErrors);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAsync()
    {
        logger.LogInformation("Request received: GetActive alerts");

        var result = await sender.Send(new GetActiveAlertsQuery());

        return result.Match(Ok, HandleErrors);
    }

    [HttpGet("bin/{binId}")]
    public async Task<IActionResult> GetByBinAsync(string binId)
    {
        logger.LogInformation("Request received: GetByBin for BinId: {BinId}", binId);

        var result = await sender.Send(new GetAlertsByBinIdQuery(binId));

        return result.Match(
            alerts => alerts.Count == 0
                ? NotFound($"No alerts found for bin with ID {binId}")
                : Ok(alerts),
            HandleErrors
        );
    }

    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> ResolveAsync(string id)
    {
        logger.LogInformation("Attempting to resolve alert with ID: {AlertId}", id);

        var result = await sender.Send(new ResolveAlertCommand(id));

        return result.Match(_ => NoContent(), HandleErrors);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogInformation("Request received: Delete alert with ID: {AlertId}", id);

        var result = await sender.Send(new DeleteAlertCommand(id));

        return result.Match(_ => NoContent(), HandleErrors);
    }
}
