using System;

namespace AltensorAccounting.Contract.DTOs.Webhooks;

public class UserCreatedIntegrationEvent
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
