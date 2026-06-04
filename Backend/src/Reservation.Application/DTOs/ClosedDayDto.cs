namespace Reservation.Application.DTOs;

public record ClosedDayDto(
    Guid Id,
    DateOnly ClosedDate,
    string Reason,
    DateTimeOffset CreatedAt);
