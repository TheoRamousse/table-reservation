using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class DiningServiceRepository(ReservationDbContext db) : IDiningServiceRepository
{
    public async Task<DiningService?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.DiningServices.FindAsync([id], ct);

    public async Task<IEnumerable<DiningService>> GetAllActiveAsync(CancellationToken ct = default)
        => await db.DiningServices.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(ct);

    public async Task AddAsync(DiningService service, CancellationToken ct = default)
        => await db.DiningServices.AddAsync(service, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
