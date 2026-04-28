using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeads;

internal sealed class GetPendingDispatchLeadsQueryHandler
    : IQueryHandler<GetPendingDispatchLeadsQuery, GetPendingDispatchLeadsResult>
{
    private readonly ILeadRepository _leadRepository;

    public GetPendingDispatchLeadsQueryHandler(ILeadRepository leadRepository)
        => _leadRepository = leadRepository;

    public async Task<Result<GetPendingDispatchLeadsResult>> Handle(
        GetPendingDispatchLeadsQuery query,
        CancellationToken ct)
    {
        var (leads, totalCount) = await _leadRepository.GetPendingDispatchLeadsAsync(
            search: query.Search,
            priorityLevel: query.PriorityLevel,
            addressContains: query.AddressContains,
            waitedMoreThanMinutes: query.WaitedMoreThanMinutes,
            page: query.Page,
            pageSize: query.PageSize,
            ct: ct);

        var now = DateTime.UtcNow;

        var items = leads.Select(l => new PendingDispatchLeadListItemDto(
            LeadId: l.Id,
            LeadCode: l.LeadCode,
            CustomerName: l.CustomerName,
            CustomerPhone: l.CustomerPhone,
            CustomerAddress: l.CustomerAddress,
            NeedDescription: l.NeedDescription,
            NeedType: l.NeedType?.ToString(),
            PriorityLevel: l.PriorityLevel?.ToString(),
            WaitedMinutes: (int)(now - l.CreatedAt).TotalMinutes,
            CreatedAt: l.CreatedAt
        )).ToList();

        return Result<GetPendingDispatchLeadsResult>.Success(new GetPendingDispatchLeadsResult(
            Items: items,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize));
    }
}
