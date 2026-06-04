using Reservation.Domain.Enums;

namespace Reservation.Application.DTOs;

public enum TableStatus { Free, Pending, Confirmed, Seated, NoShow, Inactive }

public record ActiveBookingInfo(
    Guid BookingId,
    Guid CustomerId,
    string CustomerName,
    int GuestsCount,
    TimeOnly ArrivalTime,
    bool HasAllergyAlert,
    bool IsCelebration,
    bool NeedsHighChair);

public record TableStateDto(
    Guid Id,
    int Number,
    int Capacity,
    int MinCapacity,
    TableZone Zone,
    bool IsActive,
    bool IsCombinable,
    TableStatus Status,
    ActiveBookingInfo? ActiveBooking);

public record FloorSnapshotDto(
    DateOnly Date,
    Guid ServiceId,
    string ServiceName,
    int TotalConfirmedCovers,
    int MaxCovers,
    IReadOnlyList<TableStateDto> Tables);
