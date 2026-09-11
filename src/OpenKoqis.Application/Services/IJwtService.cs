using OpenKoqis.Domain.Models;

namespace OpenKoqis.Application.Services;

public interface IJwtService
{
    Task<TokenPair> GenerateTokenPairAsync(string userId, string userName, UserRole role);

    Task<bool> IsRefreshTokenValidAsync(string userId, string refreshToken);

    Task RemoveRefreshTokenAsync(string userId, string refreshToken);
}

public record TokenPair(string AccessToken, string RefreshToken);
