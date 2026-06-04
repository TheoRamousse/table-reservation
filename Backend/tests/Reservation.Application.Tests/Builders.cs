using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tests;

internal static class Builders
{
    internal static Customer Customer(
        bool isBlacklisted = false,
        int noShowCount = 0,
        VipLevel vipLevel = VipLevel.None) =>
        new(Guid.NewGuid(), "Jean", "Dupont", "0600000000", null, isBlacklisted, noShowCount, 0, vipLevel);

    internal static Table Table(
        int capacity = 6,
        int minCapacity = 2,
        bool isCombinable = false,
        bool isActive = true,
        int number = 1) =>
        new(Guid.NewGuid(), number, capacity, minCapacity, TableZone.Salle, isCombinable, isActive);

    internal static DiningService DinnerService(
        Guid? id = null,
        int maxCovers = 60,
        int durationMinutes = 120) =>
        new(id ?? Guid.NewGuid(), "Dîner",
            new TimeOnly(19, 0), new TimeOnly(23, 0),
            new TimeOnly(21, 30), durationMinutes, maxCovers);

    internal static Booking PendingBooking(
        Guid? id = null,
        Guid? customerId = null,
        Guid? tableId = null,
        Guid? serviceId = null,
        DateOnly? bookingDate = null) =>
        new(id ?? Guid.NewGuid(),
            customerId ?? Guid.NewGuid(),
            tableId,
            serviceId ?? Guid.NewGuid(),
            bookingDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, BookingStatus.Pending, BookingSource.Online,
            null, false, false, false, false, null,
            DateTimeOffset.UtcNow);

    internal static Booking ConfirmedBooking(
        Guid? customerId = null,
        Guid? tableId = null,
        Guid? serviceId = null,
        DateOnly? bookingDate = null) =>
        new(Guid.NewGuid(),
            customerId ?? Guid.NewGuid(),
            tableId,
            serviceId ?? Guid.NewGuid(),
            bookingDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            new TimeOnly(19, 30),
            2, BookingStatus.Confirmed, BookingSource.Staff,
            null, false, false, false, false, null,
            DateTimeOffset.UtcNow);

    internal static ClosedDay ClosedDay(DateOnly? date = null, string reason = "Travaux") =>
        new(Guid.NewGuid(), date ?? DateOnly.FromDateTime(DateTime.Today.AddDays(5)), reason, DateTimeOffset.UtcNow);
}
