using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AltensorAccounting.Contract.Services;
using Microsoft.AspNetCore.Http;

namespace AltensorAccounting.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? TenantId
    {
        get
        {
            var val = User?.FindFirstValue("tenant_id");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? User?.FindFirstValue("sub");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? TenantStatus => User?.FindFirstValue("tenant_status");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsPlatformSuperAdmin => User?.IsInRole("PlatformSuperAdmin") == true;

    public bool IsTenantAdmin => User?.IsInRole("TenantAdmin") == true;
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                   ?? User?.FindFirstValue("sub");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var val = User?.FindFirstValue("tenant_id");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? TenantStatus => User?.FindFirstValue("tenant_status");

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) 
                         ?? User?.FindFirstValue("email");

    public string? FullName => User?.FindFirstValue("name");

    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                                     ?? User?.FindAll("roles").Select(c => c.Value)
                                     ?? Enumerable.Empty<string>();

    public IEnumerable<string> Permissions => User?.FindAll("permission").Select(c => c.Value)
                                           ?? User?.FindAll("permissions").Select(c => c.Value)
                                           ?? Enumerable.Empty<string>();

    public IEnumerable<string> Modules => User?.FindAll("module").Select(c => c.Value)
                                       ?? User?.FindAll("modules").Select(c => c.Value)
                                       ?? Enumerable.Empty<string>();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool HasModuleAccess(string moduleCode) =>
        Modules.Contains(moduleCode, StringComparer.OrdinalIgnoreCase);
}
