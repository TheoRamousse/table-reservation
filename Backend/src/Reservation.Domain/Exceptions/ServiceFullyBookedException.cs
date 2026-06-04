namespace Reservation.Domain.Exceptions;

public sealed class ServiceFullyBookedException(int maxCovers, int currentTotal)
    : DomainException($"Le service est complet ({currentTotal}/{maxCovers} couverts).")
{
    public int MaxCovers { get; } = maxCovers;
    public int CurrentTotal { get; } = currentTotal;
}
