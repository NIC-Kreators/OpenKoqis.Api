using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetAlertsByBinIdQuery(string BinId) : IRequest<ErrorOr<List<Alert>>>;

public class GetAlertsByBinIdQueryHandler : IRequestHandler<GetAlertsByBinIdQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection;
    private readonly ILogger<GetAlertsByBinIdQueryHandler> _logger;

    public GetAlertsByBinIdQueryHandler(IMongoDatabase database, ILogger<GetAlertsByBinIdQueryHandler> logger)
    {
        _collection = database.GetCollection<Alert>("Alerts");
        _logger = logger;
    }

    public async Task<ErrorOr<List<Alert>>> Handle(GetAlertsByBinIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching alerts for BinId: {BinId}", request.BinId);

        var filter = Builders<Alert>.Filter.Eq(a => a.BinId, request.BinId);
        var alerts = await _collection.Find(filter).ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} alerts for BinId: {BinId}", alerts.Count, request.BinId);
        return alerts;
    }
}
