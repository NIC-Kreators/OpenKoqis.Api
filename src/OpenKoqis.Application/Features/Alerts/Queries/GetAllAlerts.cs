using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Queries;

public record GetAllAlertsQuery : IRequest<ErrorOr<List<Alert>>>;

public class GetAllAlertsQueryHandler : IRequestHandler<GetAllAlertsQuery, ErrorOr<List<Alert>>>
{
    private readonly IMongoCollection<Alert> _collection;
    private readonly ILogger<GetAllAlertsQueryHandler> _logger;

    public GetAllAlertsQueryHandler(IMongoDatabase database, ILogger<GetAllAlertsQueryHandler> logger)
    {
        _collection = database.GetCollection<Alert>("Alerts");
        _logger = logger;
    }

    public async Task<ErrorOr<List<Alert>>> Handle(GetAllAlertsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all alerts from the database");

        var alerts = await _collection.Find(FilterDefinition<Alert>.Empty)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} alerts", alerts.Count);
        return alerts;
    }
}
