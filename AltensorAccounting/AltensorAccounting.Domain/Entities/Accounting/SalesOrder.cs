using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class SalesOrder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; set; } = default!; // e.g. "SO-2026-0001"
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }
    public string? CustomerReference { get; set; } // Müştərinin sifariş nömrəsi

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}

public class SalesOrderLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = default!;

    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal DeliveredQuantity { get; set; } = 0;
    public decimal InvoicedQuantity { get; set; } = 0;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public Guid? TaxCodeId { get; set; }
    public decimal TaxRate { get; set; } = 18.0m;
    public decimal LineTotal { get; set; }
}
