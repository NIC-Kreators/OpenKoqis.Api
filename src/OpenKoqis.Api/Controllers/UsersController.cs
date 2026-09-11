using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Users.Commands;
using OpenKoqis.Application.Features.Users.Queries;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        (await mediator.Send(new GetAllUsersQuery(), cancellationToken)).Match(Ok, Problem);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetUserByIdQuery(id), cancellationToken)).Match(Ok, Problem);

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] User user, CancellationToken cancellationToken) =>
        (await mediator.Send(new CreateUserCommand(user), cancellationToken)).Match(
            created => CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created),
            Problem);

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(string id, [FromBody] User user, CancellationToken cancellationToken) =>
        (await mediator.Send(new UpdateUserCommand(id, user), cancellationToken)).Match(
            _ => NoContent(),
            Problem);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken) =>
        (await mediator.Send(new DeleteUserCommand(id), cancellationToken)).Match(
            _ => NoContent(),
            Problem);

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDto registrationDto, CancellationToken cancellationToken) =>
        (await mediator.Send(new RegisterUserCommand(registrationDto), cancellationToken)).Match(Ok, Problem);

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto loginDto, CancellationToken cancellationToken) =>
        (await mediator.Send(new LoginUserCommand(loginDto.Nickname, loginDto.Password), cancellationToken)).Match(Ok, Problem);
}
