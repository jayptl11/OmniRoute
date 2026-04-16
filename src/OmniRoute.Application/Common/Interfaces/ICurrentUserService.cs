namespace OmniRoute.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid GetUserId();
    Guid? TryGetUserId();
}

