using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tables.Commands;

public record UpdateTableCommand(
    Guid TableId,
    int Capacity,
    int MinCapacity,
    TableZone Zone,
    bool IsCombinable,
    bool IsActive) : IRequest<TableDto>;

public sealed class UpdateTableCommandHandler(ITableRepository tableRepo)
    : IRequestHandler<UpdateTableCommand, TableDto>
{
    public async Task<TableDto> Handle(UpdateTableCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
