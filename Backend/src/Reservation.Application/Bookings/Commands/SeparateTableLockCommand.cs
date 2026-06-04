using MediatR;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Application.Bookings.Commands;

public record SeparateTableLockCommand(Guid BookingId) : IRequest<Unit>;

public sealed class SeparateTableLockCommandHandler(
    IBookingRepository bookingRepo,
    ITableLockRepository tableLockRepo) : IRequestHandler<SeparateTableLockCommand, Unit>
{
    public async Task<Unit> Handle(SeparateTableLockCommand cmd, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new EntityNotFoundException("Booking", cmd.BookingId);

        if (booking.Status is not BookingStatus.Pending and not BookingStatus.Confirmed)
            throw new InvalidStatusTransitionException(booking.Status, BookingStatus.Cancelled);

        var tableLock = await tableLockRepo.GetByBookingIdAsync(cmd.BookingId, ct)
            ?? throw new EntityNotFoundException("TableLock", cmd.BookingId);

        await tableLockRepo.RemoveAsync(tableLock, ct);
        await tableLockRepo.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
