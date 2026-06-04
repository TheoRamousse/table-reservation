using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class DiningServiceRepository(ReservationDbContext db) : IDiningServiceRepository
{
    public async Task<DiningService?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.DiningServices.FindAsync([id], ct);
}
