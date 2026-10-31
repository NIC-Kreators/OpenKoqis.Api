namespace OpenKoqis.Humans.Application.Dto;

public readonly record struct TokensDto(
    string AccessToken,
    string RefreshToken
);
