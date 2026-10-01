using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class Payment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string PaymentNumber { get; set; } = default!; // e.g. "PAY-2026-0001"
    public PaymentType Type { get; set; } // CustomerReceipt, SupplierPayment, CustomerAdvance, etc.
    public DateTime PaymentDate { get; set; }
    public DateTime PostingDate { get; set; }

    public Guid? PartyId { get; set; } // Customer or Supplier ID
    public string? PartyType { get; set; }

    public Guid BankOrCashAccountId { get; set; } // GL Asset account (Bank/Cash)
    public Account BankOrCashAccount { get; set; } = default!;

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; } = 0;
    public decimal UnallocatedAmount { get; set; } // Advance / On-account amount

    public string? ReferenceNumber { get; set; } // Bank transaction ref / check no
    public string? Notes { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}

public class PaymentAllocation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;

    public DocumentType TargetDocumentType { get; set; } // CustomerInvoice or SupplierInvoice
    public Guid TargetDocumentId { get; set; } // Invoice ID
    public string? TargetDocumentNumber { get; set; }

    public decimal AllocatedAmount { get; set; }
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
}
