using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class DeliveryNote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string DeliveryNumber { get; set; } = default!; // e.g. "DN-2026-0001" / "İRS-2026-0001"
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;
    public DateTime? PostingDate { get; set; }
    public DeliveryNoteStatus Status { get; set; } = DeliveryNoteStatus.Draft;

    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }

    public decimal TotalCost { get; set; } // Maya dəyəri cəmi
    public Guid? StockTransactionId { get; set; } // Anbar çıxışı linki

    public ICollection<DeliveryNoteLine> Lines { get; set; } = new List<DeliveryNoteLine>();
}

public class DeliveryNoteLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = default!;

    public Guid? SalesOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; } // Çıxış anında vahid maya dəyəri
    public decimal TotalCost { get; set; }
}
