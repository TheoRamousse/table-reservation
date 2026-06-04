using MediatR;
using Reservation.Application.DTOs;
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
        => throw new NotImplementedException();
}
