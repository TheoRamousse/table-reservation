using FluentAssertions;
using NSubstitute;
using Reservation.Application.ClosedDays.Commands;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Tests.ClosedDays;

[Trait("Category", "Unit")]
public class DeclareClosedDayCommandHandlerTests
{
    private readonly IClosedDayRepository _closedDayRepo = Substitute.For<IClosedDayRepository>();
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly IClock _clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    private DeclareClosedDayCommandHandler CreateHandler() =>
        new(_closedDayRepo, _bookingRepo, _clock);

    [Fact]
    public async Task Handle_Should_CancelAllPendingAndConfirmedBookings_When_DayDeclaredClosed()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var cmd = new DeclareClosedDayCommand(date, "Travaux");

        var pending = Builders.PendingBooking(bookingDate: date);
        var confirmed = Builders.ConfirmedBooking(bookingDate: date);

        _bookingRepo.GetPendingAndConfirmedByDateAsync(date, default)
            .Returns([pending, confirmed]);
        _closedDayRepo.GetByDateAsync(date, default).Returns((Domain.Entities.ClosedDay?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }

    [Fact]
    public async Task Handle_Should_NotCancelSeatedOrCompleted_When_DayDeclaredClosed()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var cmd = new DeclareClosedDayCommand(date, "Travaux");

        // GetPendingAndConfirmedByDateAsync retourne uniquement Pending+Confirmed
        _bookingRepo.GetPendingAndConfirmedByDateAsync(date, default).Returns([]);
        _closedDayRepo.GetByDateAsync(date, default).Returns((Domain.Entities.ClosedDay?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }

    [Fact]
    public async Task Handle_Should_ReturnCancelledCount()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var cmd = new DeclareClosedDayCommand(date, "Travaux");

        var pending = Builders.PendingBooking(bookingDate: date);
        var confirmed = Builders.ConfirmedBooking(bookingDate: date);

        _bookingRepo.GetPendingAndConfirmedByDateAsync(date, default)
            .Returns([pending, confirmed]);
        _closedDayRepo.GetByDateAsync(date, default).Returns((Domain.Entities.ClosedDay?)null);

        var handler = CreateHandler();

        // Act
        Func<Task<DeclareClosedDayResult>> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }

    [Fact]
    public async Task Handle_Should_SaveClosedDay()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var cmd = new DeclareClosedDayCommand(date, "Travaux");

        _bookingRepo.GetPendingAndConfirmedByDateAsync(date, default).Returns([]);
        _closedDayRepo.GetByDateAsync(date, default).Returns((Domain.Entities.ClosedDay?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }
}
