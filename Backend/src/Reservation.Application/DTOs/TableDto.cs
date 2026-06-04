using Reservation.Domain.Enums;

namespace Reservation.Application.DTOs;

public record TableDto(
    Guid Id,
    int Number,
    int Capacity,
    int MinCapacity,
    TableZone Zone,
    bool IsActive,
    bool IsCombinable);
