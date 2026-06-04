namespace Reservation.Domain.Exceptions;

public sealed class TimeOutsideServiceException(TimeOnly arrivalTime, TimeOnly lastBookingTime)
    : DomainException($"L'heure d'arrivée {arrivalTime} dépasse la dernière heure de réservation {lastBookingTime}.")
{
    public TimeOnly ArrivalTime { get; } = arrivalTime;
    public TimeOnly LastBookingTime { get; } = lastBookingTime;
}
