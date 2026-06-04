using Reservation.Domain.Entities;

namespace Reservation.Application.Interfaces;

public interface IClosedDayRepository
{
    Task<ClosedDay?> GetByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<IEnumerable<ClosedDay>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(ClosedDay closedDay, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
