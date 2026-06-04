namespace Reservation.Domain.ValueObjects;

public sealed class TimeSlot : IEquatable<TimeSlot>
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

    // Vrai si les deux créneaux se chevauchent (exclusive sur les bornes : A.End == B.Start → false)
    public bool Overlaps(TimeSlot other) =>
        Start < other.End && other.Start < End;

    public bool Equals(TimeSlot? other) =>
        other is not null && Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => Equals(obj as TimeSlot);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"[{Start:HH:mm} – {End:HH:mm}]";
}
