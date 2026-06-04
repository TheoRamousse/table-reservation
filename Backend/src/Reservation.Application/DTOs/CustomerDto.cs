using Reservation.Domain.Enums;

namespace Reservation.Application.DTOs;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    bool IsBlacklisted,
    int NoShowCount,
    int LateCancelCount,
    VipLevel VipLevel);
