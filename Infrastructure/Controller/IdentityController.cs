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
    [HttpPost("{accountUid:guid}/initialize-password")]
    [ProducesResponseType(typeof(InitializePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InitializePassword(Guid accountUid, [FromBody] InitializePasswordRequest request)
    {
        var command = new InitializePasswordCommand
        {
            AccountUid = accountUid,
            Password = request.Password
        };
        var response = await commandDispatcher.DispatchAsync<InitializePasswordCommand, InitializePasswordResponse>(command);
        return Ok(response);
    }

    [HttpPost("{accountUid:guid}/change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(Guid accountUid, [FromBody] ChangePasswordRequest request)
    {
        var command = new ChangePasswordCommand
        {
            AccountUid = accountUid,
            OldPassword = request.OldPassword,
            NewPassword = request.NewPassword
        };
        var response = await commandDispatcher.DispatchAsync<ChangePasswordCommand, ChangePasswordResponse>(command);
        return Ok(response);
    }
}

public record InitializePasswordRequest
{
    public required string Password { get; init; }
}

public record ChangePasswordRequest
{
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}
