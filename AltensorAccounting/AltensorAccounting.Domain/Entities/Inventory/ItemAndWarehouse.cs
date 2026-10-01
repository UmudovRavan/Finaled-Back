using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Inventory;

public class Item : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!; // SKU
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string BaseUOM { get; set; } = "PCS"; // Default ölçü vahidi: PCS, KG, L, M, etc.

    public ItemType Type { get; set; } = ItemType.StockItem;
    public ValuationMethod ValuationMethod { get; set; } = ValuationMethod.MovingAverage;

    public decimal StandardBuyingPrice { get; set; } = 0;
    public decimal StandardSellingPrice { get; set; } = 0;
    public decimal CurrentValuationRate { get; set; } = 0; // Cari orta maya dəyəri

    // GL Account Defaults for this Item
    public Guid? InventoryAccountId { get; set; }
    public Guid? COGSAccountId { get; set; }
    public Guid? RevenueAccountId { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<ItemUomConversion> UomConversions { get; set; } = new List<ItemUomConversion>();
}

public class ItemUomConversion : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public string FromUOM { get; set; } = default!; // e.g. "BOX"
    public string ToUOM { get; set; } = default!;   // e.g. "PCS"
    public decimal ConversionFactor { get; set; }  // e.g. 1 BOX = 12 PCS -> Factor = 12
}

public class Warehouse : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Location { get; set; }

    public Guid? DefaultInventoryAccountId { get; set; }
    public Account? DefaultInventoryAccount { get; set; }

    public bool IsActive { get; set; } = true;
}
