using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;
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
    {
        var closedDay = ClosedDay.Create(cmd.Date, cmd.Reason, clock);
        await closedDayRepo.AddAsync(closedDay, ct);

        var bookingsToCancel = (await bookingRepo.GetPendingAndConfirmedByDateAsync(cmd.Date, ct)).ToList();
        foreach (var booking in bookingsToCancel)
            booking.Cancel("RestaurantClosed", clock.UtcNow);

        await bookingRepo.SaveChangesAsync(ct);
        await closedDayRepo.SaveChangesAsync(ct);

        return new DeclareClosedDayResult(closedDay.ToDto(), bookingsToCancel.Count);
    }
}
