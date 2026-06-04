namespace Reservation.Domain.Entities;

public sealed class TableLock
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid PrimaryTableId { get; private set; }
    public Guid SecondaryTableId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TableLock() { }

    internal TableLock(Guid id, Guid bookingId, Guid primaryTableId, Guid secondaryTableId, DateTimeOffset createdAt)
    {
        Id = id;
        BookingId = bookingId;
        PrimaryTableId = primaryTableId;
        SecondaryTableId = secondaryTableId;
        CreatedAt = createdAt;
    }

    public static TableLock Create(Guid bookingId, Guid primaryTableId, Guid secondaryTableId, DateTimeOffset now)
        => new(Guid.NewGuid(), bookingId, primaryTableId, secondaryTableId, now);
}
