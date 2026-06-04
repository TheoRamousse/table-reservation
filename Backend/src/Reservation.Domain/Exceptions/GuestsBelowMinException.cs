namespace Reservation.Domain.Exceptions;

public sealed class GuestsBelowMinException(int minCapacity, int provided)
    : DomainException($"Le nombre de couverts ({provided}) est inférieur à la capacité minimale ({minCapacity}).")
{
    public int MinCapacity { get; } = minCapacity;
    public int Provided { get; } = provided;
}
