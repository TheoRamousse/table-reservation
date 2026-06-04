using MediatR;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult?>;

public record LoginResult(string Token, DateTimeOffset ExpiresAt, string Role);

public sealed class LoginCommandHandler(
    IUserRepository userRepo,
    IJwtTokenService jwtService) : IRequestHandler<LoginCommand, LoginResult?>
{
    public async Task<LoginResult?> Handle(LoginCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
