using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Features.Alerts.Errors;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Commands;

public record DeleteAlertCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteAlertCommandHandler(IMongoDatabase database, ILogger<DeleteAlertCommandHandler> logger) : IRequestHandler<DeleteAlertCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async ValueTask<ErrorOr<Deleted>> Handle(DeleteAlertCommand request, CancellationToken cancellationToken)
    {
        logger.LogWarning("Deleting alert with ID: {Id} from database", request.Id);

        var result = await _collection.DeleteOneAsync(a => a.Id == request.Id, cancellationToken);

        if (result.DeletedCount is 0)
        {
            logger.LogWarning("Delete failed: Alert {Id} not found", request.Id);
            return AlertErrors.NotFound(request.Id);
        }

        logger.LogInformation("Alert {Id} has been deleted", request.Id);
        return Result.Deleted;
    }
}
