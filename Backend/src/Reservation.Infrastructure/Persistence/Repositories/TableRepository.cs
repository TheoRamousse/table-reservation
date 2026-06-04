using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(ReservationDbContext db) : ITableRepository
{
    public async Task<Table?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Tables.FindAsync([id], ct);

    public async Task<IEnumerable<Table>> GetAllActiveAsync(CancellationToken ct = default)
        => await db.Tables.Where(t => t.IsActive).OrderBy(t => t.Number).ToListAsync(ct);

    public async Task AddAsync(Table table, CancellationToken ct = default)
        => await db.Tables.AddAsync(table, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
