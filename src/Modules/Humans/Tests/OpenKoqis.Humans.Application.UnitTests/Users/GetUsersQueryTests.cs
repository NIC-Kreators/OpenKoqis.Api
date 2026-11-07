using AwesomeAssertions;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Queries;
using OpenKoqis.Humans.Domain.Users;
using OpenKoqis.Humans.Domain.Users.Types;
using OpenKoqis.Shared.Application.Dto;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class GetUsersQueryTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private GetUsersQueryHandler CreateHandler() => new(_userRepository);

    private static User UserWithContacts(string login, string identityId, string fullName, string email, string phone)
        => User.Create(new User.CreationAttributes
        {
            Login = LoginOf(login),
            IdentityId = identityId,
            Name = HumanNameOf(fullName),
            Email = Email.Parse(email).Value,
            PhoneNumber = PhoneNumber.Parse(phone).Value,
            DedicatedAccess = AccessesOf("bins:read"),
        }).Value;

    private void ReturnsPage(PaginatedResponseDto<User> page)
    {
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>()).Returns(page);
        _userRepository.SearchUsersByName(Arg.Any<string>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>()).Returns(page);
    }

    /// <summary>
    /// Without a name filter every returned user is mapped to a <see cref="UserDto"/> with all fields, in repository order.
    /// </summary>
    [Test]
    public async Task Handle_NoName_ReturnsAllUsersMappedInOrderWithPagesAvailable()
    {
        // Arrange
        var first = UserWithContacts("ivan", "identity-1", "Ivan Ivanov", "ivan@example.com", "+77011234567");
        var second = RegularUser("petr", "identity-2");
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([first, second], 7));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PagesAvailable.Should().Be(7);
        result.Value.Data.Should().HaveCount(2);
        result.Value.Data[0].Should().BeEquivalentTo(UserDto.FromUser(first));
        result.Value.Data[1].Should().BeEquivalentTo(UserDto.FromUser(second));
        result.Value.Data[0].Id.Should().Be(first.Id);
        result.Value.Data[0].Email.Should().Be("ivan@example.com");
        result.Value.Data[1].Id.Should().Be(second.Id);
    }

    /// <summary>
    /// The root user and regular users are mapped alike.
    /// </summary>
    [Test]
    public async Task Handle_RootAndRegularUsers_MapsBothAlike()
    {
        // Arrange
        var root = RootUser();
        var regular = RegularUser("ivan", "identity-1");
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([root, regular], 1));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Select(d => d.Id).Should().Equal(root.Id, regular.Id);
        result.Value.Data[0].Should().BeEquivalentTo(UserDto.FromUser(root));
        result.Value.Data[1].Should().BeEquivalentTo(UserDto.FromUser(regular));
    }

    /// <summary>
    /// A name filter searches by that name and does not use the unfiltered listing.
    /// </summary>
    [Test]
    public async Task Handle_NameGiven_SearchesByNameAndSkipsUnfilteredListing()
    {
        // Arrange
        var user = RegularUser("ivan", "identity-1");
        _userRepository.SearchUsersByName("Ivan", Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([user], 3));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Name = "Ivan" }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PagesAvailable.Should().Be(3);
        result.Value.Data.Select(d => d.Id).Should().Equal(user.Id);
        await _userRepository.Received(1).SearchUsersByName("Ivan", Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A name with surrounding whitespace is trimmed before searching.
    /// </summary>
    [Test]
    public async Task Handle_NameWithSurroundingSpaces_SearchesWithTrimmedName()
    {
        // Arrange
        ReturnsPage(new PaginatedResponseDto<User>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Name = "  Ivan Ivanov \t" }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _userRepository.Received(1).SearchUsersByName("Ivan Ivanov", Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A null, empty or whitespace name lists users without filtering and never searches.
    /// </summary>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Handle_BlankName_ListsUnfilteredAndSkipsSearch(string? name)
    {
        // Arrange
        var user = RegularUser("ivan", "identity-1");
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([user], 2));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Name = name }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Select(d => d.Id).Should().Equal(user.Id);
        await _userRepository.Received(1).GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().SearchUsersByName(Arg.Any<string>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Without a name filter the requested page and page size reach the unfiltered listing.
    /// </summary>
    [Test]
    public async Task Handle_NoName_PassesPagingToUnfilteredListing()
    {
        // Arrange
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Page = 3, PageSize = 50 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _userRepository.Received(1).GetPaginatedUsers(
            Arg.Is<PaginatedRequestDto>(p => p.Page == 3 && p.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// With a name filter the requested page and page size reach the search.
    /// </summary>
    [Test]
    public async Task Handle_NameGiven_PassesPagingToSearch()
    {
        // Arrange
        _userRepository.SearchUsersByName(Arg.Any<string>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([], 0));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Name = "Ivan", Page = 4, PageSize = 15 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _userRepository.Received(1).SearchUsersByName(
            "Ivan",
            Arg.Is<PaginatedRequestDto>(p => p.Page == 4 && p.PageSize == 15),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An empty page returns empty data and the repository's pages-available count.
    /// </summary>
    [Test]
    public async Task Handle_EmptyPage_ReturnsEmptyDataWithPagesAvailable()
    {
        // Arrange
        _userRepository.GetPaginatedUsers(Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<User>([], 5));

        // Act
        var result = await CreateHandler().Handle(new GetUsersQueryRequest { Page = 9 }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeEmpty();
        result.Value.PagesAvailable.Should().Be(5);
    }

    /// <summary>
    /// Page 0 is rejected with a page-zero error and the repository is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageZero_ReturnsPageZeroErrorAndSkipsRepository()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Name = "Ivan", Page = 0, PageSize = 10 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageZero");
        await _userRepository.DidNotReceiveWithAnyArgs().SearchUsersByName(default!, default, default);
        await _userRepository.DidNotReceiveWithAnyArgs().GetPaginatedUsers(default, default);
    }

    /// <summary>
    /// Page size 0 is rejected with an out-of-range error and the repository is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeZero_ReturnsPageSizeErrorAndSkipsRepository()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Page = 1, PageSize = 0 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _userRepository.DidNotReceiveWithAnyArgs().SearchUsersByName(default!, default, default);
        await _userRepository.DidNotReceiveWithAnyArgs().GetPaginatedUsers(default, default);
    }

    /// <summary>
    /// A page size above the maximum is rejected with an out-of-range error and the repository is not called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeOverMax_ReturnsPageSizeErrorAndSkipsRepository()
    {
        // Arrange
        var request = new GetUsersQueryRequest { Page = 1, PageSize = PaginatedRequestDto.MaxPageSize + 1 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _userRepository.DidNotReceiveWithAnyArgs().SearchUsersByName(default!, default, default);
        await _userRepository.DidNotReceiveWithAnyArgs().GetPaginatedUsers(default, default);
    }
}
