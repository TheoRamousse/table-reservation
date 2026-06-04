using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Bookings.Commands;

public record ModifyBookingCommand(
    Guid BookingId,
    DateOnly BookingDate,
    TimeOnly ArrivalTime,
    int GuestsCount,
    string? SpecialRequests,
    Guid? TableId) : IRequest<BookingDto>;

public sealed class ModifyBookingCommandHandler(
    IBookingRepository bookingRepo,
    ITableRepository tableRepo,
    IDiningServiceRepository serviceRepo,
    IClock clock) : IRequestHandler<ModifyBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ModifyBookingCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
