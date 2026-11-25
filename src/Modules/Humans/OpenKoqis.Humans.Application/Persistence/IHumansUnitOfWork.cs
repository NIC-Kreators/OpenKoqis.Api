using OpenKoqis.Humans.Domain.Roles.Errors;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Application.Persistence;

namespace OpenKoqis.Humans.Application.Persistence;

/// <summary>
/// The Humans module's unit of work. <c>SaveChangesAsync</c> reports an attempt to persist
/// a second root user as <see cref="UserErrors.RootAlreadyExists"/>, a second user linked to the same identity
/// as <see cref="UserErrors.IdentityAlreadyLinked"/>, and a role whose name another role already has, compared
/// case-insensitively, as <see cref="RoleErrors.NameAlreadyExists"/>.
/// </summary>
public interface IHumansUnitOfWork : IUnitOfWork;
