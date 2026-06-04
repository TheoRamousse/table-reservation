using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(ReservationDbContext db) : ITableRepository
{
    public async Task<Table?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Tables.FindAsync([id], ct);
}
