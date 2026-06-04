using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reservation.Infrastructure.Persistence;

// Utilisée uniquement par les outils EF Core (dotnet ef migrations)
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReservationDbContext>
{
    public ReservationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReservationDbContext>()
            .UseSqlite("Data Source=reservation.db")
            .Options;

        return new ReservationDbContext(options);
    }
}
