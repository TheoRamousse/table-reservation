using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Reservation.Domain.Interfaces;

namespace Reservation.Infrastructure.Persistence.Interceptors;

public sealed class UpdatedAtInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // Ne remplace CreatedAt que si non déjà valorisé par le domaine (shadow property)
                if (entry.Metadata.FindProperty("CreatedAt") is not null)
                {
                    var current = (DateTimeOffset)entry.Property("CreatedAt").CurrentValue!;
                    if (current == default)
                        entry.Property("CreatedAt").CurrentValue = now;
                }
                if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                    entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Metadata.FindProperty("UpdatedAt") is not null)
                    entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
