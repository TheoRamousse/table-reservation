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
        var result = await handler.Handle(cmd, default);

        // Assert
        result.Should().BeOfType<TableDto>("le résultat doit être un TableDto");
        result.Number.Should().Be(1);
        result.Capacity.Should().Be(6);
    }

    [Fact]
    public async Task Handle_Should_SaveTable_When_Created()
    {
        // Arrange
        var cmd = new CreateTableCommand(2, 4, 2, TableZone.SalonPrive, true);

        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, default);

        // Assert
        await _tableRepo.Received(1).AddAsync(Arg.Any<Domain.Entities.Table>(), Arg.Any<CancellationToken>());
        await _tableRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTableDto_When_Created()
    {
        // Arrange
        var cmd = new CreateTableCommand(3, 8, 4, TableZone.Terrasse, false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.Should().BeOfType<TableDto>("le handler doit retourner un TableDto");
        result.Id.Should().NotBeEmpty("l'ID de la table doit être généré");
    }
}
