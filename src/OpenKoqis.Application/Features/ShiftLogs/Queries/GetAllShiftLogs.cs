using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Queries;

public record GetAllShiftLogsQuery : IRequest<ErrorOr<List<ShiftLog>>>;

public class GetAllShiftLogsQueryHandler : IRequestHandler<GetAllShiftLogsQuery, ErrorOr<List<ShiftLog>>>
{
    private readonly IMongoCollection<ShiftLog> _collection;
    private readonly ILogger<GetAllShiftLogsQueryHandler> _logger;

    public GetAllShiftLogsQueryHandler(IMongoDatabase database, ILogger<GetAllShiftLogsQueryHandler> logger)
    {
        _collection = database.GetCollection<ShiftLog>("ShiftLogs");
        _logger = logger;
    }

    public async Task<ErrorOr<List<ShiftLog>>> Handle(GetAllShiftLogsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all shift logs from database");

        var logs = await _collection.Find(FilterDefinition<ShiftLog>.Empty).ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} shift logs", logs.Count);
        return logs;
    }
}
