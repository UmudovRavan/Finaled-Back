using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

namespace AltensorAccounting.Application.Services.Valuation;

public class StockValuationEngine : IStockValuationEngine
{
    private readonly IGenericRepository<Item> _itemRepo;
    private readonly IGenericRepository<StockLedgerEntry> _stockLedgerRepo;
    private readonly IGenericRepository<CostLayer> _costLayerRepo;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public StockValuationEngine(
        IGenericRepository<Item> itemRepo,
        IGenericRepository<StockLedgerEntry> stockLedgerRepo,
        IGenericRepository<CostLayer> costLayerRepo,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork)
    {
        _itemRepo = itemRepo;
        _stockLedgerRepo = stockLedgerRepo;
        _costLayerRepo = costLayerRepo;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockLedgerEntry> ProcessIncomingStockAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        decimal unitCost,
        DocumentType sourceDocType,
        Guid sourceDocId,
        string? sourceDocNumber,
        DateTime postingDate,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new BusinessRuleException("Məhsul tapılmadı.");

        // Get latest stock ledger entry for this item in this warehouse
        var latestEntry = (await _stockLedgerRepo.FindAsync(e => e.ItemId == itemId && e.WarehouseId == warehouseId, ct))
            .OrderByDescending(e => e.TransactionTime)
            .FirstOrDefault();

        var currentQty = latestEntry?.BalanceQty ?? 0;
        var currentRate = item.CurrentValuationRate > 0 ? item.CurrentValuationRate : (latestEntry?.ValuationRate ?? 0);
        var currentTotalValue = currentQty * currentRate;

        var newQty = currentQty + quantity;
        decimal newRate;

        if (item.ValuationMethod == ValuationMethod.FIFO)
        {
            // Add new Cost Layer
            var costLayer = new CostLayer
            {
                TenantId = tenantId,
                ItemId = itemId,
                WarehouseId = warehouseId,
                ReceiptDate = postingDate,
                InitialQty = quantity,
                RemainingQty = quantity,
                UnitCost = unitCost,
                SourceDocumentId = sourceDocId,
                IsExhausted = false
            };
            await _costLayerRepo.AddAsync(costLayer, ct);

            newRate = newQty > 0 ? (currentTotalValue + (quantity * unitCost)) / newQty : unitCost;
        }
        else // Moving Average (AVCO)
        {
            if (newQty <= 0)
            {
                newRate = unitCost;
            }
            else
            {
                newRate = (currentTotalValue + (quantity * unitCost)) / newQty;
            }
        }

        item.CurrentValuationRate = Math.Round(newRate, 4);
        await _itemRepo.UpdateAsync(item, ct);

        var ledgerEntry = new StockLedgerEntry
        {
            TenantId = tenantId,
            PostingDate = postingDate,
            TransactionTime = DateTime.UtcNow,
            ItemId = itemId,
            WarehouseId = warehouseId,
            QtyIn = quantity,
            QtyOut = 0,
            ValuationRate = Math.Round(newRate, 4),
            IncomingCost = unitCost,
            BalanceQty = newQty,
            BalanceValue = Math.Round(newQty * newRate, 2),
            SourceDocumentType = sourceDocType,
            SourceDocumentId = sourceDocId,
            SourceDocumentNumber = sourceDocNumber
        };

        await _stockLedgerRepo.AddAsync(ledgerEntry, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ledgerEntry;
    }

    public async Task<StockLedgerEntry> ProcessOutgoingStockAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        DocumentType sourceDocType,
        Guid sourceDocId,
        string? sourceDocNumber,
        DateTime postingDate,
        CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var item = await _itemRepo.GetByIdAsync(itemId, ct)
            ?? throw new BusinessRuleException("Məhsul tapılmadı.");

        var latestEntry = (await _stockLedgerRepo.FindAsync(e => e.ItemId == itemId && e.WarehouseId == warehouseId, ct))
            .OrderByDescending(e => e.TransactionTime)
            .FirstOrDefault();

        var currentQty = latestEntry?.BalanceQty ?? 0;
        if (currentQty < quantity)
        {
            throw new InsufficientStockException(item.Code, currentQty, quantity);
        }

        decimal effectiveUnitCost;

        if (item.ValuationMethod == ValuationMethod.FIFO)
        {
            // Deduct from oldest available cost layers
            var availableLayers = (await _costLayerRepo.FindAsync(l => 
                l.ItemId == itemId && 
                l.WarehouseId == warehouseId && 
                !l.IsExhausted, ct))
                .OrderBy(l => l.ReceiptDate)
                .ThenBy(l => l.CreatedAt)
                .ToList();

            decimal remainingToDeduct = quantity;
            decimal totalCostDeducted = 0;

            foreach (var layer in availableLayers)
            {
                if (remainingToDeduct <= 0) break;

                if (layer.RemainingQty <= remainingToDeduct)
                {
                    totalCostDeducted += layer.RemainingQty * layer.UnitCost;
                    remainingToDeduct -= layer.RemainingQty;
                    layer.RemainingQty = 0;
                    layer.IsExhausted = true;
                }
                else
                {
                    totalCostDeducted += remainingToDeduct * layer.UnitCost;
                    layer.RemainingQty -= remainingToDeduct;
                    remainingToDeduct = 0;
                }

                await _costLayerRepo.UpdateAsync(layer, ct);
            }

            effectiveUnitCost = quantity > 0 ? totalCostDeducted / quantity : item.CurrentValuationRate;
        }
        else // Moving Average
        {
            effectiveUnitCost = item.CurrentValuationRate > 0 ? item.CurrentValuationRate : (latestEntry?.ValuationRate ?? 0);
        }

        var newQty = currentQty - quantity;
        var newTotalValue = Math.Round(newQty * item.CurrentValuationRate, 2);

        var ledgerEntry = new StockLedgerEntry
        {
            TenantId = tenantId,
            PostingDate = postingDate,
            TransactionTime = DateTime.UtcNow,
            ItemId = itemId,
            WarehouseId = warehouseId,
            QtyIn = 0,
            QtyOut = quantity,
            ValuationRate = Math.Round(effectiveUnitCost, 4),
            IncomingCost = 0,
            BalanceQty = newQty,
            BalanceValue = newTotalValue,
            SourceDocumentType = sourceDocType,
            SourceDocumentId = sourceDocId,
            SourceDocumentNumber = sourceDocNumber
        };

        await _stockLedgerRepo.AddAsync(ledgerEntry, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ledgerEntry;
    }
}
