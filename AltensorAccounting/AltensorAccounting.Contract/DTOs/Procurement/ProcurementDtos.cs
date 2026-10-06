using System;
using System.Collections.Generic;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Contract.DTOs.Procurement;

public class CreateSupplierDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Guid? PayableAccountId { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
}

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public Guid? PayableAccountId { get; set; }
    public decimal OutstandingPayable { get; set; }
}

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public string? TermsAndConditions { get; set; }
    public List<PurchaseOrderLineInputDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineInputDto
{
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal OrderedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal TaxPercent { get; set; } = 18.0m;
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal GrandTotal { get; set; }
}

public class CreateGoodsReceiptDto
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public DateTime PostingDate { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WaybillNumber { get; set; }
    public List<GoodsReceiptLineInputDto> Lines { get; set; } = new();
}

public class GoodsReceiptLineInputDto
{
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string? Description { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public DateTime ReceiptDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus Status { get; set; }
    public decimal TotalValue { get; set; }
}

public class CreateSupplierInvoiceDto
{
    public Guid SupplierId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public string? SupplierInvoiceReference { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public bool IsGRNIBased { get; set; } = false;
    public string? Notes { get; set; }
    public List<SupplierInvoiceLineInputDto> Lines { get; set; } = new();
}

public class SupplierInvoiceLineInputDto
{
    public Guid? ItemId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? TaxCodeId { get; set; }
    public Guid? ExpenseOrAssetAccountId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class SupplierInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public string SupplierInvoiceNumber { get; set; } = default!;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus DocumentStatus { get; set; }
    public SettlementStatus SettlementStatus { get; set; }
    public ThreeWayMatchStatus ThreeWayMatchStatus { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class ThreeWayMatchResultDto
{
    public bool IsMatched { get; set; }
    public ThreeWayMatchStatus Status { get; set; }
    public decimal QuantityDifference { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal TotalAmountDifference { get; set; }
    public string Message { get; set; } = default!;
}
