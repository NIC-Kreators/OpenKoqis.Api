using Bogus;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Alerts.Commands;
using OpenKoqis.Application.Features.Bins.Commands;
using OpenKoqis.Application.Features.Bins.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BinsController(ISender mediator, ILogger<BinsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] BinStatus? status = null, [FromQuery] int? minFillLevel = null)
    {
        logger.LogInformation("Fetching bins with filters: Status={Status}, MinFill={MinFill}", status, minFillLevel);

        var result = await mediator.Send(new GetAllBinsQuery());

        return result.Match(
            bins =>
            {
                if (status.HasValue)
                    bins = bins.Where(b => b.Status == status.Value).ToList();

                if (minFillLevel.HasValue)
                    bins = bins.Where(b => b.Telemetry.FillLevel >= minFillLevel.Value).ToList();

                logger.LogDebug("Successfully retrieved {Count} bins after filtering", bins.Count);
                return Ok(bins);
            },
            errors => Problem(errors)
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        logger.LogInformation("Getting bin details for ID: {BinId}", id);

        var result = await mediator.Send(new GetBinByIdQuery(id));

        return result.Match(
            bin => Ok(bin),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] Bin bin)
    {
        logger.LogInformation("Creating a new bin of type {Type}", bin.Type);

        var command = new CreateBinCommand(bin.Type, bin.Location, bin.Telemetry, bin.Status);
        var result = await mediator.Send(command);

        return result.Match(
            created =>
            {
                logger.LogInformation("Successfully created bin with ID: {BinId}", created.Id);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created);
            },
            errors => Problem(errors)
        );
    }

    [HttpPost("{id}/telemetry")]
    public async Task<IActionResult> PostTelemetryAsync(string id, [FromBody] BinTelemetry telemetry)
    {
        logger.LogInformation("Received telemetry update for Bin: {BinId}. FillLevel: {Fill}%", id, telemetry.FillLevel);

        var updateResult = await mediator.Send(new UpdateBinTelemetryCommand(id, telemetry));
        if (updateResult.IsError)
        {
            logger.LogWarning("Failed to update telemetry for Bin: {BinId}", id);
            return Problem(updateResult.Errors);
        }

        var historyResult = await mediator.Send(new UpdateBinTelemetryHistoryCommand(id, telemetry));
        if (historyResult.IsError)
        {
            logger.LogWarning("Failed to update telemetry history for Bin: {BinId}", id);
            return Problem(historyResult.Errors);
        }

        if (telemetry.IsSmokeDetected)
        {
            logger.LogCritical("SMOKE DETECTED in Bin: {BinId}!", id);
            await mediator.Send(new CreateAlertCommand(
                BinId: id,
                Type: AlertType.Smoke,
                Severity: AlertSeverity.Critical,
                Message: "Danger! Smoke detected in the bin.",
                ValueAtTime: null));
        }

        if (telemetry.FillLevel >= 90)
        {
            logger.LogWarning("Bin {BinId} is almost full: {Level}%", id, telemetry.FillLevel);
            var severity = telemetry.FillLevel >= 100 ? AlertSeverity.Critical : AlertSeverity.Warning;

            await mediator.Send(new CreateAlertCommand(
                BinId: id,
                Type: AlertType.Fullness,
                Severity: severity,
                Message: $"Container fill level at {telemetry.FillLevel}%",
                ValueAtTime: telemetry.FillLevel.ToString()));
        }

        return NoContent();
    }

    [HttpPost("seed/{count}")]
    public async Task<IActionResult> SeedBinsAsync(int count = 10)
    {
        logger.LogInformation("Starting seed operation for {Count} bins in Almaty", count);

        var telemetryFaker = new Faker<BinTelemetry>()
            .RuleFor(t => t.FillLevel, f => f.Random.Int(0, 100))
            .RuleFor(t => t.IsSmokeDetected, f => f.Random.Bool(0.05f))
            .RuleFor(t => t.IsOverloaded, f => f.Random.Bool(0.1f))
            .RuleFor(t => t.LastUpdated, f => f.Date.Recent(1));

        var binFaker = new Faker<Bin>()
            .RuleFor(b => b.Type, f => f.PickRandom<BinType>())
            .RuleFor(b => b.Status, f => f.PickRandom<BinStatus>())
            .RuleFor(b => b.Location, f => new GeoPoint(new double[]
            {
                f.Address.Longitude(76.80, 77.00), f.Address.Latitude(43.20, 43.30)
            }))
            .RuleFor(b => b.Telemetry, f => telemetryFaker.Generate())
            .RuleFor(b => b.TelemetryHistory, f => telemetryFaker.Generate(f.Random.Int(1, 5)).ToArray())
            .RuleFor(b => b.CreatedAt, f => f.Date.Past(1))
            .RuleFor(b => b.UpdatedAt, f => DateTime.UtcNow);

        var fakeBins = binFaker.Generate(count);
        int successCount = 0;

        foreach (var bin in fakeBins)
        {
            var command = new CreateBinCommand(bin.Type, bin.Location, bin.Telemetry, bin.Status);
            var result = await mediator.Send(command);

            if (!result.IsError)
            {
                successCount++;
            }
        }

        logger.LogInformation("Seed completed. Created {Success} out of {Total} bins", successCount, count);
        return Ok(new
        {
            message = $"Successfully seeded {successCount} bins in Almaty region"
        });
    }


    private IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Problem();
        }

        var firstError = errors.First();

        var statusCode = firstError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: firstError.Description);
    }
}
