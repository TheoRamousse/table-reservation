using Reservation.Domain.Enums;

namespace Reservation.Domain.Exceptions;

public sealed class CannotCancelSeatedException(BookingStatus currentStatus)
    : DomainException($"Impossible d'annuler une réservation avec le statut {currentStatus}.")
{
    public BookingStatus CurrentStatus { get; } = currentStatus;
}
