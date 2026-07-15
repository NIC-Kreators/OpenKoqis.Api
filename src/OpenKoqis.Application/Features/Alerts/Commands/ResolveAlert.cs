using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Commands;

public record ResolveAlertCommand(string Id) : IRequest<ErrorOr<Success>>;

public class ResolveAlertCommandHandler(IMongoDatabase database, ILogger<ResolveAlertCommandHandler> logger) : IRequestHandler<ResolveAlertCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async Task<ErrorOr<Success>> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to resolve alert with ID: {Id}", request.Id);

        var alert = await _collection.Find(a => a.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
        if (alert is null)
        {
            logger.LogWarning("Resolution failed: Alert {Id} not found", request.Id);
            return AlertErrors.NotFound(request.Id);
        }

        if (alert.IsResolved)
        {
            logger.LogWarning("Resolution skipped: Alert {Id} is already resolved", request.Id);
            return AlertErrors.AlreadyResolved(request.Id);
        }

        var filter = Builders<Alert>.Filter.Eq(a => a.Id, request.Id);
        var update = Builders<Alert>.Update
            .Set(a => a.IsResolved, true)
            .Set(a => a.ResolvedAt, DateTime.UtcNow)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        logger.LogInformation("Alert {Id} status updated to Resolved", request.Id);

        return Result.Success;
    }
}
