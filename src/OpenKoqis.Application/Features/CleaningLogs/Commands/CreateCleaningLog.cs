using ErrorOr;
using Mediator;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record CreateCleaningLogCommand(string BinId, string UserId, int RemovedWeightKg, string Notes) : IRequest<ErrorOr<CleaningLog>>;

public class CreateCleaningLogCommandHandler(IMongoDatabase database) : IRequestHandler<CreateCleaningLogCommand, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _collection = database.GetCollection<CleaningLog>("CleaningLogs");

    public async ValueTask<ErrorOr<CleaningLog>> Handle(CreateCleaningLogCommand request, CancellationToken cancellationToken)
    {
        var log = new CleaningLog(
            ObjectId.GenerateNewId().ToString(),
            request.BinId,
            request.UserId,
            request.RemovedWeightKg,
            request.Notes);

        await _collection.InsertOneAsync(log, cancellationToken: cancellationToken);

        return log;
    }
}
