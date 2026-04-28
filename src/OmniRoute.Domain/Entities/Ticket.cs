using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Entities;

public class Ticket
{
    public Guid Id { get; private set; }
    public string TicketCode { get; private set; } = string.Empty;

    // Customer info
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public string? CustomerAddress { get; private set; }
    public string? CustomerEmail { get; private set; }

    // Source & need
    public Channel Channel { get; private set; }
    public NeedType? NeedType { get; private set; }
    public string NeedDescription { get; private set; } = string.Empty;

    // Scoring & priority
    public int PriorityScore { get; private set; }
    public PriorityLevel? PriorityLevel { get; private set; }

    // Assignment
    public Guid? AssignedUserId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public Guid? AssignedStoreId { get; private set; }

    // Status & SLA
    public TicketStatus Status { get; private set; }
    public DateTime? SlaDeadline { get; private set; }
    public bool SlaViolated { get; private set; }
    public DateTime? SlaWarningSentAt { get; private set; }

    // Escalation
    public Guid? EscalatedTo { get; private set; }
    public DateTime? EscalatedAt { get; private set; }
    public string? EscalatedReason { get; private set; }

    // Satisfaction (CS-07)
    public int? SatisfactionScore { get; private set; }
    public string? SatisfactionNote { get; private set; }

    // Optional link to originating Lead
    public Guid? LeadId { get; private set; }

    // Metadata
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Navigation
    public virtual User? CreatedByUser { get; set; }
    public virtual User? AssignedUser { get; set; }
    public virtual Store? AssignedStore { get; set; }

    private Ticket() { } // EF Core

    public static Ticket Create(
        string ticketCode,
        string customerName,
        string customerPhone,
        Channel channel,
        string needDescription,
        Guid createdBy,
        string? customerAddress = null,
        string? customerEmail = null,
        Guid? leadId = null)
    {
        var now = DateTime.UtcNow;
        return new Ticket
        {
            Id = Guid.NewGuid(),
            TicketCode = ticketCode,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Channel = channel,
            NeedDescription = needDescription,
            CustomerAddress = customerAddress,
            CustomerEmail = customerEmail,
            LeadId = leadId,
            Status = TicketStatus.New,
            SlaViolated = false,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AssignToUser(Guid userId, DateTime slaDeadline)
    {
        AssignedUserId = userId;
        AssignedAt = DateTime.UtcNow;
        SlaDeadline = slaDeadline;
        Status = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetClassification(NeedType needType, int priorityScore, Enums.PriorityLevel priorityLevel)
    {
        NeedType = needType;
        PriorityScore = priorityScore;
        PriorityLevel = priorityLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    // CS-04: Chuyển trạng thái theo luồng CSKH (BR-05 — cơ bản một chiều, ngoại trừ WaitingCustomer ↔ InProgress)
    // Transitions:
    //   New              → InProgress
    //   InProgress       → WaitingCustomer | Escalated | Resolved
    //   WaitingCustomer  → InProgress | Resolved
    //   Escalated        → Resolved
    //   Resolved         → Closed
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> _allowedTransitions = new()
    {
        [TicketStatus.New]             = [TicketStatus.InProgress],
        [TicketStatus.InProgress]      = [TicketStatus.WaitingCustomer, TicketStatus.Escalated, TicketStatus.Resolved],
        [TicketStatus.WaitingCustomer] = [TicketStatus.InProgress, TicketStatus.Resolved],
        [TicketStatus.Escalated]       = [TicketStatus.Resolved],
        [TicketStatus.Resolved]        = [TicketStatus.Closed],
    };

    public void TransitionStatus(TicketStatus newStatus)
    {
        if (!_allowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Không thể chuyển từ trạng thái '{Status}' sang '{newStatus}'.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus is TicketStatus.Closed)
            ClosedAt = DateTime.UtcNow;
    }

    // CS-06: Escalate ticket vượt thẩm quyền
    public void Escalate(Guid escalatedTo, string reason)
    {
        if (!_allowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(TicketStatus.Escalated))
            throw new InvalidOperationException(
                $"Không thể escalate ticket đang ở trạng thái '{Status}'.");

        EscalatedTo = escalatedTo;
        EscalatedAt = DateTime.UtcNow;
        EscalatedReason = reason;
        Status = TicketStatus.Escalated;
        UpdatedAt = DateTime.UtcNow;
    }

    // CS-07: Ghi nhận mức độ hài lòng (score 1–5, chỉ khi Resolved hoặc Closed)
    public void RecordSatisfaction(int score, string? note)
    {
        if (Status is not TicketStatus.Resolved and not TicketStatus.Closed)
            throw new InvalidOperationException(
                "Chỉ có thể ghi nhận mức độ hài lòng khi ticket ở trạng thái Resolved hoặc Closed.");

        if (score < 1 || score > 5)
            throw new ArgumentOutOfRangeException(nameof(score), "Điểm hài lòng phải từ 1 đến 5.");

        SatisfactionScore = score;
        SatisfactionNote = note;
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
}
