using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(ReservationDbContext db) : ITableRepository
{
    public async Task<Table?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Tables.FindAsync([id], ct);

    public Task<IEnumerable<Table>> GetAllActiveAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(Table table, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}
