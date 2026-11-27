using AwesomeAssertions;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Roles;
using OpenKoqis.Humans.Application.Features.Roles.Queries;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.UnitTests.Roles;

public class GetRolesQueryTests
{
    private readonly IRoleReadStore _store = Substitute.For<IRoleReadStore>();

    private GetRolesQueryHandler CreateHandler() => new(_store);

    private static RoleSummaryDto DtoOf(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Accesses = ["bins:read"],
        CreatedBy = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        UsersCount = 2,
    };

    private void ReturnsPage(PaginatedResponseDto<RoleSummaryDto> page)
        => _store.GetRoles(Arg.Any<RoleFilter>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>()).Returns(page);

    /// <summary>
    /// Without a search the store receives an empty filter and the handler returns the store's page unchanged.
    /// </summary>
    [Test]
    public async Task Handle_NoSearch_QueriesStoreWithEmptyFilterAndReturnsItsPage()
    {
        // Arrange
        IReadOnlyList<RoleSummaryDto> data = [DtoOf("Operators"), DtoOf("Managers")];
        ReturnsPage(new PaginatedResponseDto<RoleSummaryDto>(data, 7));

        // Act
        var result = await CreateHandler().Handle(new GetRolesQueryRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeSameAs(data);
        result.Value.PagesAvailable.Should().Be(7);
        await _store.Received(1).GetRoles(new RoleFilter(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A search with surrounding whitespace is trimmed before it reaches the store.
    /// </summary>
    [Test]
    public async Task Handle_SearchWithSurroundingWhitespace_PassesTrimmedSearch()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<RoleSummaryDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetRolesQueryRequest { Search = "  Operators \t" }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetRoles(
            new RoleFilter { Search = "Operators" },
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A null, empty or whitespace search reaches the store as no search.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Handle_BlankSearch_PassesNoSearch(string? search)
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<RoleSummaryDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetRolesQueryRequest { Search = search }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetRoles(
            Arg.Is<RoleFilter>(f => f.Search == null),
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The requested page and page size reach the store.
    /// </summary>
    [Test]
    public async Task Handle_PagingGiven_PassesPageAndPageSizeToStore()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<RoleSummaryDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetRolesQueryRequest { Page = 3, PageSize = 50 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetRoles(
            Arg.Any<RoleFilter>(),
            Arg.Is<PaginatedRequestDto>(p => p.Page == 3 && p.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Page 0 is rejected with a page-zero error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageZero_ReturnsPageZeroErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetRolesQueryRequest { Search = "Operators", Page = 0, PageSize = 10 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageZero");
        await _store.DidNotReceiveWithAnyArgs().GetRoles(null!);
    }

    /// <summary>
    /// Page size 0 is rejected with an out-of-range error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeZero_ReturnsPageSizeErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetRolesQueryRequest { Page = 1, PageSize = 0 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _store.DidNotReceiveWithAnyArgs().GetRoles(null!);
    }

    /// <summary>
    /// A page size above the maximum is rejected with an out-of-range error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeOverMax_ReturnsPageSizeErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetRolesQueryRequest { Page = 1, PageSize = PaginatedRequestDto.MaxPageSize + 1 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _store.DidNotReceiveWithAnyArgs().GetRoles(null!);
    }

    /// <summary>
    /// An empty page returns empty data and a pages-available count of zero.
    /// </summary>
    [Test]
    public async Task Handle_EmptyPage_ReturnsEmptyDataWithNoPagesAvailable()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<RoleSummaryDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetRolesQueryRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeEmpty();
        result.Value.PagesAvailable.Should().Be(0);
    }
}
