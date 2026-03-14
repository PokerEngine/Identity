using Application.Command;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Controller;

[ApiController]
[Route("api/identity")]
[Produces("application/json")]
public class IdentityController(
    ICommandDispatcher commandDispatcher
) : ControllerBase
{
    [HttpPost("password-reset/request")]
    [ProducesResponseType(typeof(RequestPasswordResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        var response = await commandDispatcher.DispatchAsync<RequestPasswordResetCommand, RequestPasswordResetResponse>(command);
        return Ok(response);
    }

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType(typeof(ConfirmPasswordResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetCommand command)
    {
        var response = await commandDispatcher.DispatchAsync<ConfirmPasswordResetCommand, ConfirmPasswordResetResponse>(command);
        return Ok(response);
    }
}
