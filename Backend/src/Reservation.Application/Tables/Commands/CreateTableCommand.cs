using FluentValidation;
using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;

namespace Reservation.Application.Tables.Commands;

public record CreateTableCommand(
    int Number,
    int Capacity,
    int MinCapacity,
    TableZone Zone,
    bool IsCombinable) : IRequest<TableDto>;

public sealed class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableCommandValidator()
    {
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.MinCapacity).GreaterThan(0);
        RuleFor(x => x.MinCapacity).LessThanOrEqualTo(x => x.Capacity);
    }
}

public sealed class CreateTableCommandHandler(ITableRepository tableRepo)
    : IRequestHandler<CreateTableCommand, TableDto>
{
    public async Task<TableDto> Handle(CreateTableCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
