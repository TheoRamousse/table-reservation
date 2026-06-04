namespace Reservation.Application.Interfaces;

public interface IUserRepository
{
    Task<(bool success, string role)> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
}
