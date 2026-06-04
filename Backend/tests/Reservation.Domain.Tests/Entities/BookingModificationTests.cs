using FluentAssertions;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Tests.Entities;

/// <summary>Tests de modification d'une réservation — FR-13.</summary>
[Trait("Category", "Unit")]
public class BookingModificationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 6, 4, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Should_ThrowBookingDateInPastException_When_NewDateIsInPast()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var pastDate = new DateOnly(2026, 6, 3); // hier par rapport à l'horloge

        // Act
        Action act = () => booking.Modify(table, service, pastDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        act.Should().Throw<BookingDateInPastException>("une date passée doit être rejetée lors d'une modification");
    }

    [Fact]
    public void Should_ThrowTimeOutsideServiceException_When_NewArrivalTimeIsInvalid()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService(); // LastBookingTime = 21:30
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(22, 0), 2, null, _clock);

        // Assert
        act.Should().Throw<TimeOutsideServiceException>("une heure hors plage doit être rejetée lors d'une modification");
    }

    [Fact]
    public void Should_ThrowGuestsBelowMinException_When_NewGuestsCountBelowMin()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 1, null, _clock);

        // Assert
        act.Should().Throw<GuestsBelowMinException>("un nombre de couverts inférieur au minimum doit être rejeté");
    }

    [Fact]
    public void Should_ThrowGuestsExceedCapacityException_When_NewGuestsCountExceedsCapacity()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 5, null, _clock);

        // Assert
        act.Should().Throw<GuestsExceedCapacityException>("un nombre de couverts supérieur à la capacité doit être rejeté");
    }

    [Fact]
    public void Should_ThrowBookingConflictException_When_NewSlotConflictsWithExisting()
    {
        // Arrange
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var service = Builders.DinnerService(durationMinutes: 120);
        var date = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        var existingBooking = Builders.PendingBooking(
            table: table, service: service,
            bookingDate: date,
            arrivalTime: new TimeOnly(19, 0),
            guestsCount: 2);

        var bookingToModify = Builders.PendingBooking();

        // Act
        Action act = () => bookingToModify.Modify(
            table, service, date, new TimeOnly(19, 30), 2, null, _clock,
            existingTableBookings: [existingBooking]);

        // Assert
        act.Should().Throw<BookingConflictException>("un conflit de créneau doit être rejeté lors d'une modification");
    }

    [Fact]
    public void Should_RepassToPending_When_ConfirmedBookingIsModified()
    {
        // Arrange
        var booking = Builders.ConfirmedBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        booking.Status.Should().Be(BookingStatus.Pending, "une modification remet la réservation en statut Pending");
    }

    [Fact]
    public void Should_KeepPending_When_PendingBookingIsModified()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        booking.Status.Should().Be(BookingStatus.Pending, "une réservation Pending reste Pending après modification");
    }

    [Fact]
    public void Should_UpdateFlags_When_SpecialRequestsChanges()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, "allergie aux arachides", _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeTrue("la demande spéciale contient 'allergie', le flag doit être activé");
    }

    // ── FR-22 : borne de longueur dans Modify ─────────────────────────────

    [Fact]
    public void Should_NotThrow_When_SpecialRequestsIs500CharactersOnModify()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));
        var exactly500 = new string('a', 500);

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, exactly500, _clock);

        // Assert
        act.Should().NotThrow("500 caractères est la borne inclusive dans Modify (FR-22)"); // l:222 Equality >500→>=500
    }

    // ── FR-8 : borne de date dans Modify ──────────────────────────────────

    [Fact]
    public void Should_NotThrow_When_NewBookingDateIsToday()
    {
        // Arrange
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var today = DateOnly.FromDateTime(_clock.UtcNow.Date);

        // Act
        Action act = () => booking.Modify(table, service, today, new TimeOnly(19, 30), 2, null, _clock);

        // Assert
        act.Should().NotThrow("aujourd'hui n'est pas dans le passé, Modify doit accepter la date courante (FR-8)"); // l:228 Equality <today→<=today
    }

    // ── FR-1 : bornes de plage horaire dans Modify ────────────────────────

    [Fact]
    public void Should_NotThrow_When_NewArrivalTimeEqualsStartTime()
    {
        // Arrange — service.StartTime = 19:00
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 0), 2, null, _clock);

        // Assert
        act.Should().NotThrow("ArrivalTime == StartTime est la borne inclusive dans Modify (FR-1)"); // l:232 Equality <StartTime→<=StartTime
    }

    [Fact]
    public void Should_NotThrow_When_NewArrivalTimeEqualsLastBookingTime()
    {
        // Arrange — service.LastBookingTime = 21:30
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(21, 30), 2, null, _clock);

        // Assert
        act.Should().NotThrow("ArrivalTime == LastBookingTime est la borne inclusive dans Modify (FR-1)"); // l:232 Equality >LastBookingTime→>=LastBookingTime
    }

    // ── FR-3 : borne de capacité dans Modify ──────────────────────────────

    [Fact]
    public void Should_NotThrow_When_NewGuestsCountEqualsCapacity()
    {
        // Arrange — table.Capacity = 4, guestsCount = 4
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table(capacity: 4, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 4, null, _clock);

        // Assert
        act.Should().NotThrow("guestsCount == Capacity est la borne inclusive dans Modify (FR-3)"); // l:240 Equality >Capacity→>=Capacity
    }

    // ── FR-4 : plafond de couverts dans Modify ────────────────────────────

    [Fact]
    public void Should_NotThrow_When_CoversExcludeCurrentBookingGuests()
    {
        // Arrange — booking.GuestsCount=3, existingServiceCovers=8, MaxCovers=10, newGuestsCount=2
        // coversWithoutThis = 8-3=5, 5+2=7 ≤ 10 → ok
        // Si bug (soustraction→addition) : 8+3=11, 11+2=13 > 10 → throw
        var booking = Builders.PendingBooking(guestsCount: 3);
        var service = Builders.DinnerService(maxCovers: 10);
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, null, _clock,
            existingServiceCovers: 8);

        // Assert
        act.Should().NotThrow("le booking courant doit être exclu du calcul par soustraction (FR-4)"); // l:245 Arithmetic -GuestsCount→+GuestsCount
    }

    [Fact]
    public void Should_NotThrow_When_NewTotalCoversEqualsMaxCoversOnModify()
    {
        // Arrange — booking.GuestsCount=2, existingServiceCovers=8, MaxCovers=10, newGuestsCount=4
        // coversWithoutThis = 8-2=6, 6+4=10 = MaxCovers → borne inclusive
        var booking = Builders.PendingBooking(guestsCount: 2);
        var service = Builders.DinnerService(maxCovers: 10);
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 4, null, _clock,
            existingServiceCovers: 8);

        // Assert
        act.Should().NotThrow("coversWithoutThis + newGuestsCount == MaxCovers est la borne inclusive dans Modify (FR-4)"); // l:246 Equality >MaxCovers→>=MaxCovers
    }

    [Fact]
    public void Should_Throw_ServiceFullyBookedException_When_NewGuestsExceedRemainingCapacity()
    {
        // Arrange — booking.GuestsCount=2, existingServiceCovers=8, MaxCovers=10, newGuestsCount=5
        // coversWithoutThis = 8-2=6, 6+5=11 > 10 → throw
        // Si bug (addition→soustraction) : 6-5=1, 1 > 10 est false → pas de throw
        var booking = Builders.PendingBooking(guestsCount: 2);
        var service = Builders.DinnerService(maxCovers: 10);
        var table = Builders.Table(capacity: 6, minCapacity: 2);
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        Action act = () => booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 5, null, _clock,
            existingServiceCovers: 8);

        // Assert
        act.Should().Throw<ServiceFullyBookedException>("6 couverts restants + 5 nouveaux = 11 > MaxCovers=10 (FR-4)") // l:246 Arithmetic +newGuestsCount→-newGuestsCount
           .Which.CurrentTotal.Should().Be(11, "CurrentTotal = coversWithoutThis(6) + newGuestsCount(5) (FR-4)"); // l:247 Arithmetic +→-
    }

    // ── FR-23 : flags informatifs dans Modify ─────────────────────────────

    [Fact]
    public void Should_NotSetAnyFlag_When_SpecialRequestsHasNoKeywordOnModify()
    {
        // Arrange — texte non nul sans aucun mot-clé (tue les 7 mutations keyword→"" dans Modify)
        var booking = Builders.PendingBooking();
        var service = Builders.DinnerService();
        var table = Builders.Table();
        var futureDate = DateOnly.FromDateTime(_clock.UtcNow.Date.AddDays(3));

        // Act
        booking.Modify(table, service, futureDate, new TimeOnly(19, 30), 2, "table au calme, vue sur jardin", _clock);

        // Assert
        booking.HasAllergyAlert.Should().BeFalse("aucun mot-clé allergie/intolérance présent (FR-23)"); // l:263
        booking.IsCelebration.Should().BeFalse("aucun mot-clé anniversaire/mariage/fiançailles présent (FR-23)"); // l:264
        booking.NeedsHighChair.Should().BeFalse("aucun mot-clé chaise bébé/siège enfant présent (FR-23)"); // l:265
    }
}
