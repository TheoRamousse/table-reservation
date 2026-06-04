using Reservation.Domain.Enums;

namespace Reservation.Domain.Entities;

public sealed class Table
{
    public Guid Id { get; private set; }
    public int Number { get; private set; }
    public int Capacity { get; private set; }
    public int MinCapacity { get; private set; }
    public TableZone Zone { get; private set; }
    public bool IsCombinable { get; private set; }
    public bool IsActive { get; private set; }

    private Table() { }

    internal Table(Guid id, int number, int capacity, int minCapacity, TableZone zone, bool isCombinable, bool isActive)
    {
        Id = id;
        Number = number;
        Capacity = capacity;
        MinCapacity = minCapacity;
        Zone = zone;
        IsCombinable = isCombinable;
        IsActive = isActive;
    }

    public static Table Create(int number, int capacity, int minCapacity, TableZone zone, bool isCombinable = false)
        => throw new NotImplementedException();
}
