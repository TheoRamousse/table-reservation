using Microsoft.EntityFrameworkCore;
using Reservation.Application.Interfaces;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(ReservationDbContext db) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Customers.FindAsync([id], ct);

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await db.Customers.FirstOrDefaultAsync(c => c.Phone == phone, ct);

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await db.Customers.Where(c => idList.Contains(c.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
        => await db.Customers.AddAsync(customer, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
