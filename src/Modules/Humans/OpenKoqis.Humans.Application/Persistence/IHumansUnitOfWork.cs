using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Application.Persistence;

namespace OpenKoqis.Humans.Application.Persistence;

/// <summary>
/// The Humans module's unit of work. <c>SaveChangesAsync</c> reports an attempt to persist
/// a second root user as <see cref="UserErrors.RootAlreadyExists"/>.
/// </summary>
public interface IHumansUnitOfWork : IUnitOfWork;
