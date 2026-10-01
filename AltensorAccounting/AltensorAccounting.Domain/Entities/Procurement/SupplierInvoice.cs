using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Procurement;

public class GoodsReceipt : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ReceiptNumber { get; set; } = default!; // e.g. "GRN-2026-0001"
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public DateTime ReceiptDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public Guid WarehouseId { get; set; }
    public string? WaybillNumber { get; set; } // Qaimə / İrsaliyyə nömrəsi
    public decimal TotalValue { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

public class GoodsReceiptLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = default!;

    public Guid? PurchaseOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = default!;

    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? WarehouseId { get; set; }
}

public class SupplierInvoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = default!; // Internal ref e.g. "PINV-2026-0001"
    public string SupplierInvoiceNumber { get; set; } = default!; // Təchizatçının öz faktura nömrəsi

    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }

    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.Draft;
    public SettlementStatus SettlementStatus { get; set; } = SettlementStatus.Unpaid;
    public ThreeWayMatchStatus ThreeWayMatchStatus { get; set; } = ThreeWayMatchStatus.Pending;

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal OutstandingAmount { get; set; }

    public bool IsGRNIBased { get; set; } = false; // Mal əvvəlcədən daxil olubsa GRNI təmizlənir
    public string? Notes { get; set; }

    public ICollection<SupplierInvoiceLine> Lines { get; set; } = new List<SupplierInvoiceLine>();
}

public class SupplierInvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = default!;

    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubTotal { get; set; }

    public Guid? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Guid ExpenseOrAssetAccountId { get; set; } // GRNI, Expense or Inventory Asset account
    public Account ExpenseOrAssetAccount { get; set; } = default!;

    // Dimensions
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class SupplierDebitNote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string DebitNoteNumber { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public Guid OriginalInvoiceId { get; set; }
    public SupplierInvoice OriginalInvoice { get; set; } = default!;

    public DateTime DebitNoteDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalDebit { get; set; }
    public string Reason { get; set; } = default!;
}
