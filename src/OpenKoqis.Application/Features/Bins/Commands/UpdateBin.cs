using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Commands;

public record UpdateBinCommand(string Id, BinType Type, GeoPoint Location, BinStatus Status) : IRequest<ErrorOr<Success>>;

public class UpdateBinCommandHandler(IMongoDatabase database, ILogger<UpdateBinCommandHandler> logger) : IRequestHandler<UpdateBinCommand, ErrorOr<Success>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<Success>> Handle(UpdateBinCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update bin {BinId}", request.Id);

        var filter = Builders<Bin>.Filter.Eq(b => b.Id, request.Id);
        var update = Builders<Bin>.Update
            .Set(b => b.Type, request.Type)
            .Set(b => b.Location, request.Location)
            .Set(b => b.Status, request.Status)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            logger.LogWarning("Update failed. Bin '{BinId}' not found", request.Id);
            return BinErrors.NotFound(request.Id);
        }

        logger.LogInformation("Bin {BinId} updated successfully", request.Id);
        return Result.Success;
    }
}
