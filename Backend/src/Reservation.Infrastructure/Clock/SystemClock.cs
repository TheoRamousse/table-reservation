using Reservation.Domain.Interfaces;

namespace Reservation.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
