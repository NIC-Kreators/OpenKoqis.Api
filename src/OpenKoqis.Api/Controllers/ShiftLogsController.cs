using Mediator;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.ShiftLogs.Commands;
using OpenKoqis.Application.Features.ShiftLogs.Queries;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftLogsController(ISender mediator) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        (await mediator.Send(new GetAllShiftLogsQuery(), cancellationToken)).Match(Ok, Problem);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetShiftLogByIdQuery(id), cancellationToken)).Match(Ok, Problem);

    public record StartShiftRequest(string UserId);

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync([FromBody] StartShiftRequest req, CancellationToken cancellationToken) =>
        (await mediator.Send(new StartShiftCommand(req.UserId), cancellationToken)).Match(
            created => CreatedAtAction("GetById", new { id = created.Id }, created),
            Problem);

    public record EndShiftRequest(DateTime? EndedAt, IEnumerable<string>? CleanedBinIds, double DistanceKm, string? Route = null);

    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAsync(string id, [FromBody] EndShiftRequest req, CancellationToken cancellationToken) =>
        (await mediator.Send(new EndShiftCommand(
            id,
            req.EndedAt ?? default,
            req.CleanedBinIds ?? [],
            req.DistanceKm,
            req.Route), cancellationToken)).Match(
            _ => NoContent(),
            Problem);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new DeleteShiftLogCommand(id), cancellationToken)).Match(
            _ => NoContent(),
            Problem);
}
