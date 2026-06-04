using Reservation.Domain.Enums;
using Reservation.Domain.Exceptions;

namespace Reservation.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public bool IsBlacklisted { get; private set; }
    public int NoShowCount { get; private set; }
    public VipLevel VipLevel { get; private set; }

    private Customer() { }

    internal Customer(Guid id, string firstName, string lastName, string phone,
        string? email, bool isBlacklisted, int noShowCount, VipLevel vipLevel)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        IsBlacklisted = isBlacklisted;
        NoShowCount = noShowCount;
        VipLevel = vipLevel;
    }

    public static Customer Create(string firstName, string lastName, string phone, string? email = null)
        => throw new NotImplementedException();

    public void RegisterNoShow() => throw new NotImplementedException();

    public void RegisterLateCancel() => throw new NotImplementedException();

    public void EnsureCanBook() => throw new NotImplementedException();

    public void Blacklist() => throw new NotImplementedException();

    public void LiftBlacklist() => throw new NotImplementedException();
}
