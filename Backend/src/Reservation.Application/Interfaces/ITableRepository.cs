using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface ITableRepository
{
    Task<Table?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
