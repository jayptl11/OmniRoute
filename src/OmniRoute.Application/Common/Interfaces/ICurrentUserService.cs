namespace OmniRoute.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    Guid? TryGetUserId();
    string? Role { get; }
    Guid? TeamId { get; }
    Guid? StoreId { get; }
}

