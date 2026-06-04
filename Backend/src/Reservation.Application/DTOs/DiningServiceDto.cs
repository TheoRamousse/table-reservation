namespace Reservation.Application.DTOs;

public record DiningServiceDto(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeOnly LastBookingTime,
    int DurationMinutes,
    int MaxCovers,
    bool IsActive);
