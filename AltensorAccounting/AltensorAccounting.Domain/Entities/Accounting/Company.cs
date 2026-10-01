using AltensorAccounting.Domain.Common;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class Company : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string TaxNumber { get; set; } = default!; // VÖEN
    public string BaseCurrency { get; set; } = "AZN";
    public string Country { get; set; } = "Azerbaijan";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    // Default Control Accounts
    public Guid? DefaultReceivableAccountId { get; set; }
    public Guid? DefaultPayableAccountId { get; set; }
    public Guid? DefaultStockAccountId { get; set; }
    public Guid? DefaultGRNIAccountId { get; set; }
    public Guid? DefaultCOGSAccountId { get; set; }
    public Guid? DefaultRetainedEarningsAccountId { get; set; }
    public Guid? DefaultInputVatAccountId { get; set; }
    public Guid? DefaultOutputVatAccountId { get; set; }
    public Guid? DefaultFXGainLossAccountId { get; set; }
}
