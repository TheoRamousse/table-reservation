namespace Reservation.Domain.Exceptions;

public sealed class SpecialRequestsTooLongException(int maxLength, int provided)
    : DomainException($"Les demandes spéciales dépassent {maxLength} caractères ({provided} fournis).")
{
    public int MaxLength { get; } = maxLength;
    public int Provided { get; } = provided;
}
