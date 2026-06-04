namespace Reservation.Application.Exceptions;

public sealed class EntityNotFoundException(string entityName, Guid id)
    : Exception($"{entityName} '{id}' introuvable.");
