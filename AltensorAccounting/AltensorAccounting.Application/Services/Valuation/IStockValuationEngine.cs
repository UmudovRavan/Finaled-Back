using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Application.Services.Valuation;

public interface IStockValuationEngine
{
    Task<StockLedgerEntry> ProcessIncomingStockAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        decimal unitCost,
        DocumentType sourceDocType,
        Guid sourceDocId,
        string? sourceDocNumber,
        DateTime postingDate,
        CancellationToken ct = default);

    Task<StockLedgerEntry> ProcessOutgoingStockAsync(
        Guid itemId,
        Guid warehouseId,
        decimal quantity,
        DocumentType sourceDocType,
        Guid sourceDocId,
        string? sourceDocNumber,
        DateTime postingDate,
        CancellationToken ct = default);
}
