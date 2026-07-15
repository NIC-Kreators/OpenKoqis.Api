using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetAllAlertsQuery : IRequest<ErrorOr<List<Alert>>>;

public class GetAllAlertsQueryHandler(IMongoDatabase database, ILogger<GetAllAlertsQueryHandler> logger) : IRequestHandler<GetAllAlertsQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async Task<ErrorOr<List<Alert>>> Handle(GetAllAlertsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all alerts from the database");

        var alerts = await _collection.Find(FilterDefinition<Alert>.Empty)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} alerts", alerts.Count);
        return alerts;
    }
}
