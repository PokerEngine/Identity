using Application.Command;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controller;

[ApiController]
[Route("api/session")]
[Produces("application/json")]
public class SessionController(
    ICommandDispatcher commandDispatcher
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand command)
    {
        var response = await commandDispatcher.DispatchAsync<CreateSessionCommand, CreateSessionResponse>(command);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshSession([FromBody] RefreshSessionCommand command)
    {
        var response = await commandDispatcher.DispatchAsync<RefreshSessionCommand, RefreshSessionResponse>(command);
        return Ok(response);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(RevokeSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeSession()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer "))
            return Unauthorized();

        var accessToken = authHeader["Bearer ".Length..];
        var command = new RevokeSessionCommand { AccessToken = accessToken };
        var response = await commandDispatcher.DispatchAsync<RevokeSessionCommand, RevokeSessionResponse>(command);
        return Ok(response);
    }
}
