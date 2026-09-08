using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Commands;

public record UpdateBinTelemetryCommand(string BinId, BinTelemetry Telemetry) : IRequest<ErrorOr<Success>>;

public class UpdateBinTelemetryCommandHandler(IMongoDatabase database, ILogger<UpdateBinTelemetryCommandHandler> logger) : IRequestHandler<UpdateBinTelemetryCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async ValueTask<ErrorOr<Success>> Handle(UpdateBinTelemetryCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating current telemetry for bin {BinId}", request.BinId);

        var telemetry = request.Telemetry;
        telemetry.LastUpdated ??= DateTime.UtcNow;

        var filter = Builders<Bin>.Filter.Eq(b => b.Id, request.BinId);
        var update = Builders<Bin>.Update
            .Set(b => b.Telemetry, telemetry)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (result.MatchedCount is 0)
        {
            logger.LogWarning("Telemetry update failed. Bin '{BinId}' not found", request.BinId);
            return BinErrors.NotFound(request.BinId);
        }

        logger.LogInformation("Current telemetry for bin {BinId} updated. Fill level: {FillLevel}%", request.BinId, telemetry.FillLevel);
        return Result.Success;
    }
}
