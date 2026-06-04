namespace Reservation.Domain.Exceptions;

public sealed class HorizonExceededException(int maxDays, int provided)
    : DomainException($"La réservation dépasse l'horizon autorisé de {maxDays} jours ({provided} jours demandés).")
{
    public int MaxDays { get; } = maxDays;
    public int Provided { get; } = provided;
}
