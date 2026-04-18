namespace OmniRoute.Domain.Entities;

public class Store
{
    public Guid Id { get; private set; }
    public string StoreCode { get; private set; } = string.Empty;
    public string StoreName { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Region { get; private set; }
    public Guid? ManagerId { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual User? Manager { get; set; }
    public virtual ICollection<Lead> Leads { get; set; } = [];

    private Store() { } // EF Core

    public static Store Create(
        string storeCode,
        string storeName,
        int maxCapacity,
        string? address = null,
        string? region = null,
        Guid? managerId = null)
    {
        return new Store
        {
            Id = Guid.NewGuid(),
            StoreCode = storeCode,
            StoreName = storeName,
            MaxCapacity = maxCapacity,
            Address = address,
            Region = region,
            ManagerId = managerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void Update(string storeName, int maxCapacity, string? address, string? region, Guid? managerId)
    {
        StoreName = storeName;
        MaxCapacity = maxCapacity;
        Address = address;
        Region = region;
        ManagerId = managerId;
    }
}
