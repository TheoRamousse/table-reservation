using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Bookings.Queries;

public record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDto>;

public sealed class GetBookingByIdQueryHandler(IBookingRepository bookingRepo)
    : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    public async Task<BookingDto> Handle(GetBookingByIdQuery query, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdAsync(query.BookingId, ct)
            ?? throw new EntityNotFoundException("Booking", query.BookingId);
        return booking.ToDto();
    }
}
