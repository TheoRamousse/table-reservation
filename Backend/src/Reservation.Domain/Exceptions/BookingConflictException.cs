namespace Reservation.Domain.Exceptions;

public sealed class BookingConflictException(Guid tableId, Guid conflictingBookingId)
    : DomainException($"La table {tableId} est déjà réservée sur ce créneau (conflit avec la réservation {conflictingBookingId}).")
{
    public Guid TableId { get; } = tableId;
    public Guid ConflictingBookingId { get; } = conflictingBookingId;
}
