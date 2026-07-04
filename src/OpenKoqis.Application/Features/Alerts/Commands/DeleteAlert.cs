using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Commands;

public record DeleteAlertCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteAlertCommandHandler : IRequestHandler<DeleteAlertCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<Alert> _collection;
    private readonly ILogger<DeleteAlertCommandHandler> _logger;

    public DeleteAlertCommandHandler(IMongoDatabase database, ILogger<DeleteAlertCommandHandler> logger)
    {
        _collection = database.GetCollection<Alert>("Alerts");
        _logger = logger;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteAlertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Deleting alert with ID: {Id} from database", request.Id);

        var filter = Builders<Alert>.Filter.Eq(a => a.Id, request.Id);
        var result = await _collection.DeleteOneAsync(filter, cancellationToken);

        if (result.DeletedCount == 0)
        {
            _logger.LogWarning("Delete failed: Alert {Id} not found", request.Id);
            return AlertErrors.NotFound(request.Id);
        }

        _logger.LogInformation("Alert {Id} has been deleted", request.Id);
        return Result.Deleted;
    }
}
