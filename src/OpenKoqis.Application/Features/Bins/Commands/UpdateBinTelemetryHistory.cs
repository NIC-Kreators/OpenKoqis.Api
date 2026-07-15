using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;
    
namespace OpenKoqis.Application.Features.Bins.Commands;

public record UpdateBinTelemetryHistoryCommand(string BinId, BinTelemetry Telemetry) : IRequest<ErrorOr<Success>>;

public class UpdateBinTelemetryHistoryCommandHandler(IMongoDatabase database, ILogger<UpdateBinTelemetryHistoryCommandHandler> logger) : IRequestHandler<UpdateBinTelemetryHistoryCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<Success>> Handle(UpdateBinTelemetryHistoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding new entry to telemetry history for bin {BinId}", request.BinId);

        var telemetry = request.Telemetry;
        telemetry.LastUpdated ??= DateTime.UtcNow;

        var filter = Builders<Bin>.Filter.Eq(b => b.Id, request.BinId);
        var update = Builders<Bin>.Update
            .Push(b => b.TelemetryHistory, telemetry)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            logger.LogWarning("History update failed. Bin '{BinId}' not found", request.BinId);
            return BinErrors.NotFound(request.BinId);
        }

        logger.LogInformation("History for bin {BinId} updated successfully", request.BinId);
        return Result.Success;
    }
}
