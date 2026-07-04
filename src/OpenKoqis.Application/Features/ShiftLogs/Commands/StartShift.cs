using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Commands;

public record StartShiftCommand(string UserId) : IRequest<ErrorOr<ShiftLog>>;

public class StartShiftCommandHandler : IRequestHandler<StartShiftCommand, ErrorOr<ShiftLog>>
{
    private readonly IMongoCollection<ShiftLog> _shiftCollection;
    private readonly IMongoCollection<BsonDocument> _userCollection;
    private readonly ILogger<StartShiftCommandHandler> _logger;

    public StartShiftCommandHandler(IMongoDatabase database, ILogger<StartShiftCommandHandler> logger)
    {
        _shiftCollection = database.GetCollection<ShiftLog>("ShiftLogs");
        _userCollection = database.GetCollection<BsonDocument>("Users");
        _logger = logger;
    }

    public async Task<ErrorOr<ShiftLog>> Handle(StartShiftCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to start a new shift for User: {UserId}", request.UserId);

        var userFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(request.UserId));
        var userExists = await _userCollection.Find(userFilter).AnyAsync(cancellationToken);

        if (!userExists)
        {
            _logger.LogWarning("StartShift failed: User with ID {UserId} does not exist", request.UserId);
            return ShiftLogErrors.UserNotFound(request.UserId);
        }

        var shift = new ShiftLog
        {
            UserId = ObjectId.Parse(request.UserId),
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.MinValue,
            CleanedBins = [],
            DistanceTravelledKm = 0,
            Route = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _shiftCollection.InsertOneAsync(shift, cancellationToken: cancellationToken);
        _logger.LogInformation("New shift started and saved. ShiftId: {ShiftId} for User: {UserId}", shift.Id, request.UserId);

        return shift;
    }
}
