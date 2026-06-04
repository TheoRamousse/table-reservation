using Reservation.Domain.Interfaces;

namespace Reservation.Application.Tests;

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow => utcNow;
}
