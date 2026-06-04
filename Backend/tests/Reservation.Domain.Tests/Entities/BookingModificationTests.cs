using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests de modification d'une réservation — FR-13.</summary>
[Trait("Category", "Unit")]
public class BookingModificationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Should_ThrowBookingDateInPastException_When_NewDateIsInPast()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var pastDate = new DateOnly(2026, 6, 3); // hier par rapport à l'horloge

        // Act
        Action act = () => booking.Modify(table, service, pastDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        act.Should().Throw<BookingDateInPastException>("une date passée doit être rejetée lors d'une modification");
    }

    [Fact]
    public void Should_ThrowTimeOutsideServiceException_When_NewArrivalTimeIsInvalid()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService(); // LastBookingTime = 21:30
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(22, 0), 2, null, _clock);

        // Assert
        act.Should().Throw<TimeOutsideServiceException>("une heure hors plage doit être rejetée lors d'une modification");
    }

    [Fact]
    public void Should_ThrowGuestsBelowMinException_When_NewGuestsCountBelowMin()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 1, null, _clock);

        // Assert
        act.Should().Throw<GuestsBelowMinException>("un nombre de couverts inférieur au minimum doit être rejeté");
    }

    [Fact]
    public void Should_ThrowGuestsExceedCapacityException_When_NewGuestsCountExceedsCapacity()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 5, null, _clock);

        // Assert
        act.Should().Throw<GuestsExceedCapacityException>("un nombre de couverts supérieur à la capacité doit être rejeté");
    }

    [Fact]
    public void Should_ThrowBookingConflictException_When_NewSlotConflictsWithExisting()
    {
        // Arrange
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService(durationMinutes: 120);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        var existingBooking = Builders.PendingBooking(
            table: table, service: service,
            bookingDate: date,
            arrivalTime: new TimeOnly(19, 0),
            guestsCount: 2);

        var bookingToModify = Builders.PendingBooking();

        // Act
        Action act = () => bookingToModify.Modify(
            table, service, date, new TimeOnly(19, 30), 2, null, _clock,
            existingTableBookings: [existingBooking]);

        // Assert
        act.Should().Throw<BookingConflictException>("un conflit de créneau doit être rejeté lors d'une modification");
    }

    [Fact]
    public void Should_RepassToPending_When_ConfirmedBookingIsModified()
    {
        // Arrange
        var booking = Builders.ConfirmedBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        booking.Status.Should().Be(BookingStatus.Pending, "une modification remet la réservation en statut Pending");
    }

    [Fact]
    public void Should_KeepPending_When_PendingBookingIsModified()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        booking.Status.Should().Be(BookingStatus.Pending, "une réservation Pending reste Pending après modification");
    }

    [Fact]
    public void Should_UpdateFlags_When_SpecialRequestsChanges()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, "allergie aux arachides", _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeTrue("la demande spéciale contient 'allergie', le flag doit être activé");
    }
}
