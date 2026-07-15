using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Features.Users.Commands;
using OpenKoqis.Application.Features.Users.Queries;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender mediator, ILogger<UsersController> logger) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        logger.LogInformation("Fetching all users from the database");

        var result = await mediator.Send(new GetAllUsersQuery());

        return result.Match(
            users =>
            {
                logger.LogInformation("Successfully retrieved {Count} users", users.Count);
                return Ok(users);
            },
            HandleErrors
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        logger.LogInformation("Fetching user with ID: {UserId}", id);

        var result = await mediator.Send(new GetUserByIdQuery(id));

        return result.Match(Ok, HandleErrors);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] User user)
    {
        logger.LogInformation("Creating a new user: {Nickname}", user.Nickname);

        var result = await mediator.Send(new CreateUserCommand(user));

        return result.Match(
            created =>
            {
                logger.LogInformation("User created with ID: {UserId}", created.Id);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id.ToString() }, created);
            },
            HandleErrors
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(string id, [FromBody] User user)
    {
        logger.LogInformation("Updating user with ID: {UserId}", id);

        var result = await mediator.Send(new UpdateUserCommand(id, user));

        return result.Match(
            _ =>
            {
                logger.LogInformation("User {UserId} updated successfully", id);
                return NoContent();
            },
            HandleErrors
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogInformation("Attempting to delete user with ID: {UserId}", id);

        var result = await mediator.Send(new DeleteUserCommand(id));

        return result.Match(
            _ =>
            {
                logger.LogInformation("User {UserId} deleted", id);
                return NoContent();
            },
            HandleErrors
        );
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDto registrationDto)
    {
        logger.LogInformation("Registration attempt for nickname: {Nickname}", registrationDto.Nickname);

        var result = await mediator.Send(new RegisterUserCommand(registrationDto));

        return result.Match(
            tokenPair =>
            {
                logger.LogInformation("User {Nickname} registered successfully", registrationDto.Nickname);
                return Ok(tokenPair);
            },
            HandleErrors
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto loginDto)
    {
        logger.LogInformation("Login attempt for user: {Nickname}", loginDto.Nickname);

        var result = await mediator.Send(new LoginUserCommand(loginDto.Nickname, loginDto.Password));

        return result.Match(
            tokenPair =>
            {
                logger.LogInformation("User {Nickname} logged in successfully", loginDto.Nickname);
                return Ok(tokenPair);
            },
            HandleErrors
        );
    }
}
