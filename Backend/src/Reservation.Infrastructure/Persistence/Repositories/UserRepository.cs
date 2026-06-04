using Reservation.Application.Interfaces;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private static readonly Dictionary<string, (string password, string role)> Users = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@restaurant.fr"]   = ("Admin123!",   "Admin"),
        ["manager@restaurant.fr"] = ("Manager123!", "Manager"),
        ["staff@restaurant.fr"]   = ("Staff123!",   "Staff"),
        ["online@restaurant.fr"]  = ("Online123!",  "Online"),
    };

    public Task<(bool success, string role)> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        if (Users.TryGetValue(email, out var entry) && entry.password == password)
            return Task.FromResult((true, entry.role));
        return Task.FromResult((false, string.Empty));
    }
}
