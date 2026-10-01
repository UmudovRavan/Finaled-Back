using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Inventory;

public class StockTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string TransactionNumber { get; set; } = default!; // e.g. "STK-2026-0001"
    public StockTransactionType Type { get; set; } // Receipt, Issue, Transfer, Adjustment
    public DateTime TransactionDate { get; set; }
    public DateTime PostingDate { get; set; }

    public Guid SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = default!;

    public Guid? TargetWarehouseId { get; set; } // Only for Transfer
    public Warehouse? TargetWarehouse { get; set; }

    public DocumentType SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public decimal TotalValue { get; set; }

    public ICollection<StockTransactionLine> Lines { get; set; } = new List<StockTransactionLine>();
}

public class StockTransactionLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid StockTransactionId { get; set; }
    public StockTransaction StockTransaction { get; set; } = default!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class StockLedgerEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime PostingDate { get; set; }
    public DateTime TransactionTime { get; set; } = DateTime.UtcNow;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public decimal QtyIn { get; set; } = 0;
    public decimal QtyOut { get; set; } = 0;
    public decimal ValuationRate { get; set; } // Vahid maya dəyəri
    public decimal IncomingCost { get; set; } = 0;

    public decimal BalanceQty { get; set; }     // Hərəkətdən sonrakı qalıq kəmiyyət
    public decimal BalanceValue { get; set; }   // Hərəkətdən sonrakı qalıq dəyər

    public DocumentType SourceDocumentType { get; set; }
    public Guid SourceDocumentId { get; set; }
    public Guid? SourceLineId { get; set; }
    public string? SourceDocumentNumber { get; set; }
}

public class CostLayer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }

    public DateTime ReceiptDate { get; set; }
    public decimal InitialQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal UnitCost { get; set; }

    public Guid SourceDocumentId { get; set; }
    public bool IsExhausted { get; set; } = false;
}
