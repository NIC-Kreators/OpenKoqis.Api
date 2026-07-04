using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetActiveAlertsQuery : IRequest<ErrorOr<List<Alert>>>;

public class GetActiveAlertsQueryHandler : IRequestHandler<GetActiveAlertsQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection;
    private readonly ILogger<GetActiveAlertsQueryHandler> _logger;

    public GetActiveAlertsQueryHandler(IMongoDatabase database, ILogger<GetActiveAlertsQueryHandler> logger)
    {
        _collection = database.GetCollection<Alert>("Alerts");
        _logger = logger;
    }

    public async Task<ErrorOr<List<Alert>>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Filtering active (unresolved) alerts");

        var filter = Builders<Alert>.Filter.Eq(a => a.IsResolved, false);
        var activeAlerts = await _collection.Find(filter).ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} active alerts", activeAlerts.Count);
        return activeAlerts;
    }
}
