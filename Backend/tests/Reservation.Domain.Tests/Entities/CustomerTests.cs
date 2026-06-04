using FluentAssertions;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class CustomerTests
{
    // FR-9, FR-10, FR-11

    [Fact]
    public void Should_IncrementNoShowCount_When_NoShowRegistered()
    {
        // Arrange
        var customer = Builders.Customer(noShowCount: 0);

        // Act
        customer.RegisterNoShow();

        // Assert
        customer.NoShowCount.Should().Be(1);
    }

    [Fact]
    public void Should_SetIsBlacklisted_When_ThirdNoShowRegistered()
    {
        // Arrange
        var customer = Builders.Customer(noShowCount: 2);

        // Act
        customer.RegisterNoShow();

        // Assert
        customer.IsBlacklisted.Should().BeTrue("le 3e no-show déclenche la blacklist automatique (RB-007+RB-008)");
    }

    [Fact]
    public void Should_NotBlacklist_When_SecondNoShowRegistered()
    {
        // Arrange
        var customer = Builders.Customer(noShowCount: 1);

        // Act
        customer.RegisterNoShow();

        // Assert
        customer.IsBlacklisted.Should().BeFalse("la blacklist n'est déclenchée qu'au 3e no-show");
        customer.NoShowCount.Should().Be(2);
    }

    [Fact]
    public void Should_Throw_CustomerBlacklistedException_When_EnsureCanBookCalledOnBlacklisted()
    {
        // Arrange
        var customer = Builders.Customer(isBlacklisted: true);

        // Act
        Action act = () => customer.EnsureCanBook();

        // Assert
        act.Should().Throw<CustomerBlacklistedException>();
    }

    [Fact]
    public void Should_NotThrow_When_EnsureCanBookCalledOnNonBlacklisted()
    {
        // Arrange
        var customer = Builders.Customer(isBlacklisted: false);

        // Act
        Action act = () => customer.EnsureCanBook();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_SetVipLevelToNone_When_SecondLateCancelRegistered()
    {
        // Arrange
        var customer = Builders.Customer(vipLevel: VipLevel.VIP);
        customer.RegisterLateCancel(); // 1er late cancel — RB-009

        // Act
        customer.RegisterLateCancel(); // 2e late cancel → perte du statut VIP

        // Assert
        customer.VipLevel.Should().Be(VipLevel.None, "le 2e late cancel efface le statut VIP (RB-009)");
    }

    [Fact]
    public void Should_NotResetVipLevel_When_FirstLateCancelRegistered()
    {
        // Arrange
        var customer = Builders.Customer(vipLevel: VipLevel.VIP);

        // Act
        customer.RegisterLateCancel();

        // Assert
        customer.VipLevel.Should().Be(VipLevel.VIP, "le 1er late cancel ne remet pas encore le VIP à None");
    }

    [Fact]
    public void Should_CreateCustomer_When_ValidParameters()
    {
        // Act
        var act = () => Customer.Create("Jean", "Dupont", "0600000000");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_SetIsBlacklistedToFalse_When_LiftBlacklistCalled()
    {
        // Arrange
        var customer = Builders.Customer(isBlacklisted: true);

        // Act
        customer.LiftBlacklist();

        // Assert
        customer.IsBlacklisted.Should().BeFalse();
    }
}
