using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetActiveAlertsQuery : IRequest<ErrorOr<List<Alert>>>;

public class GetActiveAlertsQueryHandler(IMongoDatabase database, ILogger<GetActiveAlertsQueryHandler> logger) : IRequestHandler<GetActiveAlertsQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async Task<ErrorOr<List<Alert>>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Filtering active (unresolved) alerts");

        var filter = Builders<Alert>.Filter.Eq(a => a.IsResolved, false);
        var activeAlerts = await _collection.Find(filter).ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} active alerts", activeAlerts.Count);
        return activeAlerts;
    }
}
