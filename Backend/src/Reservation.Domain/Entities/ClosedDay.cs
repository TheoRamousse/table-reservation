using Reservation.Domain.Interfaces;

namespace Reservation.Domain.Entities;

public sealed class ClosedDay
{
    public Guid Id { get; private set; }
    public DateOnly ClosedDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private ClosedDay() { }

    internal ClosedDay(Guid id, DateOnly closedDate, string reason, DateTimeOffset createdAt)
    {
        Id = id;
        ClosedDate = closedDate;
        Reason = reason;
        CreatedAt = createdAt;
    }

    public static ClosedDay Create(DateOnly date, string reason, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ClosedDay(Guid.NewGuid(), date, reason, clock.UtcNow);
    }
}
