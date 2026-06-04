namespace Reservation.Domain.Exceptions;

public sealed class RestaurantClosedException(DateOnly closedDate, string reason)
    : DomainException($"Le restaurant est fermé le {closedDate} ({reason}).")
{
    public DateOnly ClosedDate { get; } = closedDate;
    public string Reason { get; } = reason;
}
