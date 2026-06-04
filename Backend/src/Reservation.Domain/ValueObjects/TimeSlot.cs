namespace Reservation.Domain.ValueObjects;

public sealed class TimeSlot
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    internal TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("La fin du créneau doit être après le début.");
        Start = start;
        End = end;
    }

    public bool Overlaps(TimeSlot other) => throw new NotImplementedException();

    public override string ToString() => $"[{Start:HH:mm} – {End:HH:mm}]";
}
