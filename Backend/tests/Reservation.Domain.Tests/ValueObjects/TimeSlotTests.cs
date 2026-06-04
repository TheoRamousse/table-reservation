using FluentAssertions;
using Reservation.Domain.ValueObjects;

namespace Reservation.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class TimeSlotTests
{
    // FR-2, FR-5 — créneau d'occupation et détection de conflits

    [Fact]
    public void Should_ReturnFalse_When_SlotsAreBackToBack()
    {
        // Arrange
        var base_ = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var slotA = new TimeSlot(base_, base_.AddHours(2));          // 12h–14h
        var slotB = new TimeSlot(base_.AddHours(2), base_.AddHours(4)); // 14h–16h

        // Act
        var result = slotA.Overlaps(slotB);

        // Assert
        result.Should().BeFalse("des créneaux dos à dos ne se chevauchent pas (A.End == B.Start)");
    }

    [Fact]
    public void Should_ReturnTrue_When_SlotsOverlapByOneMinute()
    {
        // Arrange
        var base_ = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var slotA = new TimeSlot(base_, base_.AddHours(2));                    // 12h–14h
        var slotB = new TimeSlot(base_.AddHours(2).AddMinutes(-1), base_.AddHours(4)); // 13h59–16h

        // Act
        var result = slotA.Overlaps(slotB);

        // Assert
        result.Should().BeTrue("un chevauchement d'une minute constitue un conflit");
    }

    [Fact]
    public void Should_ReturnTrue_When_SlotBIsContainedInSlotA()
    {
        // Arrange
        var base_ = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var slotA = new TimeSlot(base_, base_.AddHours(3));          // 12h–15h
        var slotB = new TimeSlot(base_.AddHours(1), base_.AddHours(2)); // 13h–14h

        // Act
        var result = slotA.Overlaps(slotB);

        // Assert
        result.Should().BeTrue("un créneau contenu dans un autre est en conflit");
    }

    [Fact]
    public void Should_BeSymmetric_When_OverlapCheckedInBothDirections()
    {
        // Arrange
        var base_ = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var slotA = new TimeSlot(base_, base_.AddHours(2));
        var slotB = new TimeSlot(base_.AddHours(1), base_.AddHours(3));

        // Act & Assert
        slotA.Overlaps(slotB).Should().Be(slotB.Overlaps(slotA));
    }
}
