using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reservation.Domain.Entities;

namespace Reservation.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.CustomerId).HasColumnName("customer_id");
        builder.Property(b => b.TableId).HasColumnName("table_id");
        builder.Property(b => b.ServiceId).HasColumnName("service_id");
        builder.Property(b => b.BookingDate).HasColumnName("booking_date");
        builder.Property(b => b.ArrivalTime).HasColumnName("arrival_time");
        builder.Property(b => b.GuestsCount).HasColumnName("guests_count");
        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(b => b.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(b => b.SpecialRequests).HasColumnName("special_requests").HasMaxLength(500);
        builder.Property(b => b.HasAllergyAlert).HasColumnName("has_allergy_alert");
        builder.Property(b => b.IsCelebration).HasColumnName("is_celebration");
        builder.Property(b => b.NeedsHighChair).HasColumnName("needs_high_chair");
        builder.Property(b => b.LateCancel).HasColumnName("late_cancel");
        builder.Property(b => b.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");

        builder.Property<DateTimeOffset>("UpdatedAt").HasColumnName("updated_at");
        builder.Property<DateTimeOffset?>("DeletedAt").HasColumnName("deleted_at");

        // Soft delete — les bookings supprimés sont filtrés par défaut
        builder.HasQueryFilter(b => EF.Property<DateTimeOffset?>(b, "DeletedAt") == null);

        builder.HasIndex(b => new { b.BookingDate, b.ServiceId }).HasDatabaseName("idx_bookings_date_service");
        builder.HasIndex(b => b.CustomerId).HasDatabaseName("idx_bookings_customer");
        builder.HasIndex(b => new { b.TableId, b.BookingDate }).HasDatabaseName("idx_bookings_table_date");
    }
}
