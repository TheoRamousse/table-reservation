using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests des demandes spéciales et flags informatifs — FR-22, FR-23 (RB-012).</summary>
[Trait("Category", "Unit")]
public class BookingSpecialRequestsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    // ── FR-22 : Longueur ≤ 500 caractères ────────────────────────────────

    [Fact]
    public void Should_Throw_SpecialRequestsTooLongException_When_Over500Characters()
    {
        // Arrange
        var tooLong = new string('a', 501);
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: tooLong, clock: _clock);

        // Assert
        act.Should().Throw<SpecialRequestsTooLongException>("SpecialRequests > 500 caractères doit être rejeté (RB-012)");
    }

    [Fact]
    public void Should_CreateBooking_When_SpecialRequestsIs500Characters()
    {
        // Arrange
        var exactly500 = new string('a', 500);
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var act = () => Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: exactly500, clock: _clock);

        // Assert
        act.Should().NotThrow("500 caractères est la borne inclusive autorisée");
    }

    // ── FR-23 : Détection automatique des flags ───────────────────────────

    [Theory]
    [InlineData("Allergie aux arachides")]
    [InlineData("intolérance au gluten")]
    [InlineData("ALLERGIE sévère au lait")]
    public void Should_SetHasAllergyAlert_When_SpecialRequestsContainsAllergyKeyword(string specialRequests)
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: specialRequests, clock: _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeTrue("le mot-clé allergie/intolérance doit positionner HasAllergyAlert (FR-23)");
    }

    [Theory]
    [InlineData("anniversaire de mariage")]
    [InlineData("Mariage romantique")]
    [InlineData("FIANÇAILLES ce soir")]
    public void Should_SetIsCelebration_When_SpecialRequestsContainsCelebrationKeyword(string specialRequests)
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: specialRequests, clock: _clock);

        // Assert
        booking.IsCelebration.Should().BeTrue("le mot-clé anniversaire/mariage/fiançailles doit positionner IsCelebration (FR-23)");
    }

    [Theory]
    [InlineData("chaise bébé requise")]
    [InlineData("siège enfant pour 18 mois")]
    [InlineData("Chaise Bébé SVP")]
    public void Should_SetNeedsHighChair_When_SpecialRequestsContainsHighChairKeyword(string specialRequests)
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: specialRequests, clock: _clock);

        // Assert
        booking.NeedsHighChair.Should().BeTrue("le mot-clé chaise bébé/siège enfant doit positionner NeedsHighChair (FR-23)");
    }

    [Fact]
    public void Should_NotSetAnyFlag_When_SpecialRequestsIsNull()
    {
        // Arrange
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: null, clock: _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeFalse();
        booking.IsCelebration.Should().BeFalse();
        booking.NeedsHighChair.Should().BeFalse();
    }

    [Fact]
    public void Should_NotSetAnyFlag_When_SpecialRequestsHasNoKeyword()
    {
        // Arrange — texte non nul sans aucun mot-clé (tue les 7 mutations keyword→"")
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var customer = Builders.Customer();
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        var booking = Domain.Entities.Booking.Create(
            customer, table, service, date,
            arrivalTime: new TimeOnly(19, 30),
            guestsCount: 2, source: BookingSource.Online,
            specialRequests: "table au calme, vue sur jardin", clock: _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeFalse("aucun mot-clé allergie/intolérance présent (FR-23)");
        booking.IsCelebration.Should().BeFalse("aucun mot-clé anniversaire/mariage/fiançailles présent (FR-23)");
        booking.NeedsHighChair.Should().BeFalse("aucun mot-clé chaise bébé/siège enfant présent (FR-23)");
    }
}
