using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Application.Features.ShiftLogs.Errors;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Commands;

public record EndShiftCommand(
    string ShiftId,
    DateTime EndedAt,
    IEnumerable<string> CleanedBinIds,
    double DistanceKm,
    string? Route = null) : IRequest<ErrorOr<Success>>;

public class EndShiftCommandHandler(IMongoDatabase database, ILogger<EndShiftCommandHandler> logger) : IRequestHandler<EndShiftCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<ShiftLog> _shiftCollection = database.GetCollection<ShiftLog>("ShiftLogs");
    private readonly IMongoCollection<Bin> _binCollection = database.GetCollection<Bin>("Bins");

    public async ValueTask<ErrorOr<Success>> Handle(EndShiftCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to end shift: {ShiftId}", request.ShiftId);

        var shift = await _shiftCollection.Find(s => s.Id == request.ShiftId).FirstOrDefaultAsync(cancellationToken);
        if (shift is null)
        {
            logger.LogWarning("EndShift failed: Shift with ID {ShiftId} not found", request.ShiftId);
            return ShiftLogErrors.NotFound(request.ShiftId);
        }

        var validBinIds = new List<string>();
        foreach (var binId in request.CleanedBinIds)
        {
            if (await _binCollection.Find(b => b.Id == binId).AnyAsync(cancellationToken))
            {
                validBinIds.Add(binId);
            }
            else
            {
                logger.LogWarning("Bin with ID {BinId} skipped: not found in database", binId);
            }
        }

        var endedAtValue = request.EndedAt == default ? DateTime.UtcNow : request.EndedAt;
        var routeValue = request.Route ?? shift.Route;

        var filter = Builders<ShiftLog>.Filter.Eq(s => s.Id, request.ShiftId);
        var update = Builders<ShiftLog>.Update
            .Set(s => s.EndedAt, (DateTime?)endedAtValue)
            .Set(s => s.CleanedBins, validBinIds)
            .Set(s => s.DistanceTravelledKm, request.DistanceKm)
            .Set(s => s.Route, routeValue)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _shiftCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        logger.LogInformation("Shift {ShiftId} ended successfully. Bins cleaned: {Count}. Distance: {Distance} km",
            request.ShiftId, validBinIds.Count, request.DistanceKm);

        return Result.Success;
    }
}
