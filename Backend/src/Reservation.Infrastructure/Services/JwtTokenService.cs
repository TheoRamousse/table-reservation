using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Reservation.Application.Interfaces;
using Reservation.Domain.Interfaces;

namespace Reservation.Infrastructure.Services;

public sealed class JwtTokenService(IConfiguration configuration, IClock clock) : IJwtTokenService
{
    public string GenerateToken(string email, string role)
    {
        var secret = configuration["Jwt:Secret"] ?? "dev-secret-key-minimum-32-characters-long!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, email),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "reservation-api",
            audience: configuration["Jwt:Audience"] ?? "reservation-client",
            claims: claims,
            expires: GetExpiry().UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTimeOffset GetExpiry() => clock.UtcNow.AddHours(8);
}
