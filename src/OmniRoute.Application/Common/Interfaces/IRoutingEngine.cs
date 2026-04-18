namespace OmniRoute.Application.Common.Interfaces;

public interface IRoutingEngine
{
    Task ProcessAsync(Guid leadId, CancellationToken ct = default);
}
