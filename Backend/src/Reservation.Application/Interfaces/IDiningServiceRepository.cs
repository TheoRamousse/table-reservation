using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface IDiningServiceRepository
{
    Task<DiningService?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
