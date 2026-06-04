using Reservation.Domain.Enums;
using Reservation.Domain.Interfaces;
using Reservation.Domain.ValueObjects;

namespace Reservation.Domain.Entities;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? TableId { get; private set; }
    public Guid ServiceId { get; private set; }
    public DateOnly BookingDate { get; private set; }
    public TimeOnly ArrivalTime { get; private set; }
    public int GuestsCount { get; private set; }
    public BookingStatus Status { get; private set; }
    public BookingSource Source { get; private set; }
    public string? SpecialRequests { get; private set; }
    public bool HasAllergyAlert { get; private set; }
    public bool IsCelebration { get; private set; }
    public bool NeedsHighChair { get; private set; }
    public bool LateCancel { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Booking() { }

    internal Booking(Guid id, Guid customerId, Guid? tableId, Guid serviceId,
        DateOnly bookingDate, TimeOnly arrivalTime, int guestsCount,
        BookingStatus status, BookingSource source, string? specialRequests,
        bool hasAllergyAlert, bool isCelebration, bool needsHighChair,
        bool lateCancel, string? cancellationReason, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        TableId = tableId;
        ServiceId = serviceId;
        BookingDate = bookingDate;
        ArrivalTime = arrivalTime;
        GuestsCount = guestsCount;
        Status = status;
        Source = source;
        SpecialRequests = specialRequests;
        HasAllergyAlert = hasAllergyAlert;
        IsCelebration = isCelebration;
        NeedsHighChair = needsHighChair;
        LateCancel = lateCancel;
        CancellationReason = cancellationReason;
        CreatedAt = createdAt;
    }

    public static Booking Create(
        Customer customer,
        Table? table,
        DiningService service,
        DateOnly bookingDate,
        TimeOnly arrivalTime,
        int guestsCount,
        BookingSource source,
        string? specialRequests,
        IClock clock,
        int existingServiceCovers = 0,
        IEnumerable<Booking>? existingTableBookings = null)
        => throw new NotImplementedException();

    public void TransitionTo(BookingStatus newStatus, UserRole actorRole, DateTimeOffset now)
        => throw new NotImplementedException();

    public void Cancel(string reason, DateTimeOffset now, UserRole actorRole)
        => throw new NotImplementedException();

    public TimeSlot GetTimeSlot(DiningService service)
        => throw new NotImplementedException();
}
