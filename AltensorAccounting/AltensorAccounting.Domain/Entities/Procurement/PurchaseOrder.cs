using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Procurement;

public class Supplier : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; } // VÖEN
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? BankAccountDetails { get; set; }

    public Guid? PayableAccountId { get; set; } // Custom AP control account if applicable
    public int PaymentTermsDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class PurchaseRequisition : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string RequisitionNumber { get; set; } = default!;
    public DateTime RequiredDate { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string? Department { get; set; }
    public string? Purpose { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = new List<PurchaseRequisitionLine>();
}

public class PurchaseRequisitionLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition PurchaseRequisition { get; set; } = default!;

    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal RequestedQuantity { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
}

public class PurchaseOrder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; set; } = default!; // e.g. "PO-2026-0001"
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public string? TermsAndConditions { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public class PurchaseOrderLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = default!;

    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; } = 0;
    public decimal InvoicedQuantity { get; set; } = 0;

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal LineSubTotal { get; set; }
    public decimal TaxPercent { get; set; } = 18.0m;
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
}
