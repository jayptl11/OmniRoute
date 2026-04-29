namespace OmniRoute.Application.Features.Leads.DTOs;

public record EscalateTargetDto(
    Guid UserId,
    string FullName,
    string RoleName);
