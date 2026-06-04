using FluentAssertions;
using NSubstitute;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Application.Tables.Commands;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tests.Tables;

[Trait("Category", "Unit")]
public class CreateTableCommandHandlerTests
{
    private readonly ITableRepository _tableRepo = Substitute.For<ITableRepository>();

    private CreateTableCommandHandler CreateHandler() =>
        new(_tableRepo);

    [Fact]
    public async Task Handle_Should_CreateTable_When_ValidCommand()
    {
        // Arrange
        var cmd = new CreateTableCommand(1, 6, 2, TableZone.Salle, false);

        var handler = CreateHandler();

        // Act
        Func<Task<TableDto>> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }

    [Fact]
    public async Task Handle_Should_SaveTable_When_Created()
    {
        // Arrange
        var cmd = new CreateTableCommand(2, 4, 2, TableZone.SalonPrive, true);

        var handler = CreateHandler();

        // Act
        Func<Task> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }

    [Fact]
    public async Task Handle_Should_ReturnTableDto_When_Created()
    {
        // Arrange
        var cmd = new CreateTableCommand(3, 8, 4, TableZone.Terrasse, false);

        var handler = CreateHandler();

        // Act
        Func<Task<TableDto>> act = () => handler.Handle(cmd, default);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>(
            "le stub lève NotImplementedException en phase RED");
    }
}
