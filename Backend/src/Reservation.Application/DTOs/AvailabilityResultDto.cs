using Reservation.Domain.Enums;

namespace Reservation.Application.DTOs;

public record AvailableTableDto(
    Guid Id,
    int Number,
    int Capacity,
    int MinCapacity,
    TableZone Zone,
    bool IsCombinable);

public record AvailabilityResultDto(
    IReadOnlyList<AvailableTableDto> Tables,
    string? Reason);
