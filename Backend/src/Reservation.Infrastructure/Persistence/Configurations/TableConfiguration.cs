using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("tables");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Number).HasColumnName("number");
        builder.Property(t => t.Capacity).HasColumnName("capacity");
        builder.Property(t => t.MinCapacity).HasColumnName("min_capacity");
        builder.Property(t => t.Zone)
            .HasColumnName("zone")
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(t => t.IsCombinable).HasColumnName("is_combinable");
        builder.Property(t => t.IsActive).HasColumnName("is_active");

        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");

        builder.HasIndex(t => t.Number).IsUnique().HasDatabaseName("idx_tables_number");
    }
}
