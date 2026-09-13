using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Commands;

public record CreateAlertCommand(string BinId, AlertType Type, AlertSeverity Severity, string Message, string? ValueAtTime) : IRequest<ErrorOr<Alert>>;

public class CreateAlertCommandHandler(IMongoDatabase database, ILogger<CreateAlertCommandHandler> logger) : IRequestHandler<CreateAlertCommand, ErrorOr<Alert>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async ValueTask<ErrorOr<Alert>> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = new Alert(
            request.BinId,
            new AlertDetails(request.Type, request.Severity, request.Message, request.ValueAtTime));

        await _collection.InsertOneAsync(alert, null, cancellationToken);

        logger.LogInformation("Alert created with ID: {Id}", alert.Id);

        return alert;
    }
}
