using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Commands;

public record CreateBinCommand(BinType Type, GeoPoint Location, BinTelemetry Telemetry, BinStatus Status) : IRequest<ErrorOr<Bin>>;

public class CreateBinCommandHandler(IMongoDatabase database, ILogger<CreateBinCommandHandler> logger) : IRequestHandler<CreateBinCommand, ErrorOr<Bin>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async ValueTask<ErrorOr<Bin>> Handle(CreateBinCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new bin of type {BinType}", request.Type);

        var bin = new Bin(ObjectId.GenerateNewId().ToString(), request.Type, request.Location, request.Status);
        bin.UpdateTelemetry(request.Telemetry);

        await _collection.InsertOneAsync(bin, cancellationToken: cancellationToken);
        logger.LogInformation("Bin {BinId} created successfully", bin.Id);

        return bin;
    }
}
