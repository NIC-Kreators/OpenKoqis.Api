using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Queries;

public record GetAllShiftLogsQuery : IRequest<ErrorOr<List<ShiftLog>>>;

public class GetAllShiftLogsQueryHandler(IMongoDatabase database, ILogger<GetAllShiftLogsQueryHandler> logger) : IRequestHandler<GetAllShiftLogsQuery, ErrorOr<List<ShiftLog>>>
{
    private readonly IMongoCollection<ShiftLog> _collection = database.GetCollection<ShiftLog>("ShiftLogs");

    public async Task<ErrorOr<List<ShiftLog>>> Handle(GetAllShiftLogsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all shift logs from database");

        var logs = await _collection.Find(FilterDefinition<ShiftLog>.Empty).ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} shift logs", logs.Count);
        return logs;
    }
}
