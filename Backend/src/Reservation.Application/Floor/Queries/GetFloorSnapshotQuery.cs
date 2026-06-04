using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;

namespace Reservation.Application.Floor.Queries;

public record GetFloorSnapshotQuery(DateOnly Date, Guid ServiceId) : IRequest<FloorSnapshotDto>;

public sealed class GetFloorSnapshotQueryHandler(
    IBookingRepository bookingRepo,
    ITableRepository tableRepo,
    IDiningServiceRepository serviceRepo,
    ICustomerRepository customerRepo) : IRequestHandler<GetFloorSnapshotQuery, FloorSnapshotDto>
{
    public async Task<FloorSnapshotDto> Handle(GetFloorSnapshotQuery query, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(query.ServiceId, ct)
            ?? throw new EntityNotFoundException("DiningService", query.ServiceId);

        var tables = (await tableRepo.GetAllActiveAsync(ct)).ToList();
        var bookings = (await bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, ct)).ToList();

        var activeBookings = bookings
            .Where(b => b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Completed
                     && b.Status != BookingStatus.Rejected)
            .ToList();

        var customerIds = activeBookings
            .Select(b => b.CustomerId)
            .Distinct();
        var customers = (await customerRepo.GetByIdsAsync(customerIds, ct))
            .ToDictionary(c => c.Id);

        var tableStates = tables.Select(t =>
        {
            var activeBooking = activeBookings.FirstOrDefault(b => b.TableId == t.Id);
            var status = DetermineTableStatus(activeBooking);
            ActiveBookingInfo? bookingInfo = null;
            if (activeBooking is not null && customers.TryGetValue(activeBooking.CustomerId, out var customer))
            {
                bookingInfo = new ActiveBookingInfo(
                    activeBooking.Id, activeBooking.CustomerId,
                    $"{customer.FirstName} {customer.LastName}",
                    activeBooking.GuestsCount, activeBooking.ArrivalTime,
                    activeBooking.HasAllergyAlert, activeBooking.IsCelebration,
                    activeBooking.NeedsHighChair);
            }
            return new TableStateDto(t.Id, t.Number, t.Capacity, t.MinCapacity, t.Zone,
                t.IsActive, t.IsCombinable, status, bookingInfo);
        }).ToList();

        var confirmedCovers = activeBookings
            .Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Seated)
            .Sum(b => b.GuestsCount);

        return new FloorSnapshotDto(query.Date, service.Id, service.Name, confirmedCovers,
            service.MaxCovers, tableStates);
    }

    private static TableStatus DetermineTableStatus(Domain.Entities.Booking? booking) =>
        booking?.Status switch
        {
            BookingStatus.Pending   => TableStatus.Pending,
            BookingStatus.Confirmed => TableStatus.Confirmed,
            BookingStatus.Seated    => TableStatus.Seated,
            BookingStatus.NoShow    => TableStatus.NoShow,
            _                       => TableStatus.Free,
        };
}
