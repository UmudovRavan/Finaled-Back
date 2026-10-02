using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Application.Services.Procurement;

public class ThreeWayMatchService : IThreeWayMatchService
{
    private readonly IGenericRepository<PurchaseOrder> _poRepo;
    private readonly IGenericRepository<GoodsReceipt> _grnRepo;

    public const decimal MaxPriceTolerancePercent = 3.0m; // ±3%
    public const decimal MaxQuantityTolerancePercent = 0.0m; // 0%

    public ThreeWayMatchService(
        IGenericRepository<PurchaseOrder> poRepo,
        IGenericRepository<GoodsReceipt> grnRepo)
    {
        _poRepo = poRepo;
        _grnRepo = grnRepo;
    }

    public async Task<ThreeWayMatchResultDto> EvaluateMatchAsync(SupplierInvoice invoice, CancellationToken ct = default)
    {
        if (invoice.PurchaseOrderId == null && invoice.GoodsReceiptId == null)
        {
            return new ThreeWayMatchResultDto
            {
                IsMatched = true,
                Status = ThreeWayMatchStatus.NotApplicable,
                Message = "Faktura PO və ya Qəbul sənədinə bağlı deyil (Direct expense invoice)."
            };
        }

        PurchaseOrder? po = null;
        if (invoice.PurchaseOrderId.HasValue)
        {
            po = await _poRepo.GetByIdAsync(invoice.PurchaseOrderId.Value, ct, p => p.Lines);
        }

        GoodsReceipt? grn = null;
        if (invoice.GoodsReceiptId.HasValue)
        {
            grn = await _grnRepo.GetByIdAsync(invoice.GoodsReceiptId.Value, ct, g => g.Lines);
        }

        decimal totalQtyDiff = 0;
        decimal totalPriceDiff = 0;
        bool hasToleranceExceeded = false;

        foreach (var invLine in invoice.Lines)
        {
            // Match with GRN quantity
            if (grn != null)
            {
                var grnLine = grn.Lines.FirstOrDefault(g => g.ItemId == invLine.ItemId);
                if (grnLine != null)
                {
                    var qtyDiff = invLine.Quantity - grnLine.ReceivedQuantity;
                    if (qtyDiff > 0)
                    {
                        totalQtyDiff += qtyDiff;
                        hasToleranceExceeded = true; // Over-billing received quantity not allowed
                    }
                }
            }

            // Match with PO unit price
            if (po != null)
            {
                var poLine = po.Lines.FirstOrDefault(p => p.ItemId == invLine.ItemId);
                if (poLine != null && poLine.UnitPrice > 0)
                {
                    var pricePercentDiff = Math.Abs((invLine.UnitPrice - poLine.UnitPrice) / poLine.UnitPrice) * 100m;
                    if (pricePercentDiff > MaxPriceTolerancePercent)
                    {
                        totalPriceDiff += Math.Abs(invLine.UnitPrice - poLine.UnitPrice);
                        hasToleranceExceeded = true;
                    }
                }
            }
        }

        if (hasToleranceExceeded)
        {
            return new ThreeWayMatchResultDto
            {
                IsMatched = false,
                Status = ThreeWayMatchStatus.OnHoldToleranceExceeded,
                QuantityDifference = totalQtyDiff,
                PriceDifference = totalPriceDiff,
                TotalAmountDifference = totalQtyDiff + totalPriceDiff,
                Message = "3-Way Match toleransı aşıldı! Faktura təsdiq olunana qədər bloklanır."
            };
        }

        return new ThreeWayMatchResultDto
        {
            IsMatched = true,
            Status = ThreeWayMatchStatus.Matched,
            QuantityDifference = 0,
            PriceDifference = 0,
            TotalAmountDifference = 0,
            Message = "3-Way Match uğurla tamamlandı. Faktura təsdiq edilə bilər."
        };
    }
}
