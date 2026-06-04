using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(ReservationDbContext db) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Bookings.FindAsync([id], ct);

    public async Task AddAsync(Booking booking, CancellationToken ct = default)
        => await db.Bookings.AddAsync(booking, ct);

    public async Task<int> GetExistingCoversAsync(Guid serviceId, DateOnly date, CancellationToken ct = default)
        => await db.Bookings
            .Where(b => b.ServiceId == serviceId
                     && b.BookingDate == date
                     && (b.Status == BookingStatus.Pending
                      || b.Status == BookingStatus.Confirmed
                      || b.Status == BookingStatus.Seated))
            .SumAsync(b => b.GuestsCount, ct);

    public async Task<IEnumerable<Booking>> GetActiveTableBookingsAsync(
        Guid tableId, DateOnly date, CancellationToken ct = default)
        => await db.Bookings
            .Where(b => b.TableId == tableId
                     && b.BookingDate == date
                     && b.Status != BookingStatus.Cancelled
                     && b.Status != BookingStatus.Completed
                     && b.Status != BookingStatus.NoShow
                     && b.Status != BookingStatus.Rejected)
            .ToListAsync(ct);

    public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await db.Bookings
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(ct);

    public Task<IEnumerable<Booking>> GetByDateAndServiceAsync(DateOnly date, Guid serviceId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IEnumerable<Booking>> GetPendingAndConfirmedByDateAsync(DateOnly date, CancellationToken ct = default)
        => throw new NotImplementedException();

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
