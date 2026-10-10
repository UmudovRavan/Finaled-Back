using System;
using System.Collections.Generic;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Contract.DTOs.Sales;

public class CreateSalesOrderDto
{
    public Guid CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesOrderLineDto> Lines { get; set; } = new();
}

public class CreateSalesOrderLineDto
{
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public Guid? TaxCodeId { get; set; }
    public decimal TaxRate { get; set; } = 18.0m;
}

public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public string StatusName { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }
    public List<SalesOrderLineDto> Lines { get; set; } = new();
}

public class SalesOrderLineDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal RemainingDeliveryQuantity => Math.Max(0, Quantity - DeliveredQuantity);
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateDeliveryNoteDto
{
    public Guid? SalesOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateDeliveryNoteLineDto> Lines { get; set; } = new();
}

public class CreateDeliveryNoteLineDto
{
    public Guid? SalesOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
}

public class DeliveryNoteDto
{
    public Guid Id { get; set; }
    public string DeliveryNumber { get; set; } = default!;
    public Guid? SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public DateTime DeliveryDate { get; set; }
    public DateTime? PostingDate { get; set; }
    public DeliveryNoteStatus Status { get; set; }
    public string StatusName { get; set; } = default!;
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
    public List<DeliveryNoteLineDto> Lines { get; set; } = new();
}

public class DeliveryNoteLineDto
{
    public Guid Id { get; set; }
    public Guid? SalesOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = default!;
    public string ItemName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
