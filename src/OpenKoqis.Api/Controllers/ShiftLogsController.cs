using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.ShiftLogs.Commands;
using OpenKoqis.Application.Features.ShiftLogs.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftLogsController(ISender mediator, ILogger<ShiftLogsController> logger) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        logger.LogInformation("Request received: Get all shift logs");

        var result = await mediator.Send(new GetAllShiftLogsQuery());

        return result.Match(
            shifts =>
            {
                logger.LogInformation("Successfully retrieved {Count} shifts", shifts.Count);
                return Ok(shifts);
            },
            HandleErrors
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        logger.LogInformation("Request received: Get shift log with ID: {Id}", id);

        var result = await mediator.Send(new GetShiftLogByIdQuery(id));

        return result.Match(
            shift =>
            {
                logger.LogInformation("Successfully retrieved shift log for User: {UserId}", shift.UserId);
                return Ok(shift);
            },
            HandleErrors
        );
    }

    public class StartShiftRequest
    {
        public string UserId { get; set; } = null!;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync([FromBody] StartShiftRequest req)
    {
        logger.LogInformation("Attempting to start a new shift for User: {UserId}", req.UserId);

        var result = await mediator.Send(new StartShiftCommand(req.UserId));

        return result.Match(
            created =>
            {
                logger.LogInformation("Shift started successfully. Assigned ID: {ShiftId}", created.Id);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
            },
            HandleErrors
        );
    }

    public class EndShiftRequest
    {
        public DateTime? EndedAt { get; set; }
        public IEnumerable<string>? CleanedBinIds { get; set; }
        public double DistanceKm { get; set; }
        public string? Route { get; set; }
    }

    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAsync(string id, [FromBody] EndShiftRequest req)
    {
        logger.LogInformation("Attempting to end shift ID: {Id}. Distance: {Distance}km", id, req.DistanceKm);

        var command = new EndShiftCommand(
            id,
            req.EndedAt ?? default,
            req.CleanedBinIds ?? Enumerable.Empty<string>(),
            req.DistanceKm,
            req.Route);

        var result = await mediator.Send(command);

        return result.Match(
            _ =>
            {
                logger.LogInformation("Shift ID: {Id} ended successfully", id);
                return NoContent();
            },
            HandleErrors
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogInformation("Request to delete shift log ID: {Id}", id);

        var result = await mediator.Send(new DeleteShiftLogCommand(id));

        return result.Match(
            _ =>
            {
                logger.LogInformation("Shift log ID: {Id} deleted successfully", id);
                return NoContent();
            },
            HandleErrors
        );
    }
}
