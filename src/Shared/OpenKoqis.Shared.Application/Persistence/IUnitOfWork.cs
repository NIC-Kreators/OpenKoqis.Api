using ErrorOr;

namespace OpenKoqis.Shared.Application.Persistence;

public interface IUnitOfWork
{
    Task<ErrorOr<Success>> SaveChangesAsync(CancellationToken ct = default);
}
