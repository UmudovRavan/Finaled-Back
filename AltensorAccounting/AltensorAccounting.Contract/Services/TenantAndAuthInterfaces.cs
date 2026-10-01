using System;
using System.Collections.Generic;

namespace AltensorAccounting.Contract.Services;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    string? TenantStatus { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformSuperAdmin { get; }
    bool IsTenantAdmin { get; }
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? TenantStatus { get; }
    string? Email { get; }
    string? FullName { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    IEnumerable<string> Modules { get; }
    bool HasPermission(string permission);
    bool HasModuleAccess(string moduleCode);
}
