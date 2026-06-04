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

    private int _lateCancelCount;

    public static Customer Create(string firstName, string lastName, string phone, string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        return new Customer(Guid.NewGuid(), firstName, lastName, phone, email,
            isBlacklisted: false, noShowCount: 0, vipLevel: VipLevel.None);
    }

    public void RegisterNoShow()
    {
        NoShowCount++;
        if (NoShowCount >= 3)
            IsBlacklisted = true;
    }

    public void RegisterLateCancel()
    {
        _lateCancelCount++;
        if (_lateCancelCount >= 2)
            VipLevel = VipLevel.None;
    }

    public void EnsureCanBook()
    {
        if (IsBlacklisted)
            throw new CustomerBlacklistedException(Id);
    }

    public void Blacklist() => IsBlacklisted = true;

    public void LiftBlacklist() => IsBlacklisted = false;
}
