namespace Reservation.Domain.Exceptions;

public sealed class GuestsExceedCapacityException(int capacity, int provided)
    : DomainException($"Le nombre de couverts ({provided}) dépasse la capacité de la table ({capacity}).")
{
    public int Capacity { get; } = capacity;
    public int Provided { get; } = provided;
}
