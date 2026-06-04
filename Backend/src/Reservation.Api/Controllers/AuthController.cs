using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.Auth.Commands;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        if (result is null)
            return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Email ou mot de passe incorrect." });

        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt, role = result.Role });
    }
}
