using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Queries;

public record GetBinTelemetryHistoryQuery(string BinId) : IRequest<ErrorOr<List<BinTelemetry>>>;

public class GetBinTelemetryHistoryQueryHandler(IMongoDatabase database, ILogger<GetBinTelemetryHistoryQueryHandler> logger) : IRequestHandler<GetBinTelemetryHistoryQuery, ErrorOr<List<BinTelemetry>>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<List<BinTelemetry>>> Handle(GetBinTelemetryHistoryQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching telemetry history for bin {BinId}", request.BinId);

        var projection = Builders<Bin>.Projection.Include(b => b.TelemetryHistory);
        var bin = await _collection.Find(b => b.Id == request.BinId)
            .Project<Bin>(projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (bin is null)
        {
            logger.LogWarning("Bin {BinId} not found for telemetry history", request.BinId);
            return BinErrors.NotFound(request.BinId);
        }

        return bin.TelemetryHistory.ToList();
    }
}
