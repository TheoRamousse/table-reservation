namespace Reservation.Domain.Exceptions;

public sealed class TableNotCombinableException(Guid tableId)
    : DomainException($"La table {tableId} n'est pas combinable (IsCombinable = false).")
{
    public Guid TableId { get; } = tableId;
}
