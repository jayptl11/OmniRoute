namespace OmniRoute.Application.Features.Leads.DTOs;

public record CreateLeadResponse(
    Guid LeadId,
    string LeadCode,
    bool IsDuplicate,
    Guid? ExistingLeadId,
    string? ExistingLeadCode,
    string? ExistingLeadStatus
);
