using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests de création d'une réservation — FR-1 à FR-9 (RB-001 à RB-007).</summary>
[Trait("Category", "Unit")]
public class BookingCreationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    // ── FR-1 : Plage horaire du service (RB-001) ───────────────────────────

    [Fact]
    public void Should_CreateBooking_When_ArrivalTimeEqualsLastBookingTime()
    {
        // Arrange
        var service = Builders.DinnerService(); // LastBookingTime = 21:30
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(21, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow("ArrivalTime = LastBookingTime est la borne inclusive autorisée (RB-001)");
    }

    [Fact]
    public void Should_Throw_TimeOutsideServiceException_When_ArrivalTimeAfterLastBookingTime()
    {
        // Arrange
        var service = Builders.DinnerService(); // LastBookingTime = 21:30
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(21, 31),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<TimeOutsideServiceException>("ArrivalTime + 1 min dépasse LastBookingTime (RB-001)");
    }

    [Fact]
    public void Should_Throw_TimeOutsideServiceException_When_ArrivalTimeBeforeServiceStart()
    {
        // Arrange
        var service = Builders.DinnerService(); // StartTime = 19:00
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(18, 59),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<TimeOutsideServiceException>("ArrivalTime avant StartTime doit être rejeté (RB-001)");
    }

    // ── FR-3 : Capacité de la table (RB-002) ──────────────────────────────

    [Fact]
    public void Should_Throw_GuestsBelowMinException_When_GuestsCountBelowMinCapacity()
    {
        // Arrange
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var service = Builders.DinnerService();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 1,
            source: BookingSource.Online, specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<GuestsBelowMinException>("GuestsCount < MinCapacity doit être rejeté (RB-002)");
    }

    [Fact]
    public void Should_Throw_GuestsExceedCapacityException_When_GuestsCountExceedsCapacity()
    {
        // Arrange
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 5,
            source: BookingSource.Online, specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<GuestsExceedCapacityException>("GuestsCount > Capacity doit être rejeté (RB-002)");
    }

    [Theory]
    [InlineData(2)] // MinCapacity
    [InlineData(4)] // Capacity
    [InlineData(3)] // Entre les deux
    public void Should_CreateBooking_When_GuestsCountIsWithinRange(int guestsCount)
    {
        // Arrange
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: guestsCount,
            source: BookingSource.Online, specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow();
    }

    // ── FR-4 : Plafond de couverts du service (RB-003) ────────────────────

    [Fact]
    public void Should_Throw_ServiceFullyBookedException_When_MaxCoversWouldBeExceeded()
    {
        // Arrange
        var service = Builders.DinnerService(maxCovers: 10);
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act — 8 couverts existants + 3 nouveau = 11 > MaxCovers=10
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 3,
            source: BookingSource.Online, specialRequests: null, clock: _clock,
            existingServiceCovers: 8);

        // Assert
        act.Should().Throw<ServiceFullyBookedException>("dépasser MaxCovers doit être rejeté (RB-003)")
           .Which.CurrentTotal.Should().Be(11); // 8 existants + 3 nouveaux
    }

    [Fact]
    public void Should_CreateBooking_When_TotalCoversEqualsMaxCovers()
    {
        // Arrange
        var service = Builders.DinnerService(maxCovers: 10);
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act — 8 existants + 2 nouveau = 10 = MaxCovers → accepté
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2,
            source: BookingSource.Online, specialRequests: null, clock: _clock,
            existingServiceCovers: 8);

        // Assert
        act.Should().NotThrow();
    }

    // ── FR-5 : Conflits de table (RB-004) ─────────────────────────────────

    [Fact]
    public void Should_Throw_BookingConflictException_When_TimeSlotOverlaps()
    {
        // Arrange — réservation existante de 19h à 21h (120 min)
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService(durationMinutes: 120);
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        var existingBooking = Builders.PendingBooking(
            table: table, service: service,
            bookingDate: date,
            arrivalTime: new TimeOnly(19, 0),
            guestsCount: 2);

        // Act — nouvelle réservation à 19h30 chevauche la 1re (qui finit à 21h)
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock,
            existingTableBookings: [existingBooking]);

        // Assert
        act.Should().Throw<BookingConflictException>("chevauchement d'une seule minute doit être rejeté (RB-004)");
    }

    [Fact]
    public void Should_CreateBooking_When_SlotsAreBackToBack()
    {
        // Arrange — réservation existante de 19h à 21h (120 min)
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService(durationMinutes: 120);
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        var existingBooking = Builders.PendingBooking(
            table: table, service: service,
            bookingDate: date,
            arrivalTime: new TimeOnly(19, 0),
            guestsCount: 2);

        // Act — nouvelle réservation commence exactement à la fin de la précédente (21h)
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(21, 0),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock,
            existingTableBookings: [existingBooking]);

        // Assert
        act.Should().NotThrow("les créneaux dos à dos sont autorisés (A.End == B.Start)");
    }

    // ── FR-6 : Horizon de réservation (RB-005) ────────────────────────────

    [Fact]
    public void Should_Throw_BookingDateInPastException_When_DateIsYesterday()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var yesterday = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(-1));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, yesterday,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<BookingDateInPastException>("une date passée doit être rejetée (RB-005)");
    }

    [Fact]
    public void Should_Throw_HorizonExceededException_When_OnlineStandardBookingAt31Days()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer(vipLevel: VipLevel.None);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(31));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<HorizonExceededException>("Online standard limité à 30 jours (RB-005)");
    }

    [Fact]
    public void Should_CreateBooking_When_OnlineVipBookingAt31Days()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer(vipLevel: VipLevel.VIP);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(31));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow("les clients VIP Online ont un horizon de 180 jours (RB-005)");
    }

    [Fact]
    public void Should_Throw_WalkInMustBeTodayException_When_WalkInForTomorrow()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var tomorrow = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(1));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, tomorrow,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.WalkIn,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<WalkInMustBeTodayException>("un walk-in doit obligatoirement être pour aujourd'hui (RB-005)");
    }

    // ── FR-7 : Délai minimum avant le service (RB-006) ────────────────────

    [Fact]
    public void Should_Throw_MinLeadTimeViolatedException_When_OnlineBookingLessThan2HoursBefore()
    {
        // Arrange — horloge simulée à 10h00, arrivalTime à 11h45 (1h45 d'avance)
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));
        var service = Builders.DinnerService(
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(14, 0),
            lastBookingTime: new TimeOnly(13, 30));
        var table = Builders.Table();
        var customer = Builders.Customer();
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, today,
            arrivalTime: new TimeOnly(11, 45),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: clock);

        // Assert
        act.Should().Throw<MinLeadTimeViolatedException>("Online requiert 2h de délai minimum avant l'arrivée (RB-006)");
    }

    [Fact]
    public void Should_Throw_MinLeadTimeViolatedException_When_PhoneBookingLessThan15MinBefore()
    {
        // Arrange — horloge simulée à 10h00, arrivalTime à 10h10 (10 min d'avance)
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));
        var service = Builders.DinnerService(
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(14, 0),
            lastBookingTime: new TimeOnly(13, 30));
        var table = Builders.Table();
        var customer = Builders.Customer();
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, today,
            arrivalTime: new TimeOnly(10, 10),
            guestsCount: 2, source: BookingSource.Phone,
            specialRequests: null, clock: clock);

        // Assert
        act.Should().Throw<MinLeadTimeViolatedException>("Phone requiert 15 min de délai minimum (RB-006)");
    }

    [Fact]
    public void Should_CreateBooking_When_WalkInIgnoresLeadTimeRequirement()
    {
        // Arrange — horloge simulée à 10h00, arrivalTime à 10h05
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));
        var service = Builders.DinnerService(
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(14, 0),
            lastBookingTime: new TimeOnly(13, 30));
        var table = Builders.Table();
        var customer = Builders.Customer();
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, today,
            arrivalTime: new TimeOnly(10, 5),
            guestsCount: 2, source: BookingSource.WalkIn,
            specialRequests: null, clock: clock);

        // Assert
        act.Should().NotThrow("les walk-ins ignorent le délai minimum (RB-006)");
    }

    // ── FR-9 : Client blacklisté (RB-007) ─────────────────────────────────

    [Fact]
    public void Should_Throw_CustomerBlacklistedException_When_CustomerIsBlacklisted()
    {
        // Arrange
        var customer = Builders.Customer(isBlacklisted: true);
        var table = Builders.Table();
        var service = Builders.DinnerService();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().Throw<CustomerBlacklistedException>("un client blacklisté ne peut pas réserver (RB-007)");
    }

    // ── FR-1 : borne inférieure StartTime (RB-001) ────────────────────────

    [Fact]
    public void Should_CreateBooking_When_ArrivalTimeEqualsStartTime()
    {
        // Arrange
        var service = Builders.DinnerService(); // StartTime = 19:00
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 0), // == StartTime
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow("ArrivalTime == StartTime est la borne inclusive autorisée (RB-001)");
    }

    // ── FR-6 : bornes d'horizon et délai minimum (RB-005, RB-006) ─────────

    [Fact]
    public void Should_CreateBooking_When_OnlineBookingAtExactHorizon()
    {
        // Arrange — Online standard : horizon = 30 jours
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer(vipLevel: VipLevel.None);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(30)); // juste à la limite

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow("daysUntilBooking == maxDays est la borne inclusive autorisée (RB-005)");
    }

    [Fact]
    public void Should_CreateBooking_When_PhoneBookingAt31Days()
    {
        // Arrange — Phone standard : horizon = 90 jours
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer(vipLevel: VipLevel.None);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(31));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Phone,
            specialRequests: null, clock: _clock);

        // Assert
        act.Should().NotThrow("Phone standard a un horizon de 90 jours, 31 jours est autorisé (RB-005)");
    }

    [Fact]
    public void Should_CreateBooking_When_PhoneBookingWith16MinLeadTime()
    {
        // Arrange — horloge à 10h00, arrivalTime à 10h16 (16 min d'avance, Phone requiert 15)
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));
        var service = Builders.DinnerService(
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(14, 0),
            lastBookingTime: new TimeOnly(13, 30));
        var table = Builders.Table();
        var customer = Builders.Customer();
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, today,
            arrivalTime: new TimeOnly(10, 16),
            guestsCount: 2, source: BookingSource.Phone,
            specialRequests: null, clock: clock);

        // Assert
        act.Should().NotThrow("Phone requiert 15 min de délai, 16 min est suffisant (RB-006)");
    }

    [Fact]
    public void Should_CreateBooking_When_OnlineBookingExactly120MinBefore()
    {
        // Arrange — horloge à 10h00, arrivalTime à 12h00 (exactement 120 min = borne inclusive)
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));
        var service = Builders.DinnerService(
            startTime: new TimeOnly(12, 0),
            endTime: new TimeOnly(16, 0),
            lastBookingTime: new TimeOnly(15, 30));
        var table = Builders.Table();
        var customer = Builders.Customer();
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, today,
            arrivalTime: new TimeOnly(12, 0),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: clock);

        // Assert
        act.Should().NotThrow("minutesLeft == minMinutes est la borne inclusive autorisée pour Online (RB-006)");
    }

    // ── LateCancel initial à false (RB-009) ───────────────────────────────

    [Fact]
    public void Should_SetLateCancelToFalse_When_BookingCreated()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        booking.LateCancel.Should().BeFalse("une nouvelle réservation n'est jamais un LateCancel");
    }
}
