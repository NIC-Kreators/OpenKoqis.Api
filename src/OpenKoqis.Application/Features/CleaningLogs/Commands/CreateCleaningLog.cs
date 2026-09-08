using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record CreateCleaningLogCommand(CleaningLog Log) : IRequest<ErrorOr<CleaningLog>>;

public class CreateCleaningLogCommandHandler(IMongoDatabase database, ILogger<CreateCleaningLogCommandHandler> logger) : IRequestHandler<CreateCleaningLogCommand, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _collection = database.GetCollection<CleaningLog>("CleaningLogs");

    public async ValueTask<ErrorOr<CleaningLog>> Handle(CreateCleaningLogCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new manual cleaning log entry");

        request.Log.CreatedAt = DateTime.UtcNow;
        request.Log.UpdatedAt = request.Log.CreatedAt;

        await _collection.InsertOneAsync(request.Log, cancellationToken: cancellationToken);
        logger.LogInformation("Cleaning log inserted with generated ID: {Id}", request.Log.Id);

        return request.Log;
    }
}
