using FluentAssertions;
using NSubstitute;
using Reservation.Application.Bookings.Commands;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Tests.Bookings;

[Trait("Category", "Unit")]
public class ModifyBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly ITableRepository _tableRepo = Substitute.For<ITableRepository>();
    private readonly IDiningServiceRepository _serviceRepo = Substitute.For<IDiningServiceRepository>();
    private readonly IClock _clock = new FakeClock(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    private ModifyBookingCommandHandler CreateHandler() =>
        new(_bookingRepo, _tableRepo, _serviceRepo, _clock);

    [Fact]
    public async Task Handle_Should_ThrowEntityNotFoundException_When_BookingNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        var cmd = new ModifyBookingCommand(
            unknownId,
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, null, null);

        _bookingRepo.GetByIdAsync(unknownId, Arg.Any<CancellationToken>())
            .Returns((Booking?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>(
            "le handler doit lever EntityNotFoundException quand la réservation n'existe pas");
    }

    [Fact]
    public async Task Handle_Should_ThrowEntityNotFoundException_When_ServiceNotFound()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var cmd = new ModifyBookingCommand(
            booking.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, null, null);

        _bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(booking);
        _serviceRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DiningService?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>(
            "le handler doit lever EntityNotFoundException quand le service n'existe pas");
    }

    [Fact]
    public async Task Handle_Should_CallSaveChanges_When_BookingModified()
    {
        // Arrange
        var service = Builders.DinnerService();
        var booking = Builders.PendingBooking(serviceId: service.Id);
        var cmd = new ModifyBookingCommand(
            booking.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, null, null);

        _bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(booking);
        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, default);

        // Assert
        await _bookingRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBookingDto_When_BookingModified()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var booking = Builders.PendingBooking(serviceId: service.Id, tableId: table.Id);
        var cmd = new ModifyBookingCommand(
            booking.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, null, table.Id);

        _bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(booking);
        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _tableRepo.GetByIdAsync(table.Id, Arg.Any<CancellationToken>())
            .Returns(table);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.Should().BeOfType<BookingDto>();
        result.Id.Should().Be(booking.Id);
    }
}
