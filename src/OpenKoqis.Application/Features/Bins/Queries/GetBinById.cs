using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Queries;

public record GetBinByIdQuery(string Id) : IRequest<ErrorOr<Bin>>;

public class GetBinByIdQueryHandler(IMongoDatabase database, ILogger<GetBinByIdQueryHandler> logger) : IRequestHandler<GetBinByIdQuery, ErrorOr<Bin>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<Bin>> Handle(GetBinByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Searching for bin with ID: {BinId}", request.Id);

        var bin = await _collection.Find(b => b.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (bin is null)
        {
            logger.LogWarning("Bin with ID: {BinId} was not found", request.Id);
            return BinErrors.NotFound(request.Id);
        }

        logger.LogInformation("Bin {BinId} found", request.Id);
        return bin;
    }
}
