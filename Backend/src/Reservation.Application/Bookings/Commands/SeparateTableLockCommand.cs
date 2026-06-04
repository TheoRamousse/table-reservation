using MediatR;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;

namespace Reservation.Application.Bookings.Commands;

public record SeparateTableLockCommand(Guid BookingId) : IRequest<Unit>;

public sealed class SeparateTableLockCommandHandler(
    IBookingRepository bookingRepo,
    ITableLockRepository tableLockRepo) : IRequestHandler<SeparateTableLockCommand, Unit>
{
    public async Task<Unit> Handle(SeparateTableLockCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
