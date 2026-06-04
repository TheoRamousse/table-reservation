using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Bookings.Commands;

public record CancelBookingCommand(Guid BookingId, string Reason) : IRequest<BookingDto>;

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepo,
    ICustomerRepository customerRepo,
    IClock clock) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CancelBookingCommand cmd, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new EntityNotFoundException("Booking", cmd.BookingId);

        booking.Cancel(cmd.Reason, clock.UtcNow);

        if (booking.LateCancel)
        {
            var customer = await customerRepo.GetByIdAsync(booking.CustomerId, ct)
                ?? throw new EntityNotFoundException("Customer", booking.CustomerId);
            customer.RegisterLateCancel();
            await customerRepo.SaveChangesAsync(ct);
        }

        await bookingRepo.SaveChangesAsync(ct);
        return booking.ToDto();
    }
}
