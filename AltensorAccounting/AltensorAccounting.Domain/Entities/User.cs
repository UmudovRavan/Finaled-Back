using AltensorAccounting.Domain.Common;

namespace AltensorAccounting.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public bool IsActive { get; set; } = true;
}
