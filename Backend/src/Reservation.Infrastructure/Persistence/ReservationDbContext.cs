using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence;

public sealed class ReservationDbContext(DbContextOptions<ReservationDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<DiningService> DiningServices => Set<DiningService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationDbContext).Assembly);
    }
}
