using FluentValidation;
using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;
using Reservation.Domain.Enums;
using Reservation.Domain.Interfaces;

namespace Reservation.Application.Bookings.Commands;

public record CreateBookingCommand(
    Guid CustomerId,
    Guid? TableId,
    Guid ServiceId,
    DateOnly BookingDate,
    TimeOnly ArrivalTime,
    int GuestsCount,
    BookingSource Source,
    string? SpecialRequests) : IRequest<BookingDto>;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.GuestsCount).GreaterThan(0);
        RuleFor(x => x.SpecialRequests).MaximumLength(500);
    }
}

public sealed class CreateBookingCommandHandler(
    IBookingRepository bookingRepo,
    ICustomerRepository customerRepo,
    ITableRepository tableRepo,
    IDiningServiceRepository serviceRepo,
    IClosedDayRepository closedDayRepo,
    IClock clock) : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.CustomerId, ct)
            ?? throw new EntityNotFoundException("Customer", cmd.CustomerId);

        var table = cmd.TableId.HasValue
            ? await tableRepo.GetByIdAsync(cmd.TableId.Value, ct)
              ?? throw new EntityNotFoundException("Table", cmd.TableId.Value)
            : null;

        var service = await serviceRepo.GetByIdAsync(cmd.ServiceId, ct)
            ?? throw new EntityNotFoundException("DiningService", cmd.ServiceId);

        var existingCovers = await bookingRepo.GetExistingCoversAsync(cmd.ServiceId, cmd.BookingDate, ct);

        var tableBookings = table is not null
            ? await bookingRepo.GetActiveTableBookingsAsync(table.Id, cmd.BookingDate, ct)
            : null;

        var closedDay = await closedDayRepo.GetByDateAsync(cmd.BookingDate, ct);

        var booking = Domain.Entities.Booking.Create(
            customer, table, service,
            cmd.BookingDate, cmd.ArrivalTime, cmd.GuestsCount,
            cmd.Source, cmd.SpecialRequests, clock,
            existingCovers, tableBookings,
            isClosedDay: closedDay is not null);

        await bookingRepo.AddAsync(booking, ct);
        await bookingRepo.SaveChangesAsync(ct);

        return booking.ToDto();
    }
}
