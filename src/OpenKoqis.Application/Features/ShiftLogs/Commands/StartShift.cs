using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Application.Features.ShiftLogs.Errors;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Commands;

public record StartShiftCommand(string UserId) : IRequest<ErrorOr<ShiftLog>>;

public class StartShiftCommandHandler(IMongoDatabase database, ILogger<StartShiftCommandHandler> logger) : IRequestHandler<StartShiftCommand, ErrorOr<ShiftLog>>
{
    private readonly IMongoCollection<ShiftLog> _shiftCollection = database.GetCollection<ShiftLog>("ShiftLogs");
    private readonly IMongoCollection<BsonDocument> _userCollection = database.GetCollection<BsonDocument>("Users");

    public async ValueTask<ErrorOr<ShiftLog>> Handle(StartShiftCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to start a new shift for User: {UserId}", request.UserId);

        var userExists = await _userCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(request.UserId))).AnyAsync(cancellationToken);

        if (!userExists)
        {
            logger.LogWarning("StartShift failed: User with ID {UserId} does not exist", request.UserId);
            return ShiftLogErrors.UserNotFound(request.UserId);
        }

        var shift = new ShiftLog(ObjectId.GenerateNewId().ToString(), request.UserId, string.Empty);

        await _shiftCollection.InsertOneAsync(shift, cancellationToken: cancellationToken);
        logger.LogInformation("New shift started and saved. ShiftId: {ShiftId} for User: {UserId}", shift.Id, request.UserId);

        return shift;
    }
}
