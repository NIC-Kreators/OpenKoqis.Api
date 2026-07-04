using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.CleaningLogs.Commands;

public record LogBinCleaningCommand(string BinId, string UserId, int RemovedKg, string? Notes = null) : IRequest<ErrorOr<CleaningLog>>;

public class LogBinCleaningCommandHandler : IRequestHandler<LogBinCleaningCommand, ErrorOr<CleaningLog>>
{
    private readonly IMongoCollection<CleaningLog> _logCollection;
    private readonly IMongoCollection<Bin> _binCollection;
    private readonly ILogger<LogBinCleaningCommandHandler> _logger;

    public LogBinCleaningCommandHandler(IMongoDatabase database, ILogger<LogBinCleaningCommandHandler> logger)
    {
        _logCollection = database.GetCollection<CleaningLog>("CleaningLogs");
        _binCollection = database.GetCollection<Bin>("Bins");
        _logger = logger;
    }

    public async Task<ErrorOr<CleaningLog>> Handle(LogBinCleaningCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting LogCleaning process for Bin: {BinId} by User: {UserId}", request.BinId, request.UserId);

        var bin = await _binCollection.Find(b => b.Id == request.BinId).FirstOrDefaultAsync(cancellationToken);
        if (bin == null)
        {
            _logger.LogWarning("LogCleaning failed: Bin {BinId} does not exist", request.BinId);
            return CleaningLogErrors.BinNotFound(request.BinId);
        }

        var cleaning = new CleaningLog
        {
            BinId = ObjectId.Parse(request.BinId),
            UserId = ObjectId.Parse(request.UserId),
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow,
            RemovedWeightKg = request.RemovedKg,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _logCollection.InsertOneAsync(cleaning, cancellationToken: cancellationToken);

        _logger.LogInformation("Updating Bin {Id} status to Active after cleaning", request.BinId);

        var binUpdate = Builders<Bin>.Update
            .Set(b => b.Status, BinStatus.Active)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        await _binCollection.UpdateOneAsync(b => b.Id == request.BinId, binUpdate, cancellationToken: cancellationToken);
        _logger.LogInformation("Cleaning process completed. Recorded {Weight}kg removed", request.RemovedKg);

        return cleaning;
    }
}
