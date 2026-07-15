using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Queries;

public record GetCleaningLogByIdQuery(string Id) : IRequest<ErrorOr<CleaningLog>>;

public class GetCleaningLogByIdQueryHandler(IMongoDatabase database, ILogger<GetCleaningLogByIdQueryHandler> logger) : IRequestHandler<GetCleaningLogByIdQuery, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _collection = database.GetCollection<CleaningLog>("CleaningLogs");

    public async Task<ErrorOr<CleaningLog>> Handle(GetCleaningLogByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching for cleaning log with ID: {Id}", request.Id);

        var log = await _collection.Find(l => l.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            logger.LogWarning("Cleaning log with ID: {Id} was not found", request.Id);
            return CleaningLogErrors.NotFound(request.Id);
        }

        logger.LogInformation("Found cleaning log for Bin: {BinId}", log.BinId);
        return log;
    }
}
