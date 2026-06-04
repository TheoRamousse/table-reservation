using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface ITableLockRepository
{
    Task<TableLock?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task AddAsync(TableLock tableLock, CancellationToken ct = default);
    Task RemoveAsync(TableLock tableLock, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
