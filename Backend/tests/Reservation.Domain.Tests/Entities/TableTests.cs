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
        act.Should().Throw<ArgumentException>("la capacité minimale ne peut pas dépasser la capacité totale");
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
}
