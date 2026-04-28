using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.CheckDuplicate;

internal sealed class CheckDuplicateQueryHandler
    : IQueryHandler<CheckDuplicateQuery, DuplicateCheckDto>
{
    private readonly IApplicationDbContext _db;

    public CheckDuplicateQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DuplicateCheckDto>> Handle(
        CheckDuplicateQuery query,
        CancellationToken ct)
    {
        var existing = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CustomerPhone == query.Phone)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.LeadCode,
                l.Status,
                l.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            return Result<DuplicateCheckDto>.Success(
                new DuplicateCheckDto(false, null, null, null, null));
        }

        return Result<DuplicateCheckDto>.Success(new DuplicateCheckDto(
            HasDuplicate: true,
            ExistingLeadId: existing.Id,
            ExistingLeadCode: existing.LeadCode,
            ExistingLeadStatus: existing.Status.ToString(),
            ExistingLeadCreatedAt: existing.CreatedAt));
    }
}
