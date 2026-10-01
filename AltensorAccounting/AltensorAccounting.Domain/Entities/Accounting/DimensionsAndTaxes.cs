using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class AccountingDimension : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DimensionType Type { get; set; } // CostCenter, Project, Department, Branch
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TaxCode : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!; // e.g. "VAT-18", "VAT-0", "EXEMPT"
    public string Name { get; set; } = default!;
    public decimal RatePercent { get; set; } // e.g. 18.0
    public bool IsInclusive { get; set; } = false;

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Guid? TaxAccountId { get; set; } // GL mapping for tax liability/asset
    public Account? TaxAccount { get; set; }

    public bool IsActive { get; set; } = true;
}
