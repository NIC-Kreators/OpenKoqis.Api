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
    public async Task<IActionResult> GetAsync() =>
        (await mediator.Send(new GetAllUsersQuery())).Match(Ok, Problem);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id) =>
        (await mediator.Send(new GetUserByIdQuery(id))).Match(Ok, Problem);

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] User user) =>
        (await mediator.Send(new CreateUserCommand(user))).Match(
            created => CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created),
            Problem);

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(string id, [FromBody] User user) =>
        (await mediator.Send(new UpdateUserCommand(id, user))).Match(
            _ => NoContent(),
            Problem);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id) =>
        (await mediator.Send(new DeleteUserCommand(id))).Match(
            _ => NoContent(),
            Problem);

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDto registrationDto) =>
        (await mediator.Send(new RegisterUserCommand(registrationDto))).Match(Ok, Problem);

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto loginDto) =>
        (await mediator.Send(new LoginUserCommand(loginDto.Nickname, loginDto.Password))).Match(Ok, Problem);
}
