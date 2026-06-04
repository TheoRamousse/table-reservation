using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class TableLockConfiguration : IEntityTypeConfiguration<TableLock>
{
    public void Configure(EntityTypeBuilder<TableLock> builder)
    {
        builder.ToTable("table_locks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.BookingId).HasColumnName("booking_id");
        builder.Property(t => t.PrimaryTableId).HasColumnName("primary_table_id");
        builder.Property(t => t.SecondaryTableId).HasColumnName("secondary_table_id");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        builder.HasIndex(t => t.BookingId).HasDatabaseName("idx_table_locks_booking");
    }
}
