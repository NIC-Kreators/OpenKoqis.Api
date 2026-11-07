using AwesomeAssertions;
using NSubstitute;
using OpenKoqis.Humans.Application.Dto;
using OpenKoqis.Humans.Application.Features.Users;
using OpenKoqis.Humans.Application.Features.Users.Queries;
using OpenKoqis.Humans.Application.Services;
using OpenKoqis.Shared.Application.Dto;
using static OpenKoqis.Humans.Application.UnitTests.Fixtures;

namespace OpenKoqis.Humans.Application.UnitTests.Users;

public class SearchIdentitiesQueryTests
{
    private readonly IIdentityProvider _identityProvider = Substitute.For<IIdentityProvider>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private SearchIdentitiesQueryHandler CreateHandler() => new(_identityProvider, _userRepository);

    private void ProviderReturns(IReadOnlyList<Identity> identities, uint pagesAvailable = 1)
        => _identityProvider
            .SearchByLogin(Arg.Any<string>(), Arg.Any<PaginatedRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponseDto<Identity>(identities, pagesAvailable));

    private void LinkedAre(params string[] linkedIds)
        => _userRepository
            .GetLinkedIdentityIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(linkedIds));

    /// <summary>
    /// Each returned identity is mapped with its fields and the linked flag, in the provider's order.
    /// </summary>
    [Test]
    public async Task Handle_IdentitiesWithLinkedAndUnlinked_MapsFieldsLinkedFlagAndPreservesOrder()
    {
        // Arrange
        var first = IdentityOf("anna", "id-1", isEnabled: true);
        var second = IdentityOf("boris", "id-2", isEnabled: false);
        var third = IdentityOf("clara", "id-3", isEnabled: true);
        ProviderReturns([first, second, third], pagesAvailable: 4);
        LinkedAre("id-1", "id-3");
        var request = new SearchIdentitiesQueryRequest { Term = "a" };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PagesAvailable.Should().Be(4);
        result.Value.Data.Should().Equal(
            new IdentityDto("id-1", "anna", true, true),
            new IdentityDto("id-2", "boris", false, false),
            new IdentityDto("id-3", "clara", true, true));
    }

    /// <summary>
    /// The linked-identity lookup is asked about exactly the ids of the returned page.
    /// </summary>
    [Test]
    public async Task Handle_PageReturned_ChecksLinkedForIdsOfThePage()
    {
        // Arrange
        ProviderReturns([IdentityOf("anna", "id-1"), IdentityOf("boris", "id-2")]);
        LinkedAre();
        var request = new SearchIdentitiesQueryRequest { Term = "a" };

        // Act
        await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        await _userRepository.Received().GetLinkedIdentityIds(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2 && ids.Contains("id-1") && ids.Contains("id-2")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The term and the requested page and page size are passed to the identity provider unchanged.
    /// </summary>
    [Test]
    public async Task Handle_TermAndPaging_PassesThemToProvider()
    {
        // Arrange
        ProviderReturns([]);
        LinkedAre();
        var request = new SearchIdentitiesQueryRequest { Term = "iva", Page = 3, PageSize = 50 };

        // Act
        await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        await _identityProvider.Received(1).SearchByLogin(
            "iva",
            Arg.Is<PaginatedRequestDto>(p => p.Page == 3 && p.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An empty term is passed as it is, so every identity can be matched.
    /// </summary>
    [Test]
    public async Task Handle_EmptyTerm_PassesEmptyTermToProvider()
    {
        // Arrange
        ProviderReturns([]);
        LinkedAre();
        var request = new SearchIdentitiesQueryRequest { Term = "" };

        // Act
        await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        await _identityProvider.Received(1).SearchByLogin(
            "",
            Arg.Any<PaginatedRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// An empty page yields empty data and the provider's number of available pages.
    /// </summary>
    [Test]
    public async Task Handle_EmptyPage_ReturnsEmptyDataWithProvidersPagesAvailable()
    {
        // Arrange
        ProviderReturns([], pagesAvailable: 7);
        LinkedAre();
        var request = new SearchIdentitiesQueryRequest { Term = "nobody" };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Data.Should().BeEmpty();
        result.Value.PagesAvailable.Should().Be(7);
    }

    /// <summary>
    /// Page 0 is rejected with a validation error and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_PageZero_ReturnsPageZeroErrorAndCallsNoPort()
    {
        // Arrange
        var request = new SearchIdentitiesQueryRequest { Term = "a", Page = 0, PageSize = 10 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageZero");
        await _identityProvider.DidNotReceiveWithAnyArgs().SearchByLogin(null!);
        await _userRepository.DidNotReceiveWithAnyArgs().GetLinkedIdentityIds(null!);
    }

    /// <summary>
    /// A page size of 0 is rejected with a validation error and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeZero_ReturnsPageSizeOutOfRangeErrorAndCallsNoPort()
    {
        // Arrange
        var request = new SearchIdentitiesQueryRequest { Term = "a", Page = 1, PageSize = 0 };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _identityProvider.DidNotReceiveWithAnyArgs().SearchByLogin(null!);
        await _userRepository.DidNotReceiveWithAnyArgs().GetLinkedIdentityIds(null!);
    }

    /// <summary>
    /// A page size above the maximum is rejected with a validation error and no port is called.
    /// </summary>
    [Test]
    public async Task Handle_PageSizeOverMax_ReturnsPageSizeOutOfRangeErrorAndCallsNoPort()
    {
        // Arrange
        var request = new SearchIdentitiesQueryRequest
        {
            Term = "a",
            Page = 1,
            PageSize = PaginatedRequestDto.MaxPageSize + 1,
        };

        // Act
        var result = await CreateHandler().Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Select(e => e.Code).Should().Contain("PaginatedRequest.PageSizeOutOfRange");
        await _identityProvider.DidNotReceiveWithAnyArgs().SearchByLogin(null!);
        await _userRepository.DidNotReceiveWithAnyArgs().GetLinkedIdentityIds(null!);
    }
}
