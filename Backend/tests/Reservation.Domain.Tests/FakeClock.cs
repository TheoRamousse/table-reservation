using Reservation.Domain.Interfaces;

namespace Reservation.Domain.Tests;

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
