namespace Reservation.Api.Requests;

public record CreateDiningServiceRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeOnly LastBookingTime,
    int DurationMinutes,
    int MaxCovers);
