using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Queries;

public record GetAllBinsQuery : IRequest<ErrorOr<List<Bin>>>;

public class GetAllBinsQueryHandler(IMongoDatabase database, ILogger<GetAllBinsQueryHandler> logger) : IRequestHandler<GetAllBinsQuery, ErrorOr<List<Bin>>>
{
    private readonly IMongoCollection<Bin> _collection = database.GetCollection<Bin>("Bins");

    public async Task<ErrorOr<List<Bin>>> Handle(GetAllBinsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all bins from database");

        var bins = await _collection.Find(FilterDefinition<Bin>.Empty).ToListAsync(cancellationToken);

        logger.LogInformation("Successfully retrieved {Count} bins", bins.Count);
        return bins;
    }
}
