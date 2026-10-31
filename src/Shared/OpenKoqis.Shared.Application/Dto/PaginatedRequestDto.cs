using ErrorOr;

namespace OpenKoqis.Shared.Application.Dto;

public readonly record struct PaginatedRequestDto
{
    public const uint DefaultPageSize = 20;
    public const uint MaxPageSize = 1000;

    private readonly uint _page;
    private readonly uint _pageSize;

    public uint Page => _page == 0 ? 1 : _page;
    public uint PageSize => _pageSize == 0 ? DefaultPageSize : _pageSize;

    private PaginatedRequestDto(uint page, uint pageSize)
    {
        (_page, _pageSize) = (page, pageSize);
    }

    public static ErrorOr<PaginatedRequestDto> Create(uint page, uint pageSize)
    {
        if (page == 0)
            return Error.Validation(
                code: "PaginatedRequest.PageZero",
                description: "You cannot request the page 0. Start from 1.");

        if (pageSize is 0 or > MaxPageSize)
            return Error.Validation(
                code: "PaginatedRequest.PageSizeOutOfRange",
                description: "You can request only page size between 1 and 999");

        return new PaginatedRequestDto(page, pageSize);
    }
}
