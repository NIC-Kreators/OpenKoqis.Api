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
public class UsersController(ISender mediator, ILogger<UsersController> logger) : ControllerBase
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
            errors => Problem(errors)
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        logger.LogInformation("Fetching user with ID: {UserId}", id);

        var result = await mediator.Send(new GetUserByIdQuery(id));

        return result.Match(
            user => Ok(user),
            errors => Problem(errors)
        );
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
            errors => Problem(errors)
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
            errors => Problem(errors)
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
            errors => Problem(errors)
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
            errors => Problem(errors)
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
            errors => Problem(errors)
        );
    }


    private IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Problem();
        }

        var firstError = errors.First();

        var statusCode = firstError.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: firstError.Description);
    }
}
