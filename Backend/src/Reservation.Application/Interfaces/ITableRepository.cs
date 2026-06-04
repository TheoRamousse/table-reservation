using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface ITableRepository
{
    Task<Table?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Table>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<Table>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Table table, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
