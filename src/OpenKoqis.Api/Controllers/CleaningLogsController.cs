using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.CleaningLogs.Commands;
using OpenKoqis.Application.Features.CleaningLogs.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningLogsController(ISender mediator, ILogger<CleaningLogsController> logger) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        logger.LogInformation("Fetching all cleaning logs at {Time}", DateTime.UtcNow);

        var result = await mediator.Send(new GetAllCleaningLogsQuery());

        return result.Match(Ok, HandleErrors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        logger.LogInformation("Requested cleaning log with ID: {LogId}", id);

        var result = await mediator.Send(new GetCleaningLogByIdQuery(id));

        return result.Match(Ok, HandleErrors);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CleaningLog log)
    {
        logger.LogInformation("Creating a new manual cleaning log entry");

        var result = await mediator.Send(new CreateCleaningLogCommand(log));

        return result.Match(
            created =>
            {
                logger.LogInformation("Successfully created log with ID: {LogId}", created.Id);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
            },
            HandleErrors
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogWarning("Request to DELETE cleaning log: {LogId}", id);

        var result = await mediator.Send(new DeleteCleaningLogCommand(id));

        return result.Match(_ => NoContent(), HandleErrors);
    }

    public class LogCleaningRequest
    {
        public string BinId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public int RemovedKg { get; set; }
        public string? Notes { get; set; }
    }

    [HttpPost("log")]
    public async Task<IActionResult> LogCleaningAsync([FromBody] LogCleaningRequest req)
    {
        logger.LogInformation("Domain Action: Logging cleaning process for Bin: {BinId} by User: {UserId}", req.BinId, req.UserId);

        var command = new LogBinCleaningCommand(req.BinId, req.UserId, req.RemovedKg, req.Notes);
        var result = await mediator.Send(command);

        return result.Match(
            created =>
            {
                logger.LogInformation("Domain Action Success: Bin {BinId} cleaned, removed {Weight}kg", req.BinId, req.RemovedKg);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
            },
            HandleErrors
        );
    }
}
