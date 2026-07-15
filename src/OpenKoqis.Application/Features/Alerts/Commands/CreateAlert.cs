using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Alerts.Commands;

public record CreateAlertCommand(string BinId, AlertType Type, AlertSeverity Severity, string Message, string? ValueAtTime) : IRequest<ErrorOr<Alert>>;

public class CreateAlertCommandHandler(IMongoDatabase database, ILogger<CreateAlertCommandHandler> logger) : IRequestHandler<CreateAlertCommand, ErrorOr<Alert>>
{
    private readonly IMongoCollection<Alert> _collection = database.GetCollection<Alert>("Alerts");

    public async Task<ErrorOr<Alert>> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating a new alert for BinId: {BinId}, Type: {Type}", request.BinId, request.Type);

        var alert = new Alert
        {
            BinId = request.BinId,
            Type = request.Type,
            Severity = request.Severity,
            Message = request.Message,
            ValueAtTime = request.ValueAtTime,
            CreatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(alert, cancellationToken: cancellationToken);
        logger.LogInformation("Alert successfully persisted to database with ID: {Id}", alert.Id);

        return alert;
    }
}
