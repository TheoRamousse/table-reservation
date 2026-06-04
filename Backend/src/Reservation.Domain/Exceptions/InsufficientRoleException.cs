using Reservation.Domain.Enums;

namespace Reservation.Domain.Exceptions;

public sealed class InsufficientRoleException(UserRole required, UserRole actual)
    : DomainException($"Rôle insuffisant : {required} requis, {actual} fourni.")
{
    public UserRole Required { get; } = required;
    public UserRole Actual { get; } = actual;
}
