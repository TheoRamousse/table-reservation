using Reservation.Domain.Enums;

namespace Reservation.Api.Requests;

public record CreateTableRequest(int Number, int Capacity, int MinCapacity, TableZone Zone, bool IsCombinable);
public record UpdateTableRequest(int Capacity, int MinCapacity, TableZone Zone, bool IsCombinable, bool IsActive);
