using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests des transitions de statut — FR-12 (RB-014), FR-10 (RB-008).</summary>
[Trait("Category", "Unit")]
public class BookingStatusTransitionTests
{
    private readonly DateTimeOffset _now = new(2026, 6, 4, 10, 0, 0, TimeSpan.Zero);

    // ── Transitions autorisées ─────────────────────────────────────────────

    [Fact]
    public void Should_TransitionToConfirmed_When_BookingIsPending()
    {
        // Arrange
        var booking = Builders.PendingBooking();

        // Act
        booking.TransitionTo(BookingStatus.Confirmed, _now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Should_TransitionToSeated_When_BookingIsConfirmed()
    {
        // Arrange
        var booking = Builders.ConfirmedBooking();

        // Act
        booking.TransitionTo(BookingStatus.Seated, _now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Seated);
    }

    [Fact]
    public void Should_TransitionToCompleted_When_BookingIsSeated()
    {
        // Arrange
        var booking = Builders.SeatedBooking();

        // Act
        booking.TransitionTo(BookingStatus.Completed, _now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public void Should_TransitionToRejected_When_BookingIsPending()
    {
        // Arrange
        var booking = Builders.PendingBooking();

        // Act
        booking.TransitionTo(BookingStatus.Rejected, _now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
    }

    // ── No-show (RB-008) ─────────────────────────────────────────────────

    [Fact]
    public void Should_Throw_NoShowTooEarlyException_When_NoShowBeforeArrivalPlus15Min()
    {
        // Arrange — arrivalTime = 20h, now = 20h10 (< 20h15)
        var arrivalTime = new TimeOnly(20, 0);
        var nowBeforeThreshold = new DateTimeOffset(2026, 6, 4, 20, 10, 0, TimeSpan.Zero);
        var booking = Builders.ConfirmedBooking(
            bookingDate: new DateOnly(2026, 6, 4),
            arrivalTime: arrivalTime);

        // Act
        Action act = () => booking.TransitionTo(BookingStatus.NoShow, nowBeforeThreshold);

        // Assert
        act.Should().Throw<NoShowTooEarlyException>("le no-show requiert ArrivalTime + 15 min (RB-008)");
    }

    [Fact]
    public void Should_TransitionToNoShow_When_ArrivalPlus15MinHasPassed()
    {
        // Arrange — arrivalTime = 20h, now = 20h15
        var arrivalTime = new TimeOnly(20, 0);
        var nowAtThreshold = new DateTimeOffset(2026, 6, 4, 20, 15, 0, TimeSpan.Zero);
        var booking = Builders.ConfirmedBooking(
            bookingDate: new DateOnly(2026, 6, 4),
            arrivalTime: arrivalTime);

        // Act
        booking.TransitionTo(BookingStatus.NoShow, nowAtThreshold);

        // Assert
        booking.Status.Should().Be(BookingStatus.NoShow);
    }

    // ── Transitions interdites (RB-014) ───────────────────────────────────

    [Theory]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Completed, BookingStatus.Seated)]
    [InlineData(BookingStatus.NoShow, BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected, BookingStatus.Pending)]
    public void Should_Throw_InvalidStatusTransitionException_When_TransitionIsNotAllowed(
        BookingStatus from, BookingStatus to)
    {
        // Arrange
        var booking = Builders.BookingInStatus(from);

        // Act
        Action act = () => booking.TransitionTo(to, _now);

        // Assert
        act.Should().Throw<InvalidStatusTransitionException>(
            $"la transition {from} → {to} n'est pas autorisée (RB-014)");
    }
}
