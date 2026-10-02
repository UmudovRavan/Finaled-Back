using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; } // VÖEN
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public Guid? ReceivableAccountId { get; set; } // Custom AR control account if applicable
    public decimal CreditLimit { get; set; } = 0;
    public int PaymentTermsDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class CustomerInvoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = default!; // e.g. "INV-2026-0001"
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }

    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.Draft;
    public SettlementStatus SettlementStatus { get; set; } = SettlementStatus.Unpaid;

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal OutstandingAmount { get; set; } // derived open item balance

    public string? Notes { get; set; }

    public ICollection<CustomerInvoiceLine> Lines { get; set; } = new List<CustomerInvoiceLine>();
}

public class CustomerInvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = default!;

    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal LineSubTotal { get; set; }

    public Guid? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Guid? RevenueAccountId { get; set; } // Income GL account
    public Account? RevenueAccount { get; set; }

    // Dimensions
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class CustomerCreditNote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string CreditNoteNumber { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid OriginalInvoiceId { get; set; }
    public CustomerInvoice OriginalInvoice { get; set; } = default!;

    public DateTime CreditNoteDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalCredit { get; set; }
    public string Reason { get; set; } = default!;
}
