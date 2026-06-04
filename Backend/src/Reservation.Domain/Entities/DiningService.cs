namespace Reservation.Domain.Entities;

public sealed class DiningService
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public TimeOnly LastBookingTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxCovers { get; private set; }
    public bool IsActive { get; private set; }

    private DiningService() { }

    internal DiningService(Guid id, string name, TimeOnly startTime, TimeOnly endTime,
        TimeOnly lastBookingTime, int durationMinutes, int maxCovers, bool isActive = true)
    {
        Id = id;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        LastBookingTime = lastBookingTime;
        DurationMinutes = durationMinutes;
        MaxCovers = maxCovers;
        IsActive = isActive;
    }

    public static DiningService Create(string name, TimeOnly startTime, TimeOnly endTime,
        TimeOnly lastBookingTime, int durationMinutes, int maxCovers)
    {
        if (endTime <= startTime)
            throw new ArgumentException("L'heure de fin doit être après l'heure de début.");
        if (lastBookingTime < startTime)
            throw new ArgumentException("L'heure limite de réservation ne peut pas être avant l'heure de début.");
        if (lastBookingTime > endTime)
            throw new ArgumentException("L'heure limite de réservation ne peut pas dépasser l'heure de fin.");
        if (durationMinutes <= 0)
            throw new ArgumentException("La durée doit être supérieure à zéro.", nameof(durationMinutes));
        if (maxCovers <= 0)
            throw new ArgumentException("Le nombre maximum de couverts doit être supérieur à zéro.", nameof(maxCovers));

        return new DiningService(Guid.NewGuid(), name, startTime, endTime, lastBookingTime, durationMinutes, maxCovers);
    }
}
