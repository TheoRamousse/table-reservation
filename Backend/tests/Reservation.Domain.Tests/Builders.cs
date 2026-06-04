using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Domain.Tests;

/// <summary>
/// Builders internes pour créer des entités en état précis sans passer par la logique domaine.
/// Utilisés uniquement dans les tests unitaires.
/// </summary>
internal static class Builders
{
    internal static Customer Customer(
        bool isBlacklisted = false,
        int noShowCount = 0,
        int lateCancelCount = 0,
        VipLevel vipLevel = VipLevel.None,
        string firstName = "Jean",
        string lastName = "Dupont",
        string phone = "0600000000",
        string? email = null) =>
        new(Guid.NewGuid(), firstName, lastName, phone, email, isBlacklisted, noShowCount, lateCancelCount, vipLevel);

    internal static Table Table(
        int capacity = 6,
        int minCapacity = 2,
        TableZone zone = TableZone.Salle,
        bool isCombinable = false,
        bool isActive = true,
        int number = 1) =>
        new(Guid.NewGuid(), number, capacity, minCapacity, zone, isCombinable, isActive);

    internal static DiningService DinnerService(
        int maxCovers = 60,
        int durationMinutes = 120,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        TimeOnly? lastBookingTime = null) =>
        new(Guid.NewGuid(), "Dîner",
            startTime ?? new TimeOnly(19, 0),
            endTime ?? new TimeOnly(23, 0),
            lastBookingTime ?? new TimeOnly(21, 30),
            durationMinutes,
            maxCovers);

    internal static DiningService LunchService(
        int maxCovers = 60,
        int durationMinutes = 120) =>
        new(Guid.NewGuid(), "Déjeuner",
            new TimeOnly(12, 0),
            new TimeOnly(14, 30),
            new TimeOnly(13, 30),
            durationMinutes,
            maxCovers);

    internal static Booking PendingBooking(
        Customer? customer = null,
        Table? table = null,
        DiningService? service = null,
        DateOnly? bookingDate = null,
        TimeOnly? arrivalTime = null,
        int guestsCount = 2,
        BookingSource source = BookingSource.Online) =>
        BookingWith(
            customer, table, service,
            bookingDate, arrivalTime,
            guestsCount, source,
            BookingStatus.Pending);

    internal static Booking ConfirmedBooking(
        Customer? customer = null,
        Table? table = null,
        DiningService? service = null,
        DateOnly? bookingDate = null,
        TimeOnly? arrivalTime = null,
        int guestsCount = 2) =>
        BookingWith(
            customer, table, service,
            bookingDate, arrivalTime,
            guestsCount, BookingSource.Staff,
            BookingStatus.Confirmed);

    internal static Booking SeatedBooking(
        Customer? customer = null,
        Table? table = null,
        DiningService? service = null,
        DateOnly? bookingDate = null,
        TimeOnly? arrivalTime = null,
        int guestsCount = 2) =>
        BookingWith(
            customer, table, service,
            bookingDate, arrivalTime,
            guestsCount, BookingSource.Staff,
            BookingStatus.Seated);

    internal static Booking BookingInStatus(BookingStatus status, DateOnly? bookingDate = null, TimeOnly? arrivalTime = null) =>
        BookingWith(null, null, null, bookingDate, arrivalTime, 2, BookingSource.Staff, status);

    private static Booking BookingWith(
        Customer? customer, Table? table, DiningService? service,
        DateOnly? bookingDate, TimeOnly? arrivalTime,
        int guestsCount, BookingSource source, BookingStatus status)
    {
        var resolvedTable = table ?? Table();
        var resolvedService = service ?? DinnerService();
        return new Booking(
            id: Guid.NewGuid(),
            customerId: customer?.Id ?? Guid.NewGuid(),
            tableId: resolvedTable.Id,
            serviceId: resolvedService.Id,
            bookingDate: bookingDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            arrivalTime: arrivalTime ?? new TimeOnly(19, 30),
            guestsCount: guestsCount,
            status: status,
            source: source,
            specialRequests: null,
            hasAllergyAlert: false,
            isCelebration: false,
            needsHighChair: false,
            lateCancel: false,
            cancellationReason: null,
            createdAt: DateTimeOffset.UtcNow);
    }
}
