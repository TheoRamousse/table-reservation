using FluentAssertions;
using NSubstitute;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Floor.Queries;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Application.Tests.Floor;

[Trait("Category", "Unit")]
public class GetFloorSnapshotQueryHandlerTests
{
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly ITableRepository _tableRepo = Substitute.For<ITableRepository>();
    private readonly IDiningServiceRepository _serviceRepo = Substitute.For<IDiningServiceRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();

    private GetFloorSnapshotQueryHandler CreateHandler() =>
        new(_bookingRepo, _tableRepo, _serviceRepo, _customerRepo);

    [Fact]
    public async Task Handle_Should_ThrowEntityNotFoundException_When_ServiceNotFound()
    {
        // Arrange
        var query = new GetFloorSnapshotQuery(DateOnly.FromDateTime(DateTime.Today), Guid.NewGuid());

        _serviceRepo.GetByIdAsync(query.ServiceId, Arg.Any<CancellationToken>())
            .Returns((DiningService?)null);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(query, default);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>(
            "le handler doit lever EntityNotFoundException quand le service n'existe pas");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyTables_When_NoActiveTables()
    {
        // Arrange
        var service = Builders.DinnerService();
        var query = new GetFloorSnapshotQuery(DateOnly.FromDateTime(DateTime.Today), service.Id);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([]);
        _bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().BeEmpty("aucune table active ne doit retourner une liste vide");
    }

    [Fact]
    public async Task Handle_Should_SetFreeStatus_When_NoActiveBooking()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var query = new GetFloorSnapshotQuery(DateOnly.FromDateTime(DateTime.Today), service.Id);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([table]);
        _bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, Arg.Any<CancellationToken>()).Returns([]);
        _customerRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(1);
        result.Tables[0].Status.Should().Be(TableStatus.Free,
            "une table sans réservation active doit avoir le statut Free");
    }

    [Fact]
    public async Task Handle_Should_SetPendingStatus_When_TableHasPendingBooking()
    {
        // Arrange
        var service = Builders.DinnerService(id: Guid.NewGuid());
        var table = Builders.Table();
        var booking = Builders.PendingBooking(tableId: table.Id, serviceId: service.Id);
        var query = new GetFloorSnapshotQuery(DateOnly.FromDateTime(DateTime.Today), service.Id);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([table]);
        _bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, Arg.Any<CancellationToken>())
            .Returns([booking]);
        _customerRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(1);
        result.Tables[0].Status.Should().Be(TableStatus.Pending,
            "une table avec une réservation Pending doit avoir le statut Pending");
    }

    [Fact]
    public async Task Handle_Should_SetConfirmedStatus_When_TableHasConfirmedBooking()
    {
        // Arrange
        var service = Builders.DinnerService(id: Guid.NewGuid());
        var table = Builders.Table();
        var booking = Builders.ConfirmedBooking(tableId: table.Id, serviceId: service.Id);
        var query = new GetFloorSnapshotQuery(DateOnly.FromDateTime(DateTime.Today), service.Id);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([table]);
        _bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, Arg.Any<CancellationToken>())
            .Returns([booking]);
        _customerRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(1);
        result.Tables[0].Status.Should().Be(TableStatus.Confirmed,
            "une table avec une réservation Confirmed doit avoir le statut Confirmed");
    }
}
