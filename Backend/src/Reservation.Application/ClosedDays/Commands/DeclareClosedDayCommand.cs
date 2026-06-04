using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.ClosedDays.Commands;

public record DeclareClosedDayCommand(DateOnly Date, string Reason) : IRequest<DeclareClosedDayResult>;

public record DeclareClosedDayResult(ClosedDayDto ClosedDay, int CancelledBookingsCount);

public sealed class DeclareClosedDayCommandHandler(
    IClosedDayRepository closedDayRepo,
    IBookingRepository bookingRepo,
    IClock clock) : IRequestHandler<DeclareClosedDayCommand, DeclareClosedDayResult>
{
    public async Task<DeclareClosedDayResult> Handle(DeclareClosedDayCommand cmd, CancellationToken ct)
        => throw new NotImplementedException();
}
