namespace Reservation.Api.Requests;

public record DeclareClosedDayRequest(DateOnly Date, string Reason);
