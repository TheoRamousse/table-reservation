using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(c => c.IsBlacklisted).HasColumnName("is_blacklisted");
        builder.Property(c => c.NoShowCount).HasColumnName("no_show_count");
        builder.Property(c => c.LateCancelCount).HasColumnName("late_cancel_count");
        builder.Property(c => c.VipLevel)
            .HasColumnName("vip_level")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");

        builder.HasIndex(c => c.Phone).HasDatabaseName("idx_customers_phone");
        builder.HasIndex(c => c.Email).HasDatabaseName("idx_customers_email");
    }
}
