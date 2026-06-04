using Reservation.Domain.Enums;

namespace Reservation.Api.Requests;

public record CreateBookingRequest(
    Guid CustomerId,
    Guid? TableId,
    Guid? SecondaryTableId,   // réservé pour la fusion de tables (Epic 5)
    Guid ServiceId,
    DateOnly BookingDate,
    TimeOnly ArrivalTime,
    int GuestsCount,
    BookingSource Source,
    string? SpecialRequests);

public record ChangeBookingStatusRequest(BookingStatus NewStatus);

public record CancelBookingRequest(string CancellationReason);

public record ModifyBookingRequest(
    DateOnly BookingDate,
    TimeOnly ArrivalTime,
    int GuestsCount,
    string? SpecialRequests,
    Guid? TableId);
