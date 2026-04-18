using System.Text.Json;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Commands.CreateLead;

internal sealed class CreateLeadCommandHandler
    : ICommandHandler<CreateLeadCommand, CreateLeadResponse>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRoutingEngine _routingEngine;
    private readonly ICurrentUserService _currentUserService;

    public CreateLeadCommandHandler(
        ILeadRepository leadRepository,
        IActivityLogRepository activityLogRepository,
        IApplicationDbContext context,
        IRoutingEngine routingEngine,
        ICurrentUserService currentUserService)
    {
        _leadRepository = leadRepository;
        _activityLogRepository = activityLogRepository;
        _context = context;
        _routingEngine = routingEngine;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CreateLeadResponse>> Handle(CreateLeadCommand command, CancellationToken ct)
    {
        // Step 1: Duplicate detection
        var existingLead = await _leadRepository.GetByPhoneAsync(command.CustomerPhone, ct);
        if (existingLead is not null && !command.ForceCreate)
        {
            return Result<CreateLeadResponse>.Success(new CreateLeadResponse(
                Guid.Empty,
                string.Empty,
                IsDuplicate: true,
                ExistingLeadId: existingLead.Id,
                ExistingLeadCode: existingLead.LeadCode,
                ExistingLeadStatus: existingLead.Status.ToString()
            ));
        }

        var createdBy = _currentUserService.GetUserId();

        // Step 2: Generate lead code (LD-YYYYMMDD-XXXX)
        var leadCode = await GenerateLeadCodeAsync(ct);

        // Step 3: Create lead entity
        string? productInterestJson = command.ProductInterest is { Count: > 0 }
            ? JsonSerializer.Serialize(command.ProductInterest)
            : null;

        var lead = Lead.Create(
            leadCode: leadCode,
            customerName: command.CustomerName,
            customerPhone: command.CustomerPhone,
            channel: command.Channel,
            needDescription: command.NeedDescription,
            createdBy: createdBy,
            customerAddress: command.CustomerAddress,
            customerEmail: command.CustomerEmail,
            productInterest: productInterestJson
        );

        await _leadRepository.AddAsync(lead, ct);
        await _context.SaveChangesAsync(ct);

        // Step 4: Log creation
        var log = ActivityLog.Create(
            entityType: "LEAD",
            entityId: lead.Id,
            action: "LEAD_CREATED",
            performedBy: createdBy,
            newValue: JsonSerializer.Serialize(new { lead.LeadCode, lead.CustomerPhone, Channel = lead.Channel.ToString() })
        );
        await _activityLogRepository.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);

        // Step 5: Run routing engine inline (SYS-01 → SYS-03)
        await _routingEngine.ProcessAsync(lead.Id, ct);

        return Result<CreateLeadResponse>.Success(new CreateLeadResponse(
            LeadId: lead.Id,
            LeadCode: lead.LeadCode,
            IsDuplicate: false,
            ExistingLeadId: null,
            ExistingLeadCode: null,
            ExistingLeadStatus: null
        ));
    }

    private async Task<string> GenerateLeadCodeAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var datePrefix = today.ToString("yyyyMMdd");
        var prefix = $"LD-{datePrefix}-";

        var maxCode = await _leadRepository.GetMaxLeadCodeTodayAsync(prefix, ct);

        int nextSeq = 1;
        if (maxCode is not null)
        {
            var parts = maxCode.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int currentSeq))
                nextSeq = currentSeq + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }
}
