using AwesomeAssertions;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Queries;
using OpenKoqis.Humans.Domain.Users.Errors;
using OpenKoqis.Shared.Application.Dto;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class GetUsersQueryTests
{
    private readonly IUserReadStore _store = Substitute.For<IUserReadStore>();

    private GetUsersQueryHandler CreateHandler() => new(_store);

    private static UserDto DtoOf(string login) => new()
    {
        Id = Guid.NewGuid(),
        IdentityId = $"identity-{login}",
        Login = login,
        FullName = "Ivan Ivanov",
        IsRoot = false,
        DedicatedAccess = ["bins:read"],
    };

    private void ReturnsPage(PaginatedResponseDto<UserDto> page)
        => _store.GetUsers(Arg.Any<UserFilter>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>()).Returns(page);

    /// <summary>
    /// Without filters the store receives an empty filter and the handler returns the store's page unchanged.
    /// </summary>
    [Test]
    public async Task Handle_NoFilters_QueriesStoreWithEmptyFilterAndReturnsItsPage()
    {
        // Arrange
        IReadOnlyList<UserDto> data = [DtoOf("ivan"), DtoOf("petr")];
        ReturnsPage(new PaginatedResponseDto<UserDto>(data, 7));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeSameAs(data);
        result.Value.PagesAvailable.Should().Be(7);
        await _store.Received(1).GetUsers(new UserFilter(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A search with surrounding whitespace is trimmed before it reaches the store.
    /// </summary>
    [Test]
    public async Task Handle_SearchWithSurroundingWhitespace_PassesTrimmedSearch()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Search = "  Ivan Ivanov \t" }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            new UserFilter { Search = "Ivan Ivanov" },
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
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Search = search }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            Arg.Is<UserFilter>(f => f.Search == null),
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A role id reaches the store in the filter.
    /// </summary>
    [Test]
    public async Task Handle_RoleIdGiven_PassesRoleIdInFilter()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { RoleId = roleId }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            new UserFilter { RoleId = roleId },
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The deleted flag reaches the store in the filter.
    /// </summary>
    [Test]
    public async Task Handle_IsDeleted_PassesIsDeletedInFilter()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { IsDeleted = true }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            new UserFilter { IsDeleted = true },
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Search, role id and the deleted flag all reach the store in one filter.
    /// </summary>
    [Test]
    public async Task Handle_AllFiltersGiven_PassesAllInOneFilter()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));
        var request = new GetUsersQueryRequest { Search = " Ivan ", RoleId = roleId, IsDeleted = true };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            new UserFilter { Search = "Ivan", RoleId = roleId, IsDeleted = true },
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
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Page = 3, PageSize = 50 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _store.Received(1).GetUsers(
            Arg.Any<UserFilter>(),
            Arg.Is<PaginatedRequestDto>(p => p.Page == 3 && p.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An empty role id is rejected with an empty-role-id error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_EmptyRoleId_ReturnsEmptyRoleIdErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetUsersQueryRequest { RoleId = Guid.Empty };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain(UserErrors.EmptyRoleId.Code);
        await _store.DidNotReceiveWithAnyArgs().GetUsers(default!, default, default);
    }

    /// <summary>
    /// Page 0 is rejected with a page-zero error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageZero_ReturnsPageZeroErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Search = "Ivan", Page = 0, PageSize = 10 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageZero");
        await _store.DidNotReceiveWithAnyArgs().GetUsers(default!, default, default);
    }

    /// <summary>
    /// Page size 0 is rejected with an out-of-range error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeZero_ReturnsPageSizeErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Page = 1, PageSize = 0 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _store.DidNotReceiveWithAnyArgs().GetUsers(default!, default, default);
    }

    /// <summary>
    /// A page size above the maximum is rejected with an out-of-range error and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeOverMax_ReturnsPageSizeErrorAndSkipsStore()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Page = 1, PageSize = PaginatedRequestDto.MaxPageSize + 1 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _store.DidNotReceiveWithAnyArgs().GetUsers(default!, default, default);
    }

    /// <summary>
    /// Invalid paging and an empty role id are reported together and the store is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageZeroAndEmptyRoleId_ReturnsBothErrorsAndSkipsStore()
    {
        // Arrange
        var request = new GetUsersQueryRequest { RoleId = Guid.Empty, Page = 0 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageZero")
            .And.Contain(UserErrors.EmptyRoleId.Code);
        await _store.DidNotReceiveWithAnyArgs().GetUsers(default!, default, default);
    }

    /// <summary>
    /// An empty page returns empty data and the store's pages-available count.
    /// </summary>
    [Test]
    public async Task Handle_EmptyPage_ReturnsEmptyDataWithPagesAvailable()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<UserDto>([], 5));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Page = 9 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeEmpty();
        result.Value.PagesAvailable.Should().Be(5);
    }
}
