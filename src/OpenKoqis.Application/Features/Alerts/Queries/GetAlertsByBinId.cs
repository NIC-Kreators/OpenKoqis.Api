using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetAlertsByBinIdQuery(string BinId) : IRequest<ErrorOr<List<Alert>>>;

public class GetAlertsByBinIdQueryHandler(IMongoDatabase database, ILogger<GetAlertsByBinIdQueryHandler> logger) : IRequestHandler<GetAlertsByBinIdQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async Task<ErrorOr<List<Alert>>> Handle(GetAlertsByBinIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching alerts for BinId: {BinId}", request.BinId);

        var filter = Builders<Alert>.Filter.Eq(a => a.BinId, request.BinId);
        var alerts = await _collection.Find(filter).ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} alerts for BinId: {BinId}", alerts.Count, request.BinId);
        return alerts;
    }
}
