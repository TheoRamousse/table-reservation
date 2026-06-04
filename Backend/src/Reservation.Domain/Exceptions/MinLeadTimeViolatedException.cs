namespace Reservation.Domain.Exceptions;

public sealed class MinLeadTimeViolatedException(int minMinutes, int minutesLeft)
    : DomainException($"La réservation est trop proche du service (minimum {minMinutes} min, il reste {minutesLeft} min).")
{
    public int MinMinutes { get; } = minMinutes;
    public int MinutesLeft { get; } = minutesLeft;
}
