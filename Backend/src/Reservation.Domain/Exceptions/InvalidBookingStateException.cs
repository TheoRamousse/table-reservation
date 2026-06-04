using Reservation.Domain.Enums;

namespace Reservation.Domain.Exceptions;

public sealed class InvalidBookingStateException(BookingStatus current, string operation)
    : DomainException($"L'opération '{operation}' n'est pas autorisée pour une réservation en statut {current}.")
{
    public BookingStatus Current { get; } = current;
    public string Operation { get; } = operation;
}
