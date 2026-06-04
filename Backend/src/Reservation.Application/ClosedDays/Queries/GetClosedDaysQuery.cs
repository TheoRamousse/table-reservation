using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.ClosedDays.Queries;

public record GetClosedDaysQuery : IRequest<IReadOnlyList<ClosedDayDto>>;

public sealed class GetClosedDaysQueryHandler(IClosedDayRepository closedDayRepo)
    : IRequestHandler<GetClosedDaysQuery, IReadOnlyList<ClosedDayDto>>
{
    public async Task<IReadOnlyList<ClosedDayDto>> Handle(GetClosedDaysQuery query, CancellationToken ct)
        => throw new NotImplementedException();
}
