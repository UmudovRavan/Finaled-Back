using System;
using System.Collections.Generic;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Contract.DTOs.Inventory;

public class CreateItemDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string BaseUOM { get; set; } = "PCS";
    public ItemType Type { get; set; } = ItemType.StockItem;
    public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.MovingAverage;
    public decimal StandardBuyingPrice { get; set; }
    public decimal StandardSellingPrice { get; set; }
    public Guid? InventoryAccountId { get; set; }
    public Guid? COGSAccountId { get; set; }
    public Guid? RevenueAccountId { get; set; }
}

public class ItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string BaseUOM { get; set; } = "PCS";
    public ItemType Type { get; set; }
    public ValuationMethod ValuationMethod { get; set; }
    public decimal CurrentValuationRate { get; set; }
    public decimal TotalStockOnHand { get; set; }
    public decimal TotalStockValue { get; set; }
}

public class CreateWarehouseDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public Guid? DefaultInventoryAccountId { get; set; }
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
}

public class CreateStockTransactionDto
{
    public StockTransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime PostingDate { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public List<StockTransactionLineInputDto> Lines { get; set; } = new();
}

public class StockTransactionLineInputDto
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class StockTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = default!;
    public StockTransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus Status { get; set; }
    public decimal TotalValue { get; set; }
}

public class StockLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime PostingDate { get; set; }
    public string ItemCode { get; set; } = default!;
    public string ItemName { get; set; } = default!;
    public string WarehouseCode { get; set; } = default!;
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal ValuationRate { get; set; }
    public decimal BalanceQty { get; set; }
    public decimal BalanceValue { get; set; }
    public DocumentType SourceDocumentType { get; set; }
    public string? SourceDocumentNumber { get; set; }
}
