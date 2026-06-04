using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Services.Queries;

public record GetServicesQuery : IRequest<IReadOnlyList<DiningServiceDto>>;

public sealed class GetServicesQueryHandler(IDiningServiceRepository serviceRepo)
    : IRequestHandler<GetServicesQuery, IReadOnlyList<DiningServiceDto>>
{
    public async Task<IReadOnlyList<DiningServiceDto>> Handle(GetServicesQuery query, CancellationToken ct)
        => throw new NotImplementedException();
}
