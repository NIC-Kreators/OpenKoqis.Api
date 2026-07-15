using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Features.ShiftLogs.Commands;

public record DeleteShiftLogCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public class DeleteShiftLogCommandHandler(IMongoDatabase database, ILogger<DeleteShiftLogCommandHandler> logger) : IRequestHandler<DeleteShiftLogCommand, ErrorOr<Deleted>>
{
    private readonly IMongoCollection<ShiftLog> _collection = database.GetCollection<ShiftLog>("ShiftLogs");

    public async Task<ErrorOr<Deleted>> Handle(DeleteShiftLogCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request to delete shift log: {Id}", request.Id);

        var result = await _collection.DeleteOneAsync(s => s.Id == request.Id, cancellationToken);

        if (result.DeletedCount == 0)
        {
            logger.LogWarning("Delete failed: ShiftLog {Id} not found", request.Id);
            return ShiftLogErrors.NotFound(request.Id);
        }

        logger.LogInformation("Shift log {Id} deleted successfully", request.Id);
        return Result.Deleted;
    }
}
