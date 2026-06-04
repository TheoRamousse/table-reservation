using FluentAssertions;
using NSubstitute;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Application.Services.Commands;

namespace Reservation.Application.Tests.Services;

[Trait("Category", "Unit")]
public class CreateDiningServiceCommandHandlerTests
{
    private readonly IDiningServiceRepository _serviceRepo = Substitute.For<IDiningServiceRepository>();

    private CreateDiningServiceCommandHandler CreateHandler() =>
        new(_serviceRepo);

    [Fact]
    public async Task Handle_Should_CreateService_When_ValidCommand()
    {
        // Arrange
        var cmd = new CreateDiningServiceCommand(
            "Déjeuner",
            new TimeOnly(12, 0),
            new TimeOnly(14, 30),
            new TimeOnly(13, 30),
            90, 40);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.Should().BeOfType<DiningServiceDto>("le résultat doit être un DiningServiceDto");
        result.Name.Should().Be("Déjeuner");
    }

    [Fact]
    public async Task Handle_Should_SaveService_When_Created()
    {
        // Arrange
        var cmd = new CreateDiningServiceCommand(
            "Dîner",
            new TimeOnly(19, 0),
            new TimeOnly(23, 0),
            new TimeOnly(21, 30),
            120, 60);

        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, default);

        // Assert
        await _serviceRepo.Received(1).AddAsync(
            Arg.Any<Domain.Entities.DiningService>(), Arg.Any<CancellationToken>());
        await _serviceRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnDiningServiceDto_When_Created()
    {
        // Arrange
        var cmd = new CreateDiningServiceCommand(
            "Brunch",
            new TimeOnly(10, 0),
            new TimeOnly(13, 0),
            new TimeOnly(12, 0),
            60, 30);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.Should().BeOfType<DiningServiceDto>("le handler doit retourner un DiningServiceDto");
        result.Id.Should().NotBeEmpty("l'ID du service doit être généré");
    }
}
