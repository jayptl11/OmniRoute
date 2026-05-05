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
    /// <summary>Score at classification time (W_channel + W_need + W_history), excludes W_waittime.
    /// Used by SlaMonitoringService to recalculate score without accumulation each cycle.</summary>
    public int BasePriorityScore { get; private set; }
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
        BasePriorityScore = priorityScore; // freeze base — W_waittime not yet applied at classification
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

    // DP-04: Gán lead về cửa hàng cụ thể (điều phối thủ công)
    public void DispatchToStore(Guid storeId, DateTime slaDeadline)
    {
        if (Status != LeadStatus.PendingDispatch)
            throw new InvalidOperationException(
                $"Chỉ có thể gán cửa hàng cho lead đang ở trạng thái PendingDispatch. Trạng thái hiện tại: '{Status}'.");

        AssignedStoreId = storeId;
        AssignedAt = DateTime.UtcNow;
        SlaDeadline = slaDeadline;
        Status = LeadStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    // DP-04b: Sau khi dispatch về store, gán cụ thể cho SS ít việc nhất trong store đó
    public void AssignUserAfterDispatch(Guid userId)
    {
        AssignedUserId = userId;
        AssignedAt = DateTime.UtcNow;
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

    // SA-04: Chuyển trạng thái theo luồng Sale (BR-05: một chiều)
    // Các transition hợp lệ:
    //   Assigned    → Contacted | Cancelled
    //   Contacted   → InProgress | Cancelled
    //   InProgress  → Won | Lost | Cancelled
    private static readonly Dictionary<LeadStatus, HashSet<LeadStatus>> _allowedTransitions = new()
    {
        [LeadStatus.Assigned]   = [LeadStatus.Contacted, LeadStatus.Cancelled],
        [LeadStatus.Contacted]  = [LeadStatus.InProgress, LeadStatus.Cancelled],
        [LeadStatus.InProgress] = [LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled],
    };

    public void TransitionStatus(LeadStatus newStatus)
    {
        if (!_allowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Không thể chuyển từ trạng thái '{Status}' sang '{newStatus}'.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus is LeadStatus.Won or LeadStatus.Lost or LeadStatus.Cancelled)
            ClosedAt = DateTime.UtcNow;
    }
}
