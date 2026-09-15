namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
}
