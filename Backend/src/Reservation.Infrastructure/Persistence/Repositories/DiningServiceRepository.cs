using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class DiningServiceRepository(ReservationDbContext db) : IDiningServiceRepository
{
    public async Task<DiningService?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.DiningServices.FindAsync([id], ct);

    public Task<IEnumerable<DiningService>> GetAllActiveAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(DiningService service, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}
