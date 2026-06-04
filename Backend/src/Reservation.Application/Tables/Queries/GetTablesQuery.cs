using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Tables.Queries;

public record GetTablesQuery : IRequest<IReadOnlyList<TableDto>>;

public sealed class GetTablesQueryHandler(ITableRepository tableRepo)
    : IRequestHandler<GetTablesQuery, IReadOnlyList<TableDto>>
{
    public async Task<IReadOnlyList<TableDto>> Handle(GetTablesQuery query, CancellationToken ct)
    {
        var tables = await tableRepo.GetAllAsync(ct);
        return tables.Select(t => t.ToDto()).ToList();
    }
}
