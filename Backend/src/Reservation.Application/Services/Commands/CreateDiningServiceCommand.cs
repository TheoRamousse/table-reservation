using FluentValidation;
using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Services.Commands;

public record CreateDiningServiceCommand(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeOnly LastBookingTime,
    int DurationMinutes,
    int MaxCovers) : IRequest<DiningServiceDto>;

public sealed class CreateDiningServiceCommandValidator : AbstractValidator<CreateDiningServiceCommand>
{
    public CreateDiningServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.MaxCovers).GreaterThan(0);
    }
}

public sealed class CreateDiningServiceCommandHandler(IDiningServiceRepository serviceRepo)
    : IRequestHandler<CreateDiningServiceCommand, DiningServiceDto>
{
    public async Task<DiningServiceDto> Handle(CreateDiningServiceCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
