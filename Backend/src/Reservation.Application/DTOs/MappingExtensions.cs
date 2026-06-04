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
}
