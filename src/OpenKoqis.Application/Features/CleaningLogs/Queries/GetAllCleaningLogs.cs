using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Queries;

public record GetAllCleaningLogsQuery : IRequest<ErrorOr<List<CleaningLog>>>;

public class GetAllCleaningLogsQueryHandler(IMongoDatabase database, ILogger<GetAllCleaningLogsQueryHandler> logger) : IRequestHandler<GetAllCleaningLogsQuery, ErrorOr<List<CleaningLog>>>
{
    private readonly IMongoCollection<CleaningLog> _collection = database.GetCollection<CleaningLog>("CleaningLogs");

    public async Task<ErrorOr<List<CleaningLog>>> Handle(GetAllCleaningLogsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all cleaning logs from database");

        var logs = await _collection.Find(FilterDefinition<CleaningLog>.Empty).ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} logs", logs.Count);
        return logs;
    }
}
