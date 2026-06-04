using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tables.Queries;

public record GetAvailabilityQuery(
    DateOnly Date,
    Guid ServiceId,
    int GuestsCount,
    TableZone? Zone) : IRequest<AvailabilityResultDto>;

public sealed class GetAvailabilityQueryHandler(
    IBookingRepository bookingRepo,
    ITableRepository tableRepo,
    IDiningServiceRepository serviceRepo) : IRequestHandler<GetAvailabilityQuery, AvailabilityResultDto>
{
    public async Task<AvailabilityResultDto> Handle(GetAvailabilityQuery query, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(query.ServiceId, ct)
            ?? throw new EntityNotFoundException("DiningService", query.ServiceId);

        var existingCovers = await bookingRepo.GetExistingCoversAsync(query.ServiceId, query.Date, ct);
        if (existingCovers >= service.MaxCovers)
            return new AvailabilityResultDto([], "ServiceFullyBooked");

        var allTables = (await tableRepo.GetAllActiveAsync(ct)).ToList();

        var activeBookings = await bookingRepo.GetByDateAndServiceAsync(query.Date, query.ServiceId, ct);
        var occupiedTableIds = activeBookings
            .Where(b => b.TableId.HasValue
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Completed
                     && b.Status != BookingStatus.Rejected)
            .Select(b => b.TableId!.Value)
            .ToHashSet();

        var available = allTables
            .Where(t => t.Capacity >= query.GuestsCount
                     && t.MinCapacity <= query.GuestsCount
                     && !occupiedTableIds.Contains(t.Id)
                     && (query.Zone is null || t.Zone == query.Zone))
            .OrderBy(t => t.Capacity)
            .Select(t => new AvailableTableDto(t.Id, t.Number, t.Capacity, t.MinCapacity, t.Zone, t.IsCombinable))
            .ToList();

        return new AvailabilityResultDto(available, null);
    }
}
