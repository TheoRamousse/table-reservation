namespace Reservation.Domain.Exceptions;

public sealed class WalkInMustBeTodayException(DateOnly today, DateOnly provided)
    : DomainException($"Un walk-in doit être pour aujourd'hui ({today}), pas pour le {provided}.")
{
    public DateOnly Today { get; } = today;
    public DateOnly Provided { get; } = provided;
}
