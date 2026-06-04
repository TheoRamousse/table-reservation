using FluentAssertions;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests des jours de fermeture — FR-25.</summary>
[Trait("Category", "Unit")]
public class ClosedDayTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Should_Create_ClosedDay_When_ValidParameters()
    {
        // Arrange
        var date = new DateOnly(2026, 12, 25);
        const string reason = "Noël";

        // Act
        var closedDay = ClosedDay.Create(date, reason, _clock);

        // Assert
        closedDay.Should().NotBeNull("un jour de fermeture valide doit être créé");
        closedDay.ClosedDate.Should().Be(date);
        closedDay.Reason.Should().Be(reason);
    }

    [Fact]
    public void Should_SetCreatedAt_When_ClosedDayCreated()
    {
        // Arrange
        var date = new DateOnly(2026, 12, 25);
        const string reason = "Noël";

        // Act
        var closedDay = ClosedDay.Create(date, reason, _clock);

        // Assert
        closedDay.CreatedAt.Should().Be(_clock.UtcNow, "CreatedAt doit correspondre à l'heure de l'horloge");
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_ReasonIsEmpty()
    {
        // Arrange
        var date = new DateOnly(2026, 12, 25);
        const string reason = "";

        // Act
        Action act = () => ClosedDay.Create(date, reason, _clock);

        // Assert
        act.Should().Throw<Exception>("une raison vide doit être rejetée");
    }

    [Fact]
    public void Should_Throw_RestaurantClosedException_When_BookingOnClosedDay()
    {
        // Arrange
        var customer = Builders.Customer();
        var table = Builders.Table();
        var service = Builders.DinnerService();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => Domain.Entities.Booking.Create(
            customer, table, service, futureDate,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock,
            isClosedDay: true);

        // Assert
        act.Should().Throw<RestaurantClosedException>("une réservation sur un jour fermé doit être rejetée (FR-25)");
    }

    [Fact]
    public void Should_Allow_Booking_When_DayIsNotClosed()
    {
        // Arrange
        var customer = Builders.Customer();
        var table = Builders.Table();
        var service = Builders.DinnerService();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => Domain.Entities.Booking.Create(
            customer, table, service, futureDate,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock,
            isClosedDay: false);

        // Assert
        act.Should().NotThrow("un jour non fermé doit être accepté normalement");
    }

    [Fact]
    public void Should_ExposeJourFermeReason_When_BookingOnClosedDay()
    {
        // Arrange
        var customer = Builders.Customer();
        var table = Builders.Table();
        var service = Builders.DinnerService();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var ex = Assert.Throws<RestaurantClosedException>(() =>
            Domain.Entities.Booking.Create(
                customer, table, service, futureDate,
                arrivalTime: new TimeOnly(19, 30),
                guestsCount: 2, source: BookingSource.Online,
                specialRequests: null, clock: _clock,
                isClosedDay: true));

        // Assert
        ex.Reason.Should().Be("Jour fermé", "la raison transmise à l'exception doit être 'Jour fermé'"); // l:79 String "Jour fermé"→""
    }
}
