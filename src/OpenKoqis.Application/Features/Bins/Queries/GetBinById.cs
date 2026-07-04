using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.Bins.Queries;

public record GetBinByIdQuery(string Id) : IRequest<ErrorOr<Bin>>;

public class GetBinByIdQueryHandler : IRequestHandler<GetBinByIdQuery, ErrorOr<Bin>>
{
    private readonly IMongoCollection<Bin> _collection;
    private readonly ILogger<GetBinByIdQueryHandler> _logger;

    public GetBinByIdQueryHandler(IMongoDatabase database, ILogger<GetBinByIdQueryHandler> logger)
    {
        _collection = database.GetCollection<Bin>("Bins");
        _logger = logger;
    }

    public async Task<ErrorOr<Bin>> Handle(GetBinByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for bin with ID: {BinId}", request.Id);

        var bin = await _collection.Find(b => b.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (bin == null)
        {
            _logger.LogWarning("Bin with ID: {BinId} was not found", request.Id);
            return BinErrors.NotFound(request.Id);
        }

        _logger.LogInformation("Bin {BinId} found", request.Id);
        return bin;
    }
}
