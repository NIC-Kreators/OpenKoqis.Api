using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Application.Persistence;

namespace OpenKoqis.Humans.Application.Persistence;

/// <summary>
/// The Humans module's unit of work. <c>SaveChangesAsync</c> reports an attempt to persist
/// a second root user as <see cref="UserErrors.RootAlreadyExists"/>, and a second user linked to the same identity
/// as <see cref="UserErrors.IdentityAlreadyLinked"/>.
/// </summary>
public interface IHumansUnitOfWork : IUnitOfWork;
