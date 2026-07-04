using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Queries;

public record GetCleaningLogByIdQuery(string Id) : IRequest<ErrorOr<CleaningLog>>;

public class GetCleaningLogByIdQueryHandler : IRequestHandler<GetCleaningLogByIdQuery, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _collection;
    private readonly ILogger<GetCleaningLogByIdQueryHandler> _logger;

    public GetCleaningLogByIdQueryHandler(IMongoDatabase database, ILogger<GetCleaningLogByIdQueryHandler> logger)
    {
        _collection = database.GetCollection<CleaningLog>("CleaningLogs");
        _logger = logger;
    }

    public async Task<ErrorOr<CleaningLog>> Handle(GetCleaningLogByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for cleaning log with ID: {Id}", request.Id);

        var log = await _collection.Find(l => l.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (log == null)
        {
            _logger.LogWarning("Cleaning log with ID: {Id} was not found", request.Id);
            return CleaningLogErrors.NotFound(request.Id);
        }

        _logger.LogInformation("Found cleaning log for Bin: {BinId}", log.BinId);
        return log;
    }
}
