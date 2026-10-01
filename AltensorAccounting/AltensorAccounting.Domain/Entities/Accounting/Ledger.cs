using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class PostingBatch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string BatchNumber { get; set; } = default!;
    public DateTime PostingDate { get; set; }
    public DocumentType SourceDocumentType { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string? SourceDocumentNumber { get; set; }
    public string? Description { get; set; }

    public decimal TotalDebitBase { get; set; }
    public decimal TotalCreditBase { get; set; }

    public bool IsReversed { get; set; } = false;
    public Guid? ReversalBatchId { get; set; }

    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();
}

public class LedgerEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PostingBatchId { get; set; }
    public PostingBatch PostingBatch { get; set; } = default!;

    public DateTime PostingDate { get; set; }
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    // Base currency amounts (AZN)
    public decimal DebitBase { get; set; }
    public decimal CreditBase { get; set; }

    // Transaction currency amounts
    public string TransactionCurrency { get; set; } = "AZN";
    public decimal TransactionAmount { get; set; }
    public decimal ExchangeRate { get; set; } = 1.0m;

    // Party Link (Customer / Supplier)
    public Guid? PartyId { get; set; }
    public string? PartyType { get; set; } // "Customer", "Supplier"

    // Traceability to source
    public DocumentType SourceDocumentType { get; set; }
    public Guid SourceDocumentId { get; set; }
    public Guid? SourceLineId { get; set; }

    // Dimensions
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }

    public string? LineDescription { get; set; }
}
