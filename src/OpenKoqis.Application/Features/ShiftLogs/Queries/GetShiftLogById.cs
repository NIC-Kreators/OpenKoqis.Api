using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Queries;

public record GetShiftLogByIdQuery(string Id) : IRequest<ErrorOr<ShiftLog>>;

public class GetShiftLogByIdQueryHandler(IMongoDatabase database, ILogger<GetShiftLogByIdQueryHandler> logger) : IRequestHandler<GetShiftLogByIdQuery, ErrorOr<ShiftLog>>
{
    private readonly IMongoCollection<ShiftLog> _collection = database.GetCollection<ShiftLog>("ShiftLogs");
   
    public async Task<ErrorOr<ShiftLog>> Handle(GetShiftLogByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching for shift log with ID: {Id}", request.Id);

        var log = await _collection.Find(s => s.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            logger.LogWarning("Shift log with ID: {Id} was not found", request.Id);
            return ShiftLogErrors.NotFound(request.Id);
        }

        logger.LogInformation("Shift log with ID: {Id} found", request.Id);
        return log;
    }
}
