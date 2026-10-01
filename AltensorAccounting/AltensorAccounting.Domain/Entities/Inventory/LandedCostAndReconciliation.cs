using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Inventory;

public class LandedCostVoucher : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string VoucherNumber { get; set; } = default!;
    public DateTime PostingDate { get; set; }

    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = default!;

    public decimal TotalFreightAmount { get; set; } = 0;
    public decimal TotalCustomsAmount { get; set; } = 0;
    public decimal TotalInsuranceAmount { get; set; } = 0;
    public decimal TotalAdditionalCost { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public ICollection<LandedCostItem> Items { get; set; } = new List<LandedCostItem>();
}

public class LandedCostItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid LandedCostVoucherId { get; set; }
    public LandedCostVoucher LandedCostVoucher { get; set; } = default!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public decimal OriginalCost { get; set; }
    public decimal AllocatedAdditionalCost { get; set; }
    public decimal NewTotalCost { get; set; }
}

public class StockReconciliation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ReconciliationNumber { get; set; } = default!;
    public DateTime ReconciliationDate { get; set; }
    public DateTime PostingDate { get; set; }

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public string? Reason { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal TotalAdjustmentValue { get; set; }

    public ICollection<StockReconciliationLine> Lines { get; set; } = new List<StockReconciliationLine>();
}

public class StockReconciliationLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StockReconciliationId { get; set; }
    public StockReconciliation StockReconciliation { get; set; } = default!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public decimal SystemQty { get; set; }
    public decimal PhysicalQty { get; set; }
    public decimal DifferenceQty { get; set; } // PhysicalQty - SystemQty

    public decimal ValuationRate { get; set; }
    public decimal DifferenceValue { get; set; }
}
