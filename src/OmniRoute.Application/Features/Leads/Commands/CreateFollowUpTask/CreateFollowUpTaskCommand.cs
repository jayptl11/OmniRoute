using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Commands.CreateFollowUpTask;

public record CreateFollowUpTaskCommand(
    Guid LeadId,
    DateTime DueAt,
    string Note) : ICommand<CreateFollowUpTaskResponse>;
