using Bogus;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Alerts.Commands;
using OpenKoqis.Application.Features.Bins.Commands;
using OpenKoqis.Application.Features.Bins.Queries;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Controllers;

public class BinsController(ISender mediator) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] BinStatus? status = null,
        [FromQuery] int? minFillLevel = null,
        CancellationToken cancellationToken = default) =>
        (await mediator.Send(new GetAllBinsQuery(), cancellationToken)).Match(
            bins =>
            {
                if (status is { } s)
                    bins = bins.Where(b => b.Status == s).ToList();
                if (minFillLevel is { } m)
                    bins = bins.Where(b => b.Telemetry?.FillLevel is { } fl && (int)fl >= m).ToList();
                return Ok(bins);
            },
            Problem
        );


    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetBinByIdQuery(id), cancellationToken)).Match(Ok, Problem);

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] Bin bin, CancellationToken cancellationToken) =>
        bin.Telemetry is null
            ? BadRequest("Telemetry is required.")
            : (await mediator.Send(new CreateBinCommand(bin.Type, bin.Location, bin.Telemetry, bin.Status), cancellationToken))
                .Match(created => CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created), Problem);

    [HttpPost("{id}/telemetry")]
    public async Task<IActionResult> PostTelemetryAsync(string id, [FromBody] BinTelemetry telemetry, CancellationToken cancellationToken)
    {
        var updateResult = await mediator.Send(new UpdateBinTelemetryCommand(id, telemetry), cancellationToken);
        if (updateResult.IsError)
            return Problem(updateResult.Errors);

        var historyResult = await mediator.Send(new UpdateBinTelemetryHistoryCommand(id, telemetry), cancellationToken);
        if (historyResult.IsError)
            return Problem(historyResult.Errors);

        if (telemetry.IsSmokeDetected)
            await mediator.Send(new CreateAlertCommand(id, AlertType.Smoke, AlertSeverity.Critical, "Danger! Smoke detected in the bin.", null), cancellationToken);

        if (telemetry.FillLevel >= 90)
        {
            var severity = telemetry.FillLevel >= 100 ? AlertSeverity.Critical : AlertSeverity.Warning;
            await mediator.Send(new CreateAlertCommand(id, AlertType.Fullness, severity, $"Container fill level at {telemetry.FillLevel}%", telemetry.FillLevel.ToString()), cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("seed/{count}")]
    public async Task<IActionResult> SeedBinsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var telemetryFaker = new Faker<BinTelemetry>()
            .RuleFor(t => t.FillLevel, f => FillLevel.Parse(f.Random.Int(0, 100)))
            .RuleFor(t => t.IsSmokeDetected, f => f.Random.Bool(0.05f))
            .RuleFor(t => t.IsOverloaded, f => f.Random.Bool(0.1f))
            .RuleFor(t => t.LastUpdated, f => f.Date.Recent(1));

        var binFaker = new Faker<Bin>()
            .RuleFor(b => b.Type, f => f.PickRandom<BinType>())
            .RuleFor(b => b.Status, f => f.PickRandom<BinStatus>())
            .RuleFor(b => b.Location, f => new GeoPoint(f.Address.Longitude(76.80, 77.00), f.Address.Latitude(43.20, 43.30)))
            .RuleFor(b => b.Telemetry, f => telemetryFaker.Generate())
            .RuleFor(b => b.TelemetryHistory, f => telemetryFaker.Generate(f.Random.Int(1, 5)))
            .RuleFor(b => b.CreatedAt, f => f.Date.Past(1));

        int successCount = 0;
        foreach (var bin in binFaker.Generate(count))
        {
            if (bin.Telemetry is not null)
            {
                var result = await mediator.Send(new CreateBinCommand(bin.Type, bin.Location, bin.Telemetry, bin.Status), cancellationToken);
                if (!result.IsError)
                    successCount++;
            }
        }

        return Ok(new { message = $"Successfully seeded {successCount} out of {count} bins in Almaty region" });
    }
}
