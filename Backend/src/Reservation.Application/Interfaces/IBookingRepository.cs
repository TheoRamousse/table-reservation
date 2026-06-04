using Reservation.Domain.Entities;
using Reservation.Domain.Enums;

namespace Reservation.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>Somme des couverts Pending+Confirmed+Seated pour un service et une date.</summary>
    Task<int> GetExistingCoversAsync(Guid serviceId, DateOnly date, CancellationToken ct = default);

    /// <summary>Réservations actives (hors Cancelled/Completed/NoShow) sur une table à une date.</summary>
    Task<IEnumerable<Booking>> GetActiveTableBookingsAsync(Guid tableId, DateOnly date, CancellationToken ct = default);

    Task<IEnumerable<Booking>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByDateAndServiceAsync(DateOnly date, Guid serviceId, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetPendingAndConfirmedByDateAsync(DateOnly date, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
