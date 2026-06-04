using Reservation.Domain.Entities;

namespace Reservation.Application.DTOs;

internal static class MappingExtensions
{
    internal static BookingDto ToDto(this Booking b) => new(
        b.Id, b.CustomerId, b.TableId, b.ServiceId,
        b.BookingDate, b.ArrivalTime, b.GuestsCount,
        b.Status, b.Source, b.SpecialRequests,
        b.HasAllergyAlert, b.IsCelebration, b.NeedsHighChair,
        b.LateCancel, b.CancellationReason, b.CreatedAt);

    internal static CustomerDto ToDto(this Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Phone, c.Email,
        c.IsBlacklisted, c.NoShowCount, c.LateCancelCount, c.VipLevel);

    internal static TableDto ToDto(this Table t) => new(
        t.Id, t.Number, t.Capacity, t.MinCapacity, t.Zone, t.IsActive, t.IsCombinable);

    internal static DiningServiceDto ToDto(this DiningService s) => new(
        s.Id, s.Name, s.StartTime, s.EndTime, s.LastBookingTime,
        s.DurationMinutes, s.MaxCovers, s.IsActive);

    internal static ClosedDayDto ToDto(this ClosedDay d) => new(
        d.Id, d.ClosedDate, d.Reason, d.CreatedAt);
}
