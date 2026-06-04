namespace Reservation.Domain.Exceptions;

public sealed class BookingDateInPastException(DateOnly provided)
    : DomainException($"La date de réservation ({provided}) est dans le passé.")
{
    public DateOnly Provided { get; } = provided;
}
