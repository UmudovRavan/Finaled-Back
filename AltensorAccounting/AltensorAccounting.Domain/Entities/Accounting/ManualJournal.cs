using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class ManualJournal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string JournalNumber { get; set; } = default!; // e.g. "JV-2026-0001"
    public DateTime PostingDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Description { get; set; } = default!;

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal TotalAmount { get; set; }

    public bool IsReversal { get; set; } = false;
    public Guid? ReversalOfJournalId { get; set; }
    public string? ReversalReason { get; set; }

    public ICollection<ManualJournalLine> Lines { get; set; } = new List<ManualJournalLine>();
}

public class ManualJournalLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ManualJournalId { get; set; }
    public ManualJournal ManualJournal { get; set; } = default!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public Guid? PartyId { get; set; }
    public string? PartyType { get; set; }

    // Dimensions
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }

    public string? Description { get; set; }
}
