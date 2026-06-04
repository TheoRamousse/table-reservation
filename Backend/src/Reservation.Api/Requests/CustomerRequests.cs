namespace Reservation.Api.Requests;

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Phone,
    string? Email);

public record UpdateBlacklistRequest(bool IsBlacklisted);
