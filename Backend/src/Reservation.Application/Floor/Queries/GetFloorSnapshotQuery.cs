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
        => throw new NotImplementedException();
}
