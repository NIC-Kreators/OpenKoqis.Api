namespace OpenKoqis.Shared.Application.Dto;

public readonly record struct PaginatedResponseDto<T>(IReadOnlyList<T> Data, uint PagesAvailable);
