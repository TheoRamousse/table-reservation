using FluentAssertions;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class TableTests
{
    [Fact]
    public void Should_CreateTable_When_ValidParameters()
    {
        // Act
        var act = () => Table.Create(number: 5, capacity: 6, minCapacity: 2, zone: TableZone.Terrasse);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_SetProperties_When_Created()
    {
        // Act
        var table = Table.Create(number: 5, capacity: 6, minCapacity: 2, zone: TableZone.Terrasse, isCombinable: true);

        // Assert
        table.Number.Should().Be(5);
        table.Capacity.Should().Be(6);
        table.MinCapacity.Should().Be(2);
        table.Zone.Should().Be(TableZone.Terrasse);
        table.IsCombinable.Should().BeTrue();
        table.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_Throw_When_MinCapacityExceedsCapacity()
    {
        // Act
        var act = () => Table.Create(number: 1, capacity: 4, minCapacity: 5, zone: TableZone.Salle);

        // Assert
        act.Should().Throw<ArgumentException>("la capacité minimale ne peut pas dépasser la capacité totale")
           .WithMessage("*ne peut pas dépasser*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Throw_When_CapacityIsNotPositive(int capacity)
    {
        // Act
        var act = () => Table.Create(number: 1, capacity: capacity, minCapacity: 1, zone: TableZone.Salle);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Throw_When_CapacityIsZero_WithCapacityParamName()
    {
        // Arrange — capacity=0 avec minCapacity=1 : distingue le paramètre "capacity" de "minCapacity"
        var act = () => Table.Create(number: 1, capacity: 0, minCapacity: 1, zone: TableZone.Salle);

        // Assert
        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.ParamName.Should().Be("capacity");
        ex.Message.Should().Contain("capacité");
    }

    [Fact]
    public void Should_Throw_When_MinCapacityIsZero()
    {
        // Arrange — minCapacity=0 avec capacity valide
        var act = () => Table.Create(number: 1, capacity: 4, minCapacity: 0, zone: TableZone.Salle);

        // Assert
        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.ParamName.Should().Be("minCapacity");
        ex.Message.Should().Contain("minimale");
    }

    [Fact]
    public void Should_CreateTable_When_MinCapacityEqualsCapacity()
    {
        // Act — minCapacity == capacity est la borne inclusive autorisée
        var act = () => Table.Create(number: 1, capacity: 4, minCapacity: 4, zone: TableZone.Salle);

        // Assert
        act.Should().NotThrow("minCapacity == capacity est valide (borne inclusive)");
    }
}
