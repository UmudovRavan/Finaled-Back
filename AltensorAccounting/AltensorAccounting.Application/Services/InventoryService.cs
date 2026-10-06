using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Application.Services.Valuation;
using AltensorAccounting.Contract.DTOs.Inventory;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

namespace AltensorAccounting.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IGenericRepository<Item> _itemRepo;
    private readonly IGenericRepository<Warehouse> _warehouseRepo;
    private readonly IGenericRepository<StockTransaction> _stockTxRepo;
    private readonly IGenericRepository<StockLedgerEntry> _stockLedgerRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IStockValuationEngine _stockEngine;
    private readonly IPostingEngine _postingEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Logging.ILogger<InventoryService> _logger;

    public InventoryService(
        IGenericRepository<Item> itemRepo,
        IGenericRepository<Warehouse> warehouseRepo,
        IGenericRepository<StockTransaction> stockTxRepo,
        IGenericRepository<StockLedgerEntry> stockLedgerRepo,
        IGenericRepository<Company> companyRepo,
        IGenericRepository<Account> accountRepo,
        IStockValuationEngine stockEngine,
        IPostingEngine postingEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        Microsoft.Extensions.Logging.ILogger<InventoryService> logger)
    {
        _itemRepo = itemRepo;
        _warehouseRepo = warehouseRepo;
        _stockTxRepo = stockTxRepo;
        _stockLedgerRepo = stockLedgerRepo;
        _companyRepo = companyRepo;
        _accountRepo = accountRepo;
        _stockEngine = stockEngine;
        _postingEngine = postingEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ItemDto> CreateItemAsync(CreateItemDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        if (await _itemRepo.ExistsAsync(i => i.Code == dto.Code, ct))
        {
            throw new BusinessRuleException($"'{dto.Code}' kodlu məhsul artıq mövcuddur.");
        }

        var item = new Item
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            BaseUOM = dto.BaseUOM,
            Type = dto.Type,
            ValuationMethod = dto.ValuationMethod,
            StandardBuyingPrice = dto.StandardBuyingPrice,
            StandardSellingPrice = dto.StandardSellingPrice,
            CurrentValuationRate = dto.StandardBuyingPrice,
            InventoryAccountId = dto.InventoryAccountId,
            COGSAccountId = dto.COGSAccountId,
            RevenueAccountId = dto.RevenueAccountId
        };

        await _itemRepo.AddAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ItemDto
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            BaseUOM = item.BaseUOM,
            Type = item.Type,
            ValuationMethod = item.ValuationMethod,
            CurrentValuationRate = item.CurrentValuationRate,
            TotalStockOnHand = 0,
            TotalStockValue = 0
        };
    }

    public async Task<List<ItemDto>> GetItemsAsync(CancellationToken ct = default)
    {
        var items = await _itemRepo.GetAllAsync(ct);
        var entries = await _stockLedgerRepo.GetAllAsync(ct);

        return items.Select(i =>
        {
            var itemEntries = entries.Where(e => e.ItemId == i.Id).ToList();
            var onHand = itemEntries.GroupBy(e => e.WarehouseId)
                                    .Select(g => g.OrderByDescending(x => x.TransactionTime).FirstOrDefault())
                                    .Sum(x => x?.BalanceQty ?? 0);
            var value = itemEntries.GroupBy(e => e.WarehouseId)
                                   .Select(g => g.OrderByDescending(x => x.TransactionTime).FirstOrDefault())
                                   .Sum(x => x?.BalanceValue ?? 0);

            return new ItemDto
            {
                Id = i.Id,
                Code = i.Code,
                Name = i.Name,
                BaseUOM = i.BaseUOM,
                Type = i.Type,
                ValuationMethod = i.ValuationMethod,
                CurrentValuationRate = i.CurrentValuationRate,
                TotalStockOnHand = onHand,
                TotalStockValue = value
            };
        }).OrderBy(i => i.Code).ToList();
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var warehouse = new Warehouse
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            Location = dto.Location,
            DefaultInventoryAccountId = dto.DefaultInventoryAccountId
        };

        await _warehouseRepo.AddAsync(warehouse, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new WarehouseDto
        {
            Id = warehouse.Id,
            Code = warehouse.Code,
            Name = warehouse.Name,
            Location = warehouse.Location
        };
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken ct = default)
    {
        var warehouses = await _warehouseRepo.GetAllAsync(ct);
        return warehouses.Select(w => new WarehouseDto
        {
            Id = w.Id,
            Code = w.Code,
            Name = w.Name,
            Location = w.Location
        }).OrderBy(w => w.Code).ToList();
    }

    public async Task<StockTransactionDto> CreateStockTransactionAsync(CreateStockTransactionDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            throw new BusinessRuleException("Stok əməliyyatında ən azı bir sətir olmalıdır.");
        }

        foreach (var l in dto.Lines)
        {
            if (l.Quantity <= 0)
                throw new BusinessRuleException("Məhsul sayı 0-dan böyük olmalıdır.");
            if (l.UnitCost < 0)
                throw new BusinessRuleException("Vahid maya dəyəri mənfi ola bilməz.");
        }

        var totalValue = dto.Lines.Sum(l => l.Quantity * l.UnitCost);

        var tx = new StockTransaction
        {
            TenantId = tenantId,
            TransactionNumber = $"STK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Type = dto.Type,
            TransactionDate = dto.TransactionDate,
            PostingDate = dto.PostingDate,
            SourceWarehouseId = dto.SourceWarehouseId,
            TargetWarehouseId = dto.TargetWarehouseId,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            Status = DocumentStatus.Draft,
            TotalValue = totalValue
        };

        foreach (var l in dto.Lines)
        {
            tx.Lines.Add(new StockTransactionLine
            {
                TenantId = tenantId,
                ItemId = l.ItemId,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                TotalCost = l.Quantity * l.UnitCost,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId
            });
        }

        await _stockTxRepo.AddAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new StockTransactionDto
        {
            Id = tx.Id,
            TransactionNumber = tx.TransactionNumber,
            Type = tx.Type,
            TransactionDate = tx.TransactionDate,
            PostingDate = tx.PostingDate,
            Status = tx.Status,
            TotalValue = tx.TotalValue
        };
    }

    public async Task<StockTransactionDto> PostStockTransactionAsync(Guid transactionId, CancellationToken ct = default)
    {
        var tx = await _stockTxRepo.GetByIdAsync(transactionId, ct, t => t.Lines)
            ?? throw new BusinessRuleException("Stok əməliyyatı tapılmadı.");

        if (tx.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(tx.TransactionNumber);
        }

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var allAccounts = await _accountRepo.GetAllAsync(ct);

        var stockAccountId = company.DefaultStockAccountId
            ?? allAccounts.FirstOrDefault(a => a.Code == "1100" || a.Type == AccountType.Stock || (a.Category == AccountCategory.Asset && a.IsLeaf))?.Id
            ?? throw new BusinessRuleException("Anbar (Inventory) hesabı təyin edilməyib.");

        var cogsAccountId = company.DefaultCOGSAccountId
            ?? allAccounts.FirstOrDefault(a => a.Code == "7010" || a.Type == AccountType.COGS || (a.Category == AccountCategory.Expense && a.IsLeaf))?.Id
            ?? throw new BusinessRuleException("Maya dəyəri (COGS) hesabı təyin edilməyib.");

        decimal totalMovementCost = 0;

        foreach (var line in tx.Lines)
        {
            if (tx.Type == StockTransactionType.Receipt)
            {
                var entry = await _stockEngine.ProcessIncomingStockAsync(
                    line.ItemId,
                    tx.SourceWarehouseId,
                    line.Quantity,
                    line.UnitCost,
                    DocumentType.StockAdjustment,
                    tx.Id,
                    tx.TransactionNumber,
                    tx.PostingDate,
                    ct);
                totalMovementCost += line.Quantity * line.UnitCost;
            }
            else if (tx.Type == StockTransactionType.Issue)
            {
                var entry = await _stockEngine.ProcessOutgoingStockAsync(
                    line.ItemId,
                    tx.SourceWarehouseId,
                    line.Quantity,
                    DocumentType.StockIssue,
                    tx.Id,
                    tx.TransactionNumber,
                    tx.PostingDate,
                    ct);
                totalMovementCost += line.Quantity * entry.ValuationRate;
            }
            else if (tx.Type == StockTransactionType.Transfer)
            {
                if (!tx.TargetWarehouseId.HasValue)
                {
                    throw new BusinessRuleException("Transfer üçün hədəf anbar seçilməlidir.");
                }

                var outEntry = await _stockEngine.ProcessOutgoingStockAsync(
                    line.ItemId,
                    tx.SourceWarehouseId,
                    line.Quantity,
                    DocumentType.StockTransfer,
                    tx.Id,
                    tx.TransactionNumber,
                    tx.PostingDate,
                    ct);

                await _stockEngine.ProcessIncomingStockAsync(
                    line.ItemId,
                    tx.TargetWarehouseId.Value,
                    line.Quantity,
                    outEntry.ValuationRate,
                    DocumentType.StockTransfer,
                    tx.Id,
                    tx.TransactionNumber,
                    tx.PostingDate,
                    ct);

                totalMovementCost += line.Quantity * outEntry.ValuationRate;
            }
        }

        // GL Posting for Issue (COGS)
        if (tx.Type == StockTransactionType.Issue && totalMovementCost > 0)
        {
            var batch = new PostingBatch
            {
                SourceDocumentType = DocumentType.StockIssue,
                SourceDocumentId = tx.Id,
                SourceDocumentNumber = tx.TransactionNumber,
                PostingDate = tx.PostingDate,
                Description = $"Stock Issue {tx.TransactionNumber} - COGS"
            };

            // Dr COGS
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = cogsAccountId,
                DebitBase = totalMovementCost,
                CreditBase = 0,
                TransactionAmount = totalMovementCost,
                LineDescription = $"COGS for Issue {tx.TransactionNumber}"
            });

            // Cr Inventory
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = stockAccountId,
                DebitBase = 0,
                CreditBase = totalMovementCost,
                TransactionAmount = -totalMovementCost,
                LineDescription = $"Inventory Reduction for {tx.TransactionNumber}"
            });

            await _postingEngine.PostBatchAsync(batch, ct);
        }
        else if (tx.Type == StockTransactionType.Receipt && totalMovementCost > 0)
        {
            var grniOrGainAccountId = company.DefaultGRNIAccountId
                ?? allAccounts.FirstOrDefault(a => a.Code == "2200" || a.Type == AccountType.GRNI)?.Id
                ?? cogsAccountId;

            var batch = new PostingBatch
            {
                SourceDocumentType = DocumentType.StockAdjustment,
                SourceDocumentId = tx.Id,
                SourceDocumentNumber = tx.TransactionNumber,
                PostingDate = tx.PostingDate,
                Description = $"Stock Receipt {tx.TransactionNumber}"
            };

            // Dr Inventory
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = stockAccountId,
                DebitBase = totalMovementCost,
                CreditBase = 0,
                TransactionAmount = totalMovementCost,
                LineDescription = $"Inventory Inflow for {tx.TransactionNumber}"
            });

            // Cr GRNI / Adjustment
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = grniOrGainAccountId,
                DebitBase = 0,
                CreditBase = totalMovementCost,
                TransactionAmount = -totalMovementCost,
                LineDescription = $"Inventory Adjustment Inflow for {tx.TransactionNumber}"
            });

            await _postingEngine.PostBatchAsync(batch, ct);
        }

        tx.Status = DocumentStatus.Posted;
        tx.TotalValue = totalMovementCost;
        await _stockTxRepo.UpdateAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new StockTransactionDto
        {
            Id = tx.Id,
            TransactionNumber = tx.TransactionNumber,
            Type = tx.Type,
            TransactionDate = tx.TransactionDate,
            PostingDate = tx.PostingDate,
            Status = tx.Status,
            TotalValue = tx.TotalValue
        };
    }

    public async Task<List<StockLedgerEntryDto>> GetStockLedgerAsync(Guid? itemId, Guid? warehouseId, CancellationToken ct = default)
    {
        var entries = await _stockLedgerRepo.GetAllAsync(ct);

        if (itemId.HasValue)
        {
            entries = entries.Where(e => e.ItemId == itemId.Value).ToList();
        }
        if (warehouseId.HasValue)
        {
            entries = entries.Where(e => e.WarehouseId == warehouseId.Value).ToList();
        }

        var items = (await _itemRepo.GetAllAsync(ct)).ToDictionary(i => i.Id);
        var warehouses = (await _warehouseRepo.GetAllAsync(ct)).ToDictionary(w => w.Id);

        return entries.OrderBy(e => e.TransactionTime).Select(e => new StockLedgerEntryDto
        {
            Id = e.Id,
            PostingDate = e.PostingDate,
            ItemCode = items.TryGetValue(e.ItemId, out var itm) ? itm.Code : "",
            ItemName = items.TryGetValue(e.ItemId, out var itm2) ? itm2.Name : "",
            WarehouseCode = warehouses.TryGetValue(e.WarehouseId, out var wh) ? wh.Code : "",
            QtyIn = e.QtyIn,
            QtyOut = e.QtyOut,
            ValuationRate = e.ValuationRate,
            BalanceQty = e.BalanceQty,
            BalanceValue = e.BalanceValue,
            SourceDocumentType = e.SourceDocumentType,
            SourceDocumentNumber = e.SourceDocumentNumber
        }).ToList();
    }
}
