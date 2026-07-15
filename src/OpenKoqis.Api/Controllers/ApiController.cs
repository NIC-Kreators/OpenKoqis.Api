// Controllers/ApiController.cs
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace OpenKoqis.Api.Controllers;

public abstract class ApiController : ControllerBase
{
    protected IActionResult HandleErrors(List<Error> errors)
    {
        if (errors.Count == 0)
            return Problem();

        var firstError = errors[0];

        var statusCode = firstError.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, detail: firstError.Description);
    }
}
