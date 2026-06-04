using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Bookings.Queries;

public record GetBookingsByCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<BookingDto>>;

public sealed class GetBookingsByCustomerQueryHandler(IBookingRepository bookingRepo)
    : IRequestHandler<GetBookingsByCustomerQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(GetBookingsByCustomerQuery query, CancellationToken ct)
    {
        var bookings = await bookingRepo.GetByCustomerIdAsync(query.CustomerId, ct);
        return bookings.Select(b => b.ToDto()).ToList();
    }
}
