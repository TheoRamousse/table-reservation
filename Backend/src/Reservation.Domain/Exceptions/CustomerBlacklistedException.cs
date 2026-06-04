namespace Reservation.Domain.Exceptions;

public sealed class CustomerBlacklistedException(Guid customerId)
    : DomainException($"Le client {customerId} est blacklisté et ne peut pas réserver.")
{
    public Guid CustomerId { get; } = customerId;
}
