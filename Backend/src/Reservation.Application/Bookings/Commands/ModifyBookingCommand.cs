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
    {
        var booking = await bookingRepo.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new EntityNotFoundException("Booking", cmd.BookingId);

        var table = cmd.TableId.HasValue
            ? await tableRepo.GetByIdAsync(cmd.TableId.Value, ct)
              ?? throw new EntityNotFoundException("Table", cmd.TableId.Value)
            : null;

        var service = await serviceRepo.GetByIdAsync(booking.ServiceId, ct)
            ?? throw new EntityNotFoundException("DiningService", booking.ServiceId);

        var existingCovers = await bookingRepo.GetExistingCoversAsync(booking.ServiceId, cmd.BookingDate, ct);
        var tableBookings = table is not null
            ? await bookingRepo.GetActiveTableBookingsAsync(table.Id, cmd.BookingDate, ct)
            : null;

        booking.Modify(table, service, cmd.BookingDate, cmd.ArrivalTime, cmd.GuestsCount,
            cmd.SpecialRequests, clock, existingCovers, tableBookings);

        await bookingRepo.SaveChangesAsync(ct);
        return booking.ToDto();
    }
}
