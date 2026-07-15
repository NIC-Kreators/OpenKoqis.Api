using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record DeleteCleaningLogCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteCleaningLogCommandHandler(IMongoDatabase database, ILogger<DeleteCleaningLogCommandHandler> logger) : IRequestHandler<DeleteCleaningLogCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<CleaningLog> _collection = database.GetCollection<CleaningLog>("CleaningLogs");

    public async Task<ErrorOr<Deleted>> Handle(DeleteCleaningLogCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete cleaning log: {Id}", request.Id);

        var result = await _collection.DeleteOneAsync(l => l.Id == request.Id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            logger.LogWarning("Delete failed: CleaningLog '{Id}' not found", request.Id);
            return CleaningLogErrors.NotFound(request.Id);
        }

        logger.LogInformation("Cleaning log {Id} successfully deleted", request.Id);
        return Result.Deleted;
    }
}
