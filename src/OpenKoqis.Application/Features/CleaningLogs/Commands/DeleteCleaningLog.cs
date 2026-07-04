using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record DeleteCleaningLogCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteCleaningLogCommandHandler : IRequestHandler<DeleteCleaningLogCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<CleaningLog> _collection;
    private readonly ILogger<DeleteCleaningLogCommandHandler> _logger;

    public DeleteCleaningLogCommandHandler(IMongoDatabase database, ILogger<DeleteCleaningLogCommandHandler> logger)
    {
        _collection = database.GetCollection<CleaningLog>("CleaningLogs");
        _logger = logger;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteCleaningLogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete cleaning log: {Id}", request.Id);

        var result = await _collection.DeleteOneAsync(l => l.Id == request.Id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            _logger.LogWarning("Delete failed: CleaningLog '{Id}' not found", request.Id);
            return CleaningLogErrors.NotFound(request.Id);
        }

        _logger.LogInformation("Cleaning log {Id} successfully deleted", request.Id);
        return Result.Deleted;
    }
}
