namespace Reservation.Domain.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
