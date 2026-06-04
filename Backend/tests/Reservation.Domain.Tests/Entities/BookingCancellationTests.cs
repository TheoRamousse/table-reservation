using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests d'annulation d'une réservation — FR-11 (RB-009).</summary>
[Trait("Category", "Unit")]
public class BookingCancellationTests
{
    // ── LateCancel (RB-009) ───────────────────────────────────────────────

    [Fact]
    public void Should_SetLateCancel_When_CancelledWithin24HoursOfArrival()
    {
        // Arrange — arrivalTime = 20h le 5 juin, annulation le 4 juin à 21h (< 24h)
        var arrivalTime = new TimeOnly(20, 0);
        var bookingDate = new DateOnly(2026, 6, 5);
        var now = new DateTimeOffset(2026, 6, 4, 21, 0, 0, TimeSpan.Zero); // 23h avant
        var booking = Builders.ConfirmedBooking(bookingDate: bookingDate, arrivalTime: arrivalTime);

        // Act
        booking.Cancel("Indisponible", now);

        // Assert
        booking.LateCancel.Should().BeTrue("annulation < 24h avant l'arrivée = LateCancel (RB-009)");
    }

    [Fact]
    public void Should_NotSetLateCancel_When_CancelledMoreThan24HoursBeforeArrival()
    {
        // Arrange — arrivalTime = 20h le 6 juin, annulation le 4 juin à 10h (> 24h)
        var arrivalTime = new TimeOnly(20, 0);
        var bookingDate = new DateOnly(2026, 6, 6);
        var now = new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero); // 58h avant
        var booking = Builders.ConfirmedBooking(bookingDate: bookingDate, arrivalTime: arrivalTime);

        // Act
        booking.Cancel("Indisponible", now);

        // Assert
        booking.LateCancel.Should().BeFalse("annulation > 24h avant l'arrivée n'est pas un LateCancel");
    }

    [Fact]
    public void Should_SetStatusToCancelled_When_CancelCalled()
    {
        // Arrange
        var booking = Builders.PendingBooking();

        // Act
        booking.Cancel("Client indisponible", DateTimeOffset.UtcNow);

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Should_StoreCancellationReason_When_CancelCalled()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        const string reason = "Déménagement imprévu";

        // Act
        booking.Cancel(reason, DateTimeOffset.UtcNow);

        // Assert
        booking.CancellationReason.Should().Be(reason);
    }

    // ── Annulation interdite sur Seated / Completed ────────────────────────

    [Fact]
    public void Should_Throw_CannotCancelSeatedException_When_BookingIsSeated()
    {
        // Arrange
        var booking = Builders.SeatedBooking();

        // Act
        Action act = () => booking.Cancel("Urgence", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<CannotCancelSeatedException>("impossible d'annuler une réservation Seated (RB-009)");
    }

    [Fact]
    public void Should_Throw_When_CancelledWithoutReason()
    {
        // Arrange
        var booking = Builders.PendingBooking();

        // Act
        Action act = () => booking.Cancel(string.Empty, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>("CancellationReason est obligatoire");
    }
}
