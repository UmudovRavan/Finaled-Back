using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class FiscalYear : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!; // e.g. "FY-2026"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; } = false;

    public ICollection<AccountingPeriod> Periods { get; set; } = new List<AccountingPeriod>();
}

public class AccountingPeriod : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = default!;

    public string Name { get; set; } = default!; // e.g. "2026-01", "Yanvar 2026"
    public int PeriodNumber { get; set; } // 1..12
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;

    public bool IsAdjustmentPeriod { get; set; } = false;
}
