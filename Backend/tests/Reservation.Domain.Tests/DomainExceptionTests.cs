using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests;

/// <summary>Vérifie que chaque exception domain expose ses propriétés et un message non vide.</summary>
[Trait("Category", "Unit")]
public class DomainExceptionTests
{
    [Fact]
    public void BookingConflictException_Should_ExposeIdsAndMessage()
    {
        var tableId   = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var ex = new BookingConflictException(tableId, bookingId);

        ex.TableId.Should().Be(tableId);
        ex.ConflictingBookingId.Should().Be(bookingId);
        ex.Message.Should().Contain(tableId.ToString());
    }

    [Fact]
    public void BookingDateInPastException_Should_ExposeProvidedDateAndMessage()
    {
        var provided = new DateOnly(2026, 1, 1);

        var ex = new BookingDateInPastException(provided);

        ex.Provided.Should().Be(provided);
        ex.Message.Should().Contain(provided.ToString());
    }

    [Fact]
    public void CannotCancelSeatedException_Should_ExposeStatusAndMessage()
    {
        var ex = new CannotCancelSeatedException(BookingStatus.Seated);

        ex.CurrentStatus.Should().Be(BookingStatus.Seated);
        ex.Message.Should().Contain("Seated");
    }

    [Fact]
    public void CustomerBlacklistedException_Should_ExposeIdAndMessage()
    {
        var customerId = Guid.NewGuid();

        var ex = new CustomerBlacklistedException(customerId);

        ex.CustomerId.Should().Be(customerId);
        ex.Message.Should().Contain(customerId.ToString());
    }

    [Fact]
    public void GuestsBelowMinException_Should_ExposeValuesAndMessage()
    {
        var ex = new GuestsBelowMinException(minCapacity: 4, provided: 1);

        ex.MinCapacity.Should().Be(4);
        ex.Provided.Should().Be(1);
        ex.Message.Should().Contain("4");
    }

    [Fact]
    public void GuestsExceedCapacityException_Should_ExposeValuesAndMessage()
    {
        var ex = new GuestsExceedCapacityException(capacity: 4, provided: 6);

        ex.Capacity.Should().Be(4);
        ex.Provided.Should().Be(6);
        ex.Message.Should().Contain("4");
    }

    [Fact]
    public void HorizonExceededException_Should_ExposeValuesAndMessage()
    {
        var ex = new HorizonExceededException(maxDays: 30, provided: 45);

        ex.MaxDays.Should().Be(30);
        ex.Provided.Should().Be(45);
        ex.Message.Should().Contain("30");
    }

    [Fact]
    public void InvalidStatusTransitionException_Should_ExposeFromToAndMessage()
    {
        var ex = new InvalidStatusTransitionException(BookingStatus.Pending, BookingStatus.Completed);

        ex.From.Should().Be(BookingStatus.Pending);
        ex.To.Should().Be(BookingStatus.Completed);
        ex.Message.Should().Contain("Pending");
    }

    [Fact]
    public void MinLeadTimeViolatedException_Should_ExposeValuesAndMessage()
    {
        var ex = new MinLeadTimeViolatedException(minMinutes: 120, minutesLeft: 45);

        ex.MinMinutes.Should().Be(120);
        ex.MinutesLeft.Should().Be(45);
        ex.Message.Should().Contain("120");
    }

    [Fact]
    public void NoShowTooEarlyException_Should_ExposeThresholdAndMessage()
    {
        var threshold = new DateTimeOffset(2026, 6, 4, 20, 15, 0, TimeSpan.Zero);

        var ex = new NoShowTooEarlyException(threshold);

        ex.EarliestNoShowAt.Should().Be(threshold);
        ex.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void ServiceFullyBookedException_Should_ExposeValuesAndMessage()
    {
        var ex = new ServiceFullyBookedException(maxCovers: 10, currentTotal: 13);

        ex.MaxCovers.Should().Be(10);
        ex.CurrentTotal.Should().Be(13);
        ex.Message.Should().Contain("10");
    }

    [Fact]
    public void SpecialRequestsTooLongException_Should_ExposeValuesAndMessage()
    {
        var ex = new SpecialRequestsTooLongException(maxLength: 500, provided: 512);

        ex.MaxLength.Should().Be(500);
        ex.Provided.Should().Be(512);
        ex.Message.Should().Contain("500");
    }

    [Fact]
    public void TimeOutsideServiceException_Should_ExposeTimesAndMessage()
    {
        var arrival     = new TimeOnly(22, 0);
        var lastBooking = new TimeOnly(21, 30);

        var ex = new TimeOutsideServiceException(arrival, lastBooking);

        ex.ArrivalTime.Should().Be(arrival);
        ex.LastBookingTime.Should().Be(lastBooking);
        ex.Message.Should().Contain(arrival.ToString());
    }

    [Fact]
    public void WalkInMustBeTodayException_Should_ExposeDatesAndMessage()
    {
        var today    = new DateOnly(2026, 6, 4);
        var provided = new DateOnly(2026, 6, 5);

        var ex = new WalkInMustBeTodayException(today, provided);

        ex.Today.Should().Be(today);
        ex.Provided.Should().Be(provided);
        ex.Message.Should().Contain(provided.ToString());
    }

    [Fact]
    public void RestaurantClosedException_Should_ExposePropertiesAndMessage()
    {
        var closedDate = new DateOnly(2026, 12, 25);
        const string reason = "Noël";

        var ex = new RestaurantClosedException(closedDate, reason);

        ex.ClosedDate.Should().Be(closedDate);
        ex.Reason.Should().Be(reason);
        ex.Message.Should().Contain(closedDate.ToString()); // l:4 String message→""
    }
}
