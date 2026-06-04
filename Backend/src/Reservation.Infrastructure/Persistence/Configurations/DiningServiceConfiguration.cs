using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class DiningServiceConfiguration : IEntityTypeConfiguration<DiningService>
{
    public void Configure(EntityTypeBuilder<DiningService> builder)
    {
        builder.ToTable("dining_services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.StartTime).HasColumnName("start_time");
        builder.Property(s => s.EndTime).HasColumnName("end_time");
        builder.Property(s => s.LastBookingTime).HasColumnName("last_booking_time");
        builder.Property(s => s.DurationMinutes).HasColumnName("duration_minutes");
        builder.Property(s => s.MaxCovers).HasColumnName("max_covers");
        builder.Property(s => s.IsActive).HasColumnName("is_active");

        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        builder.Property<DateTimeOffset>("CreatedAt").HasColumnName("created_at");
    }
}
