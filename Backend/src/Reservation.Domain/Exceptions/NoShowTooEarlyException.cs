namespace Reservation.Domain.Exceptions;

public sealed class NoShowTooEarlyException(DateTimeOffset earliestNoShowAt)
    : DomainException($"Le no-show ne peut pas être enregistré avant {earliestNoShowAt} (ArrivalTime + 15 min).")
{
    public DateTimeOffset EarliestNoShowAt { get; } = earliestNoShowAt;
}
