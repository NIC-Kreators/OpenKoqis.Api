using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Queries;

public record GetShiftLogByIdQuery(string Id) : IRequest<ErrorOr<ShiftLog>>;

public class GetShiftLogByIdQueryHandler : IRequestHandler<GetShiftLogByIdQuery, ErrorOr<ShiftLog>>
{
    private readonly IMongoCollection<ShiftLog> _collection;
    private readonly ILogger<GetShiftLogByIdQueryHandler> _logger;

    public GetShiftLogByIdQueryHandler(IMongoDatabase database, ILogger<GetShiftLogByIdQueryHandler> logger)
    {
        _collection = database.GetCollection<ShiftLog>("ShiftLogs");
        _logger = logger;
    }

    public async Task<ErrorOr<ShiftLog>> Handle(GetShiftLogByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for shift log with ID: {Id}", request.Id);

        var log = await _collection.Find(s => s.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (log == null)
        {
            _logger.LogWarning("Shift log with ID: {Id} was not found", request.Id);
            return ShiftLogErrors.NotFound(request.Id);
        }

        _logger.LogInformation("Shift log with ID: {Id} found", request.Id);
        return log;
    }
}
