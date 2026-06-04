using Reservation.Domain.Enums;

namespace Reservation.Domain.Exceptions;

public sealed class InvalidStatusTransitionException(BookingStatus from, BookingStatus to)
    : DomainException($"La transition de statut {from} → {to} n'est pas autorisée.")
{
    public BookingStatus From { get; } = from;
    public BookingStatus To { get; } = to;
}
