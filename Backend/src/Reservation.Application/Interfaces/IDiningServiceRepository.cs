using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface IDiningServiceRepository
{
    Task<DiningService?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<DiningService>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(DiningService service, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
