using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class Lead
{
    public Guid Id { get; private set; }
    public string LeadCode { get; private set; } = string.Empty;

    // Customer info
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public string? CustomerAddress { get; private set; }
    public string? CustomerEmail { get; private set; }

    // Source & need
    public Channel Channel { get; private set; }
    public NeedType? NeedType { get; private set; }
    public string NeedDescription { get; private set; } = string.Empty;
    public string? ProductInterest { get; private set; } // JSON array stored as string

    // Scoring & priority
    public int PriorityScore { get; private set; }
    public PriorityLevel? PriorityLevel { get; private set; }
    public RoutingType RoutingType { get; private set; }

    // Assignment
    public AssignedGroup? AssignedGroup { get; private set; }
    public Guid? AssignedStoreId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public DateTime? AssignedAt { get; private set; }

    // Status & SLA
    public LeadStatus Status { get; private set; }
    public DateTime? SlaDeadline { get; private set; }
    public bool SlaViolated { get; private set; }
    public DateTime? SlaWarningSentAt { get; private set; }

    // Metadata
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Navigation
    public virtual User? CreatedByUser { get; set; }
    public virtual User? AssignedUser { get; set; }
    public virtual Store? AssignedStore { get; set; }

    private Lead() { } // EF Core

    public static Lead Create(
        string leadCode,
        string customerName,
        string customerPhone,
        Channel channel,
        string needDescription,
        Guid createdBy,
        string? customerAddress = null,
        string? customerEmail = null,
        string? productInterest = null)
    {
        var now = DateTime.UtcNow;
        return new Lead
        {
            Id = Guid.NewGuid(),
            LeadCode = leadCode,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Channel = channel,
            NeedDescription = needDescription,
            CustomerAddress = customerAddress,
            CustomerEmail = customerEmail,
            ProductInterest = productInterest,
            Status = LeadStatus.New,
            RoutingType = RoutingType.Auto,
            SlaViolated = false,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SetClassification(NeedType needType, int priorityScore, Enums.PriorityLevel priorityLevel, AssignedGroup assignedGroup)
    {
        NeedType = needType;
        PriorityScore = priorityScore;
        PriorityLevel = priorityLevel;
        AssignedGroup = assignedGroup;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToUser(Guid userId, DateTime slaDeadline)
    {
        AssignedUserId = userId;
        AssignedAt = DateTime.UtcNow;
        SlaDeadline = slaDeadline;
        Status = LeadStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingDispatch()
    {
        Status = LeadStatus.PendingDispatch;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingAssignment()
    {
        Status = LeadStatus.PendingAssignment;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSlaViolated()
    {
        SlaViolated = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSlaWarningSent()
    {
        SlaWarningSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriorityScore(int newScore, Enums.PriorityLevel newLevel)
    {
        PriorityScore = newScore;
        PriorityLevel = newLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string? customerAddress, string? customerEmail, string? productInterest, string? needDescription)
    {
        if (customerAddress is not null) CustomerAddress = customerAddress;
        if (customerEmail is not null) CustomerEmail = customerEmail;
        if (productInterest is not null) ProductInterest = productInterest;
        if (needDescription is not null) NeedDescription = needDescription;
        UpdatedAt = DateTime.UtcNow;
    }
}
