using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Commands;

public record UpdateBinTelemetryHistoryCommand(string BinId, BinTelemetry Telemetry) : IRequest<ErrorOr<Success>>;

public class UpdateBinTelemetryHistoryCommandHandler : IRequestHandler<UpdateBinTelemetryHistoryCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<Bin> _collection;
    private readonly ILogger<UpdateBinTelemetryHistoryCommandHandler> _logger;

    public UpdateBinTelemetryHistoryCommandHandler(IMongoDatabase database, ILogger<UpdateBinTelemetryHistoryCommandHandler> logger)
    {
        _collection = database.GetCollection<Bin>("Bins");
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateBinTelemetryHistoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding new entry to telemetry history for bin {BinId}", request.BinId);

        var telemetry = request.Telemetry;
        telemetry.LastUpdated = telemetry.LastUpdated == default ? DateTime.UtcNow : telemetry.LastUpdated;

        var filter = Builders<Bin>.Filter.Eq(b => b.Id, request.BinId);
        var update = Builders<Bin>.Update
            .Push(b => b.TelemetryHistory, telemetry)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            _logger.LogWarning("History update failed. Bin '{BinId}' not found", request.BinId);
            return BinErrors.NotFound(request.BinId);
        }

        _logger.LogInformation("History for bin {BinId} updated successfully", request.BinId);
        return Result.Success;
    }
}
