using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Resolves the current user's identity and context from the HTTP request.
/// Used by the audit trail system to capture user details for each action.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(userIdClaim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
        ?? "System";

    /// <inheritdoc />
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? SessionId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("sid")
        ?? _httpContextAccessor.HttpContext?.Session?.Id;

    /// <inheritdoc />
    public Guid? TenantId
    {
        get
        {
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
            return Guid.TryParse(tenantClaim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
