namespace Reservation.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string email, string role);
    DateTimeOffset GetExpiry();
}
