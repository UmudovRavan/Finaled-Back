using System;

namespace AltensorAccounting.Domain.Common;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
