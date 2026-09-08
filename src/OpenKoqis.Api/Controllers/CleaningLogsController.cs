using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.CleaningLogs.Commands;
using OpenKoqis.Application.Features.CleaningLogs.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningLogsController(ISender mediator) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync() =>
        (await mediator.Send(new GetAllCleaningLogsQuery())).Match(Ok, Problem);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id) =>
        (await mediator.Send(new GetCleaningLogByIdQuery(id))).Match(Ok, Problem);

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CleaningLog log) =>
        (await mediator.Send(new CreateCleaningLogCommand(log))).Match(
            created => CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created),
            Problem);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id) =>
        (await mediator.Send(new DeleteCleaningLogCommand(id))).Match(
            _ => NoContent(),
            Problem);

    public record LogCleaningRequest(string BinId, string UserId, int RemovedKg, string? Notes = null);

    [HttpPost("log")]
    public async Task<IActionResult> LogCleaningAsync([FromBody] LogCleaningRequest req) =>
        (await mediator.Send(new LogBinCleaningCommand(req.BinId, req.UserId, req.RemovedKg, req.Notes))).Match(
            created => CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created),
            Problem);
}
