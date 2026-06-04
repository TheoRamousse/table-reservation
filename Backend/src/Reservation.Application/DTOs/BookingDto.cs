using Reservation.Domain.Enums;

namespace Reservation.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid CustomerId,
    Guid? TableId,
    Guid ServiceId,
    DateOnly BookingDate,
    TimeOnly ArrivalTime,
    int GuestsCount,
    BookingStatus Status,
    BookingSource Source,
    string? SpecialRequests,
    bool HasAllergyAlert,
    bool IsCelebration,
    bool NeedsHighChair,
    bool LateCancel,
    string? CancellationReason,
    DateTimeOffset CreatedAt);
