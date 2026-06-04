using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence;

public sealed class ReservationDbContext(DbContextOptions<ReservationDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<DiningService> DiningServices => Set<DiningService>();
    public DbSet<ClosedDay> ClosedDays => Set<ClosedDay>();
    public DbSet<TableLock> TableLocks => Set<TableLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationDbContext).Assembly);

        // SQLite stores GUIDs as TEXT via the seed migrations — force string conversion
        // so EF Core's WHERE parameters match the stored format.
        var guidConverter = new GuidToStringConverter();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entityType.GetProperties())
                if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                    property.SetValueConverter(guidConverter);
    }
}
