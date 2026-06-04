using FluentAssertions;
using NSubstitute;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Application.Tables.Queries;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tests.Tables;

[Trait("Category", "Unit")]
public class GetAvailabilityQueryHandlerTests
{
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly ITableRepository _tableRepo = Substitute.For<ITableRepository>();
    private readonly IDiningServiceRepository _serviceRepo = Substitute.For<IDiningServiceRepository>();

    private GetAvailabilityQueryHandler CreateHandler() =>
        new(_bookingRepo, _tableRepo, _serviceRepo);

    [Fact]
    public async Task Handle_Should_ThrowEntityNotFoundException_When_ServiceNotFound()
    {
        // Arrange
        var query = new GetAvailabilityQuery(
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Guid.NewGuid(), 2, null);

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
    public async Task Handle_Should_ReturnEmptyWithReason_When_ServiceIsFullyBooked()
    {
        // Arrange
        var service = Builders.DinnerService(maxCovers: 10);
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var query = new GetAvailabilityQuery(date, service.Id, 2, null);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _bookingRepo.GetExistingCoversAsync(service.Id, date, Arg.Any<CancellationToken>()).Returns(10);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().BeEmpty("le service est complet, aucune table disponible");
        result.Reason.Should().NotBeNullOrEmpty("une raison doit être fournie quand le service est complet");
    }

    [Fact]
    public async Task Handle_Should_ExcludeTablesWithActiveBookings()
    {
        // Arrange
        var service = Builders.DinnerService();
        var tableWithBooking = Builders.Table(number: 1);
        var freeTable = Builders.Table(number: 2);
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var query = new GetAvailabilityQuery(date, service.Id, 2, null);

        var activeBooking = Builders.PendingBooking(tableId: tableWithBooking.Id, serviceId: service.Id, bookingDate: date);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([tableWithBooking, freeTable]);
        _bookingRepo.GetByDateAndServiceAsync(date, service.Id, Arg.Any<CancellationToken>()).Returns([activeBooking]);
        _bookingRepo.GetExistingCoversAsync(service.Id, date, Arg.Any<CancellationToken>()).Returns(2);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(1, "la table avec réservation active doit être exclue");
        result.Tables[0].Id.Should().Be(freeTable.Id, "seule la table libre doit être retournée");
    }

    [Fact]
    public async Task Handle_Should_FilterByZone_When_ZoneProvided()
    {
        // Arrange
        var service = Builders.DinnerService();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var query = new GetAvailabilityQuery(date, service.Id, 2, TableZone.Terrasse);

        var salleTable = new Domain.Entities.Table(Guid.NewGuid(), 1, 4, 2, TableZone.Salle, false, true);
        var terrasseTable = new Domain.Entities.Table(Guid.NewGuid(), 2, 4, 2, TableZone.Terrasse, false, true);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([salleTable, terrasseTable]);
        _bookingRepo.GetByDateAndServiceAsync(date, service.Id, Arg.Any<CancellationToken>()).Returns([]);
        _bookingRepo.GetExistingCoversAsync(service.Id, date, Arg.Any<CancellationToken>()).Returns(0);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(1, "seules les tables de la zone Terrasse doivent être retournées");
        result.Tables[0].Zone.Should().Be(TableZone.Terrasse);
    }

    [Fact]
    public async Task Handle_Should_SortByCapacityAscending()
    {
        // Arrange
        var service = Builders.DinnerService();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var query = new GetAvailabilityQuery(date, service.Id, 2, null);

        var largeTable = Builders.Table(capacity: 10, minCapacity: 2, number: 1);
        var smallTable = Builders.Table(capacity: 4, minCapacity: 2, number: 2);

        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _tableRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([largeTable, smallTable]);
        _bookingRepo.GetByDateAndServiceAsync(date, service.Id, Arg.Any<CancellationToken>()).Returns([]);
        _bookingRepo.GetExistingCoversAsync(service.Id, date, Arg.Any<CancellationToken>()).Returns(0);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Tables.Should().HaveCount(2);
        result.Tables[0].Capacity.Should().BeLessThanOrEqualTo(result.Tables[1].Capacity,
            "les tables doivent être triées par capacité croissante");
    }
}
