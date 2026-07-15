using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Commands;

public record CreateBinCommand(BinType Type, GeoPoint Location, BinTelemetry Telemetry, BinStatus Status) : IRequest<ErrorOr<Bin>>;

public class CreateBinCommandHandler(IMongoDatabase database, ILogger<CreateBinCommandHandler> logger) : IRequestHandler<CreateBinCommand, ErrorOr<Bin>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<Bin>> Handle(CreateBinCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating a new bin of type {BinType}", request.Type);

        var bin = new Bin
        {
            Type = request.Type,
            Location = request.Location,
            Telemetry = request.Telemetry,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(bin, cancellationToken: cancellationToken);
        logger.LogInformation("Bin created successfully with ID: {BinId}", bin.Id);

        return bin;
    }
}
