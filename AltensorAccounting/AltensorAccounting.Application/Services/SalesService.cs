using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Application.Services.Valuation;
using AltensorAccounting.Contract.DTOs.Sales;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Application.Services;

public class SalesService : ISalesService
{
    private readonly IGenericRepository<SalesOrder> _salesOrderRepo;
    private readonly IGenericRepository<DeliveryNote> _deliveryRepo;
    private readonly IGenericRepository<Customer> _customerRepo;
    private readonly IGenericRepository<Item> _itemRepo;
    private readonly IGenericRepository<Warehouse> _warehouseRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IPostingEngine _postingEngine;
    private readonly IStockValuationEngine _valuationEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IGenericRepository<SalesOrder> salesOrderRepo,
        IGenericRepository<DeliveryNote> deliveryRepo,
        IGenericRepository<Customer> customerRepo,
        IGenericRepository<Item> itemRepo,
        IGenericRepository<Warehouse> warehouseRepo,
        IGenericRepository<Company> companyRepo,
        IGenericRepository<Account> accountRepo,
        IPostingEngine postingEngine,
        IStockValuationEngine valuationEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        ILogger<SalesService> logger)
    {
        _salesOrderRepo = salesOrderRepo;
        _deliveryRepo = deliveryRepo;
        _customerRepo = customerRepo;
        _itemRepo = itemRepo;
        _warehouseRepo = warehouseRepo;
        _companyRepo = companyRepo;
        _accountRepo = accountRepo;
        _postingEngine = postingEngine;
        _valuationEngine = valuationEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // 1) Satış Sifarişləri (Sales Orders)
    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId, ct)
            ?? throw new BusinessRuleException("Müştəri tapılmadı.");

        var orderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var order = new SalesOrder
        {
            TenantId = tenantId,
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            OrderDate = dto.OrderDate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            Status = SalesOrderStatus.Draft,
            Currency = dto.Currency,
            CustomerReference = dto.CustomerReference,
            Notes = dto.Notes
        };

        decimal subTotal = 0;
        decimal taxTotal = 0;

        foreach (var line in dto.Lines)
        {
            var lineSub = line.Quantity * line.UnitPrice * (1 - (line.DiscountPercent / 100m));
            var lineTax = lineSub * (line.TaxRate / 100m);
            var lineTotal = lineSub + lineTax;

            subTotal += lineSub;
            taxTotal += lineTax;

            order.Lines.Add(new SalesOrderLine
            {
                TenantId = tenantId,
                ItemId = line.ItemId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountPercent = line.DiscountPercent,
                TaxCodeId = line.TaxCodeId,
                TaxRate = line.TaxRate,
                LineTotal = lineTotal
            });
        }

        order.SubTotal = subTotal;
        order.TaxTotal = taxTotal;
        order.GrandTotal = subTotal + taxTotal;

        await _salesOrderRepo.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapSalesOrderToDto(order, customer.Name);
    }

    public async Task<List<SalesOrderDto>> GetSalesOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _salesOrderRepo.GetAllAsync(ct);
        var customers = (await _customerRepo.GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);

        return orders.Select(o => MapSalesOrderToDto(o, customers.GetValueOrDefault(o.CustomerId, "Məlum deyil")))
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _salesOrderRepo.GetByIdAsync(orderId, ct);
        if (order == null) return null;

        var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
        return MapSalesOrderToDto(order, customer?.Name ?? "Məlum deyil");
    }

    public async Task<SalesOrderDto> ConfirmSalesOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _salesOrderRepo.GetByIdAsync(orderId, ct)
            ?? throw new BusinessRuleException("Satış sifarişi tapılmadı.");

        if (order.Status != SalesOrderStatus.Draft)
        {
            throw new BusinessRuleException("Yalnız qaralama statusunda olan sifarişlər təsdiqlənə bilər.");
        }

        order.Status = SalesOrderStatus.Confirmed;
        await _salesOrderRepo.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
        return MapSalesOrderToDto(order, customer?.Name ?? "Məlum deyil");
    }

    public async Task<SalesOrderDto> CancelSalesOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _salesOrderRepo.GetByIdAsync(orderId, ct)
            ?? throw new BusinessRuleException("Satış sifarişi tapılmadı.");

        if (order.Status == SalesOrderStatus.FullyDelivered || order.Status == SalesOrderStatus.Invoiced)
        {
            throw new BusinessRuleException("Tam icra edilmiş və ya qaimələşdirilmiş sifariş ləğv edilə bilməz.");
        }

        order.Status = SalesOrderStatus.Cancelled;
        await _salesOrderRepo.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepo.GetByIdAsync(order.CustomerId, ct);
        return MapSalesOrderToDto(order, customer?.Name ?? "Məlum deyil");
    }

    // 2) Mal Təhvili / Göndərişi (Delivery Note / Goods Issue)
    public async Task<DeliveryNoteDto> CreateDeliveryNoteAsync(CreateDeliveryNoteDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId, ct)
            ?? throw new BusinessRuleException("Müştəri tapılmadı.");
        var warehouse = await _warehouseRepo.GetByIdAsync(dto.WarehouseId, ct)
            ?? throw new BusinessRuleException("Anbar tapılmadı.");

        var deliveryNumber = $"DN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var delivery = new DeliveryNote
        {
            TenantId = tenantId,
            DeliveryNumber = deliveryNumber,
            SalesOrderId = dto.SalesOrderId,
            CustomerId = customer.Id,
            WarehouseId = warehouse.Id,
            DeliveryDate = dto.DeliveryDate,
            Status = DeliveryNoteStatus.Draft,
            DriverName = dto.DriverName,
            VehicleNumber = dto.VehicleNumber,
            TrackingNumber = dto.TrackingNumber,
            Notes = dto.Notes
        };

        foreach (var line in dto.Lines)
        {
            var item = await _itemRepo.GetByIdAsync(line.ItemId, ct)
                ?? throw new BusinessRuleException($"Məhsul (ID: {line.ItemId}) tapılmadı.");

            delivery.Lines.Add(new DeliveryNoteLine
            {
                TenantId = tenantId,
                SalesOrderLineId = line.SalesOrderLineId,
                ItemId = item.Id,
                Description = line.Description ?? item.Name,
                Quantity = line.Quantity,
                UnitCost = item.CurrentValuationRate,
                TotalCost = line.Quantity * item.CurrentValuationRate
            });
        }

        delivery.TotalCost = delivery.Lines.Sum(l => l.TotalCost);

        await _deliveryRepo.AddAsync(delivery, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapDeliveryToDto(delivery, customer.Name, warehouse.Name);
    }

    public async Task<List<DeliveryNoteDto>> GetDeliveryNotesAsync(CancellationToken ct = default)
    {
        var deliveries = await _deliveryRepo.GetAllAsync(ct);
        var customers = (await _customerRepo.GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var warehouses = (await _warehouseRepo.GetAllAsync(ct)).ToDictionary(w => w.Id, w => w.Name);

        return deliveries.Select(d => MapDeliveryToDto(
            d,
            customers.GetValueOrDefault(d.CustomerId, "Məlum deyil"),
            warehouses.GetValueOrDefault(d.WarehouseId, "Məlum deyil")))
            .OrderByDescending(d => d.DeliveryDate)
            .ToList();
    }

    public async Task<DeliveryNoteDto?> GetDeliveryNoteByIdAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var delivery = await _deliveryRepo.GetByIdAsync(deliveryId, ct);
        if (delivery == null) return null;

        var customer = await _customerRepo.GetByIdAsync(delivery.CustomerId, ct);
        var warehouse = await _warehouseRepo.GetByIdAsync(delivery.WarehouseId, ct);

        return MapDeliveryToDto(delivery, customer?.Name ?? "Məlum deyil", warehouse?.Name ?? "Məlum deyil");
    }

    public async Task<DeliveryNoteDto> PostDeliveryNoteAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var delivery = await _deliveryRepo.GetByIdAsync(deliveryId, ct)
            ?? throw new BusinessRuleException("Mal göndərişi sənədi tapılmadı.");

        if (delivery.Status == DeliveryNoteStatus.Posted)
        {
            throw new BusinessRuleException("Bu göndəriş sənədi artıq icra edilib.");
        }

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        var allAccounts = await _accountRepo.GetAllAsync(ct);

        var stockAccountId = company?.DefaultStockAccountId
            ?? allAccounts.FirstOrDefault(a => a.Code == "1100" || a.Type == AccountType.Stock)?.Id
            ?? throw new MissingDefaultAccountException("Anbar (Stok) üçün default hesab tapılmadı.");

        var cogsAccountId = company?.DefaultCOGSAccountId
            ?? allAccounts.FirstOrDefault(a => a.Code == "7010" || a.Type == AccountType.COGS || a.Subcategory == AccountSubcategory.CostOfGoodsSold)?.Id
            ?? throw new MissingDefaultAccountException("Satışın Maya Dəyəri (COGS) üçün default hesab tapılmadı.");

        decimal totalIssuedCost = 0;

        foreach (var line in delivery.Lines)
        {
            var item = await _itemRepo.GetByIdAsync(line.ItemId, ct)
                ?? throw new BusinessRuleException($"Məhsul tapılmadı: {line.Description}");

            var unitCost = item.CurrentValuationRate;
            var lineTotalCost = line.Quantity * unitCost;

            line.UnitCost = unitCost;
            line.TotalCost = lineTotalCost;
            totalIssuedCost += lineTotalCost;
        }

        delivery.TotalCost = totalIssuedCost;

        // Accounting GL batch:
        // Dr: Satışın Maya Dəyəri (COGS)
        // Cr: Mallar və Materiallar (Stok)
        if (totalIssuedCost > 0)
        {
            var batch = new PostingBatch
            {
                TenantId = tenantId,
                SourceDocumentType = DocumentType.DeliveryNote,
                SourceDocumentId = delivery.Id,
                SourceDocumentNumber = delivery.DeliveryNumber,
                PostingDate = delivery.DeliveryDate.Date,
                Description = $"Mal Təhvili / Göndərişi - {delivery.DeliveryNumber}"
            };

            batch.Entries.Add(new LedgerEntry
            {
                TenantId = tenantId,
                AccountId = cogsAccountId,
                PostingDate = delivery.DeliveryDate.Date,
                SourceDocumentType = DocumentType.DeliveryNote,
                SourceDocumentId = delivery.Id,
                DebitBase = totalIssuedCost,
                CreditBase = 0,
                TransactionCurrency = "AZN",
                TransactionAmount = totalIssuedCost,
                ExchangeRate = 1.0m,
                LineDescription = $"Satışın Maya Dəyəri (COGS) çıxışı: {delivery.DeliveryNumber}"
            });

            batch.Entries.Add(new LedgerEntry
            {
                TenantId = tenantId,
                AccountId = stockAccountId,
                PostingDate = delivery.DeliveryDate.Date,
                SourceDocumentType = DocumentType.DeliveryNote,
                SourceDocumentId = delivery.Id,
                DebitBase = 0,
                CreditBase = totalIssuedCost,
                TransactionCurrency = "AZN",
                TransactionAmount = -totalIssuedCost,
                ExchangeRate = 1.0m,
                LineDescription = $"Anbar stok çıxışı: {delivery.DeliveryNumber}"
            });

            await _postingEngine.PostBatchAsync(batch, ct);
        }

        delivery.Status = DeliveryNoteStatus.Posted;
        delivery.PostingDate = DateTime.UtcNow;

        // If linked to SalesOrder, update delivered quantities
        if (delivery.SalesOrderId.HasValue)
        {
            var order = await _salesOrderRepo.GetByIdAsync(delivery.SalesOrderId.Value, ct);
            if (order != null)
            {
                bool allDelivered = true;
                foreach (var delLine in delivery.Lines)
                {
                    var ordLine = order.Lines.FirstOrDefault(ol => ol.ItemId == delLine.ItemId);
                    if (ordLine != null)
                    {
                        ordLine.DeliveredQuantity += delLine.Quantity;
                        if (ordLine.DeliveredQuantity < ordLine.Quantity)
                        {
                            allDelivered = false;
                        }
                    }
                }
                order.Status = allDelivered ? SalesOrderStatus.FullyDelivered : SalesOrderStatus.PartiallyDelivered;
                await _salesOrderRepo.UpdateAsync(order, ct);
            }
        }

        await _deliveryRepo.UpdateAsync(delivery, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepo.GetByIdAsync(delivery.CustomerId, ct);
        var warehouse = await _warehouseRepo.GetByIdAsync(delivery.WarehouseId, ct);
        return MapDeliveryToDto(delivery, customer?.Name ?? "Məlum deyil", warehouse?.Name ?? "Məlum deyil");
    }

    private static SalesOrderDto MapSalesOrderToDto(SalesOrder o, string customerName)
    {
        return new SalesOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = customerName,
            OrderDate = o.OrderDate,
            ExpectedDeliveryDate = o.ExpectedDeliveryDate,
            Status = o.Status,
            StatusName = o.Status switch
            {
                SalesOrderStatus.Draft => "Qaralama",
                SalesOrderStatus.Confirmed => "Təsdiqlənib",
                SalesOrderStatus.PartiallyDelivered => "Qismən göndərilib",
                SalesOrderStatus.FullyDelivered => "Tam göndərilib",
                SalesOrderStatus.Invoiced => "Qaimələşdirilib",
                SalesOrderStatus.Cancelled => "Ləğv edilib",
                _ => o.Status.ToString()
            },
            Currency = o.Currency,
            SubTotal = o.SubTotal,
            TaxTotal = o.TaxTotal,
            GrandTotal = o.GrandTotal,
            CustomerReference = o.CustomerReference,
            Notes = o.Notes,
            Lines = o.Lines.Select(l => new SalesOrderLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                DeliveredQuantity = l.DeliveredQuantity,
                InvoicedQuantity = l.InvoicedQuantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                TaxRate = l.TaxRate,
                LineTotal = l.LineTotal
            }).ToList()
        };
    }

    private static DeliveryNoteDto MapDeliveryToDto(DeliveryNote d, string customerName, string warehouseName)
    {
        return new DeliveryNoteDto
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            SalesOrderId = d.SalesOrderId,
            CustomerId = d.CustomerId,
            CustomerName = customerName,
            WarehouseId = d.WarehouseId,
            WarehouseName = warehouseName,
            DeliveryDate = d.DeliveryDate,
            PostingDate = d.PostingDate,
            Status = d.Status,
            StatusName = d.Status switch
            {
                DeliveryNoteStatus.Draft => "Qaralama",
                DeliveryNoteStatus.Posted => "İcra edilib",
                DeliveryNoteStatus.Invoiced => "Qaimələşdirilib",
                DeliveryNoteStatus.Cancelled => "Ləğv edilib",
                _ => d.Status.ToString()
            },
            DriverName = d.DriverName,
            VehicleNumber = d.VehicleNumber,
            TrackingNumber = d.TrackingNumber,
            TotalCost = d.TotalCost,
            Notes = d.Notes,
            Lines = d.Lines.Select(l => new DeliveryNoteLineDto
            {
                Id = l.Id,
                SalesOrderLineId = l.SalesOrderLineId,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                TotalCost = l.TotalCost
            }).ToList()
        };
    }
}
