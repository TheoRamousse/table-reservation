using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class ClosedDayConfiguration : IEntityTypeConfiguration<ClosedDay>
{
    public void Configure(EntityTypeBuilder<ClosedDay> builder)
    {
        builder.ToTable("closed_days");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.ClosedDate).HasColumnName("closed_date");
        builder.Property(c => c.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        builder.HasIndex(c => c.ClosedDate).IsUnique().HasDatabaseName("idx_closed_days_date");
    }
}
