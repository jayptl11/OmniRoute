using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class MasterDataItem
{
    public Guid Id { get; private set; }
    public MasterDataCategory Category { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MasterDataItem() { } // EF Core

    public static MasterDataItem Create(
        MasterDataCategory category,
        string code,
        string displayName,
        string? description = null,
        int sortOrder = 0)
    {
        return new MasterDataItem
        {
            Id = Guid.NewGuid(),
            Category = category,
            Code = code,
            DisplayName = displayName,
            Description = description,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string displayName, string? description, int sortOrder)
    {
        DisplayName = displayName;
        Description = description;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
