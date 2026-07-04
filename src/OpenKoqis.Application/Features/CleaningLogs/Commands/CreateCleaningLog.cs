using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record CreateCleaningLogCommand(CleaningLog Log) : IRequest<ErrorOr<CleaningLog>>;

public class CreateCleaningLogCommandHandler : IRequestHandler<CreateCleaningLogCommand, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _collection;
    private readonly ILogger<CreateCleaningLogCommandHandler> _logger;

    public CreateCleaningLogCommandHandler(IMongoDatabase database, ILogger<CreateCleaningLogCommandHandler> logger)
    {
        _collection = database.GetCollection<CleaningLog>("CleaningLogs");
        _logger = logger;
    }

    public async Task<ErrorOr<CleaningLog>> Handle(CreateCleaningLogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new manual cleaning log entry");

        var log = request.Log;
        log.CreatedAt = DateTime.UtcNow;
        log.UpdatedAt = log.CreatedAt;

        await _collection.InsertOneAsync(log, cancellationToken: cancellationToken);
        _logger.LogInformation("Cleaning log inserted with generated ID: {Id}", log.Id);

        return log;
    }
}
