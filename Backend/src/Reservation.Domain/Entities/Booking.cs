using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;
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
    {
        // FR-22 : longueur des demandes spéciales
        if (specialRequests is { Length: > 500 })
            throw new SpecialRequestsTooLongException(500, specialRequests.Length);

        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // FR-8 : date dans le passé
        if (bookingDate < today)
            throw new BookingDateInPastException(bookingDate);

        // FR-6 (WalkIn) : doit être aujourd'hui
        if (source == BookingSource.WalkIn && bookingDate != today)
            throw new WalkInMustBeTodayException(today, bookingDate);

        // FR-9 : client blacklisté
        customer.EnsureCanBook();

        // FR-1 : heure d'arrivée dans la plage du service
        if (arrivalTime < service.StartTime || arrivalTime > service.LastBookingTime)
            throw new TimeOutsideServiceException(arrivalTime, service.LastBookingTime);

        // FR-6 : horizon de réservation selon source × VipLevel
        if (source != BookingSource.WalkIn)
        {
            var maxDays = GetMaxHorizonDays(source, customer.VipLevel);
            var daysUntilBooking = bookingDate.DayNumber - today.DayNumber;
            if (daysUntilBooking > maxDays)
                throw new HorizonExceededException(maxDays, daysUntilBooking);
        }

        // FR-7 : délai minimum avant le service
        if (source != BookingSource.WalkIn)
        {
            var minMinutes = source == BookingSource.Online ? 120 : 15;
            var arrivalUtc = new DateTimeOffset(bookingDate.ToDateTime(arrivalTime), TimeSpan.Zero);
            var minutesLeft = (int)(arrivalUtc - clock.UtcNow).TotalMinutes;
            if (minutesLeft < minMinutes)
                throw new MinLeadTimeViolatedException(minMinutes, minutesLeft);
        }

        // FR-3 : capacité de la table
        if (table is not null)
        {
            if (guestsCount < table.MinCapacity)
                throw new GuestsBelowMinException(table.MinCapacity, guestsCount);
            if (guestsCount > table.Capacity)
                throw new GuestsExceedCapacityException(table.Capacity, guestsCount);
        }

        // FR-4 : plafond de couverts du service
        if (existingServiceCovers + guestsCount > service.MaxCovers)
            throw new ServiceFullyBookedException(service.MaxCovers, existingServiceCovers + guestsCount);

        // FR-5 : conflits de créneaux sur la table
        if (table is not null && existingTableBookings is not null)
        {
            var newStart = new DateTimeOffset(bookingDate.ToDateTime(arrivalTime), TimeSpan.Zero);
            var newSlot  = new TimeSlot(newStart, newStart.AddMinutes(service.DurationMinutes));

            foreach (var existing in existingTableBookings)
            {
                if (newSlot.Overlaps(existing.GetTimeSlot(service)))
                    throw new BookingConflictException(table.Id, existing.Id);
            }
        }

        // FR-23 : détection automatique des flags informatifs
        var hasAllergyAlert = specialRequests is not null &&
            (specialRequests.Contains("allergie",    StringComparison.InvariantCultureIgnoreCase) ||
             specialRequests.Contains("intolérance", StringComparison.InvariantCultureIgnoreCase));

        var isCelebration = specialRequests is not null &&
            (specialRequests.Contains("anniversaire", StringComparison.InvariantCultureIgnoreCase) ||
             specialRequests.Contains("mariage",      StringComparison.InvariantCultureIgnoreCase) ||
             specialRequests.Contains("fiançailles",  StringComparison.InvariantCultureIgnoreCase));

        var needsHighChair = specialRequests is not null &&
            (specialRequests.Contains("chaise bébé",  StringComparison.InvariantCultureIgnoreCase) ||
             specialRequests.Contains("siège enfant", StringComparison.InvariantCultureIgnoreCase));

        return new Booking(
            id: Guid.NewGuid(),
            customerId: customer.Id,
            tableId: table?.Id,
            serviceId: service.Id,
            bookingDate: bookingDate,
            arrivalTime: arrivalTime,
            guestsCount: guestsCount,
            status: BookingStatus.Pending,
            source: source,
            specialRequests: specialRequests,
            hasAllergyAlert: hasAllergyAlert,
            isCelebration: isCelebration,
            needsHighChair: needsHighChair,
            lateCancel: false,
            cancellationReason: null,
            createdAt: clock.UtcNow);
    }

    // FR-12 : transitions de statut autorisées (RB-014)
    public void TransitionTo(BookingStatus newStatus, UserRole actorRole, DateTimeOffset now)
    {
        var isAllowed = (Status, newStatus) switch
        {
            (BookingStatus.Pending,   BookingStatus.Confirmed) => true,
            (BookingStatus.Pending,   BookingStatus.Rejected)  => true,
            (BookingStatus.Confirmed, BookingStatus.Seated)    => true,
            (BookingStatus.Confirmed, BookingStatus.NoShow)    => true,
            (BookingStatus.Seated,    BookingStatus.Completed) => true,
            _                                                  => false,
        };

        if (!isAllowed)
            throw new InvalidStatusTransitionException(Status, newStatus);

        // RB-008 : no-show uniquement après ArrivalTime + 15 min
        if (newStatus == BookingStatus.NoShow)
        {
            var arrivalUtc  = new DateTimeOffset(BookingDate.ToDateTime(ArrivalTime), TimeSpan.Zero);
            var noShowAfter = arrivalUtc.AddMinutes(15);
            if (now < noShowAfter)
                throw new NoShowTooEarlyException(noShowAfter);
        }

        Status = newStatus;
    }

    // FR-11 : annulation avec gestion du LateCancel (RB-009)
    public void Cancel(string reason, DateTimeOffset now, UserRole actorRole)
    {
        if (Status == BookingStatus.Seated)
            throw new CannotCancelSeatedException(Status);

        if (Status is not BookingStatus.Pending and not BookingStatus.Confirmed)
            throw new InvalidStatusTransitionException(Status, BookingStatus.Cancelled);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("La raison d'annulation est obligatoire.", nameof(reason));

        var arrivalUtc = new DateTimeOffset(BookingDate.ToDateTime(ArrivalTime), TimeSpan.Zero);
        if (now > arrivalUtc.AddHours(-24))
            LateCancel = true;

        CancellationReason = reason;
        Status = BookingStatus.Cancelled;
    }

    public TimeSlot GetTimeSlot(DiningService service)
    {
        var start = new DateTimeOffset(BookingDate.ToDateTime(ArrivalTime), TimeSpan.Zero);
        return new TimeSlot(start, start.AddMinutes(service.DurationMinutes));
    }

    private static int GetMaxHorizonDays(BookingSource source, VipLevel vipLevel)
    {
        if (vipLevel is VipLevel.VIP or VipLevel.VVIP) return 180;
        return source == BookingSource.Online ? 30 : 90;
    }
}
