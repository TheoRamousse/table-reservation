using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Bookings.Commands;

public record ChangeBookingStatusCommand(Guid BookingId, BookingStatus NewStatus) : IRequest<BookingDto>;

public sealed class ChangeBookingStatusCommandHandler(
    IBookingRepository bookingRepo,
    ICustomerRepository customerRepo,
    IClock clock) : IRequestHandler<ChangeBookingStatusCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ChangeBookingStatusCommand cmd, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new EntityNotFoundException("Booking", cmd.BookingId);

        booking.TransitionTo(cmd.NewStatus, clock.UtcNow);

        // RB-007 + RB-008 : no-show → incrémente le compteur client, blacklist auto au 3e
        if (cmd.NewStatus == BookingStatus.NoShow)
        {
            var customer = await customerRepo.GetByIdAsync(booking.CustomerId, ct)
                ?? throw new EntityNotFoundException("Customer", booking.CustomerId);
            customer.RegisterNoShow();
            await customerRepo.SaveChangesAsync(ct);
        }

        await bookingRepo.SaveChangesAsync(ct);
        return booking.ToDto();
    }
}
