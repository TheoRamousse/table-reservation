using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class ClosedDayRepository(ReservationDbContext db) : IClosedDayRepository
{
    public async Task<ClosedDay?> GetByDateAsync(DateOnly date, CancellationToken ct = default)
        => await db.ClosedDays.FirstOrDefaultAsync(c => c.ClosedDate == date, ct);

    public async Task<IEnumerable<ClosedDay>> GetAllAsync(CancellationToken ct = default)
        => await db.ClosedDays.OrderBy(c => c.ClosedDate).ToListAsync(ct);

    public async Task AddAsync(ClosedDay closedDay, CancellationToken ct = default)
        => await db.ClosedDays.AddAsync(closedDay, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
