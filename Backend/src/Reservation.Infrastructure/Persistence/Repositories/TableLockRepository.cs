using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class TableLockRepository(ReservationDbContext db) : ITableLockRepository
{
    public async Task<TableLock?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await db.TableLocks.FirstOrDefaultAsync(t => t.BookingId == bookingId, ct);

    public async Task AddAsync(TableLock tableLock, CancellationToken ct = default)
        => await db.TableLocks.AddAsync(tableLock, ct);

    public async Task RemoveAsync(TableLock tableLock, CancellationToken ct = default)
    {
        db.TableLocks.Remove(tableLock);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
