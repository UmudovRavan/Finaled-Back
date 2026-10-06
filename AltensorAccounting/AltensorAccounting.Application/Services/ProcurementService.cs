using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Application.Services.Procurement;
using AltensorAccounting.Application.Services.Valuation;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

namespace AltensorAccounting.Application.Services;

public class ProcurementService : IProcurementService
{
    private readonly IGenericRepository<Supplier> _supplierRepo;
    private readonly IGenericRepository<PurchaseOrder> _poRepo;
    private readonly IGenericRepository<GoodsReceipt> _grnRepo;
    private readonly IGenericRepository<SupplierInvoice> _invoiceRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IThreeWayMatchService _matchService;
    private readonly IStockValuationEngine _stockEngine;
    private readonly IPostingEngine _postingEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Logging.ILogger<ProcurementService> _logger;

    public ProcurementService(
        IGenericRepository<Supplier> supplierRepo,
        IGenericRepository<PurchaseOrder> poRepo,
        IGenericRepository<GoodsReceipt> grnRepo,
        IGenericRepository<SupplierInvoice> invoiceRepo,
        IGenericRepository<Company> companyRepo,
        IGenericRepository<Account> accountRepo,
        IThreeWayMatchService matchService,
        IStockValuationEngine stockEngine,
        IPostingEngine postingEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        Microsoft.Extensions.Logging.ILogger<ProcurementService> logger)
    {
        _supplierRepo = supplierRepo;
        _poRepo = poRepo;
        _grnRepo = grnRepo;
        _invoiceRepo = invoiceRepo;
        _companyRepo = companyRepo;
        _accountRepo = accountRepo;
        _matchService = matchService;
        _stockEngine = stockEngine;
        _postingEngine = postingEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var supplier = new Supplier
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            TaxNumber = dto.TaxNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            PayableAccountId = (dto.PayableAccountId.HasValue && dto.PayableAccountId.Value != Guid.Empty) ? dto.PayableAccountId : null,
            PaymentTermsDays = dto.PaymentTermsDays
        };

        await _supplierRepo.AddAsync(supplier, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SupplierDto
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            TaxNumber = supplier.TaxNumber,
            Email = supplier.Email,
            PayableAccountId = supplier.PayableAccountId,
            OutstandingPayable = 0
        };
    }

    public async Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default)
    {
        var suppliers = await _supplierRepo.GetAllAsync(ct);
        return suppliers.Select(s => new SupplierDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            TaxNumber = s.TaxNumber,
            Email = s.Email,
            PayableAccountId = s.PayableAccountId
        }).OrderBy(s => s.Code).ToList();
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, ct)
            ?? throw new BusinessRuleException("Təchizatçı tapılmadı.");

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            throw new BusinessRuleException("Sifarişdə ən azı bir sətir olmalıdır.");
        }

        foreach (var l in dto.Lines)
        {
            if (l.OrderedQuantity <= 0)
                throw new BusinessRuleException("Sifariş sayı 0-dan böyük olmalıdır.");
            if (l.UnitPrice < 0)
                throw new BusinessRuleException("Vahid qiymət mənfi ola bilməz.");
        }

        var subTotal = dto.Lines.Sum(l => (l.OrderedQuantity * l.UnitPrice) * (1 - (l.DiscountPercent / 100m)));
        var taxTotal = subTotal * 0.18m;
        var grandTotal = subTotal + taxTotal;

        var po = new PurchaseOrder
        {
            TenantId = tenantId,
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            SupplierId = supplier.Id,
            OrderDate = DateTime.SpecifyKind(dto.OrderDate, DateTimeKind.Utc),
            ExpectedDeliveryDate = DateTime.SpecifyKind(dto.ExpectedDeliveryDate, DateTimeKind.Utc),
            Status = PurchaseOrderStatus.Draft,
            Currency = dto.Currency,
            ExchangeRate = dto.ExchangeRate,
            SubTotal = subTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            TermsAndConditions = dto.TermsAndConditions
        };

        foreach (var l in dto.Lines)
        {
            var lineSub = (l.OrderedQuantity * l.UnitPrice) * (1 - (l.DiscountPercent / 100m));
            var lineTax = lineSub * (l.TaxPercent / 100m);

            po.Lines.Add(new PurchaseOrderLine
            {
                TenantId = tenantId,
                ItemId = l.ItemId,
                Description = l.Description,
                OrderedQuantity = l.OrderedQuantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineSubTotal = lineSub,
                TaxPercent = l.TaxPercent,
                TaxAmount = lineTax,
                LineTotal = lineSub + lineTax,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId
            });
        }

        await _poRepo.AddAsync(po, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PurchaseOrderDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            OrderDate = po.OrderDate,
            Status = po.Status,
            GrandTotal = po.GrandTotal
        };
    }

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _poRepo.GetAllAsync(ct);
        var suppliers = (await _supplierRepo.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        return orders.Select(po => new PurchaseOrderDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            SupplierId = po.SupplierId,
            SupplierName = suppliers.TryGetValue(po.SupplierId, out var sName) ? sName : string.Empty,
            OrderDate = po.OrderDate,
            Status = po.Status,
            GrandTotal = po.GrandTotal
        }).OrderByDescending(po => po.OrderDate).ToList();
    }

    public async Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var po = await _poRepo.GetByIdAsync(orderId, ct, p => p.Lines)
            ?? throw new BusinessRuleException("Sifariş (PO) tapılmadı.");

        po.Status = PurchaseOrderStatus.Approved;
        await _poRepo.UpdateAsync(po, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var supplier = await _supplierRepo.GetByIdAsync(po.SupplierId, ct);

        return new PurchaseOrderDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            SupplierId = po.SupplierId,
            SupplierName = supplier?.Name ?? "",
            OrderDate = po.OrderDate,
            Status = po.Status,
            GrandTotal = po.GrandTotal
        };
    }

    public async Task<List<GoodsReceiptDto>> GetGoodsReceiptsAsync(CancellationToken ct = default)
    {
        var receipts = await _grnRepo.GetAllAsync(ct);
        var suppliers = (await _supplierRepo.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        return receipts.Select(grn => new GoodsReceiptDto
        {
            Id = grn.Id,
            ReceiptNumber = grn.ReceiptNumber,
            SupplierId = grn.SupplierId,
            SupplierName = suppliers.TryGetValue(grn.SupplierId, out var sName) ? sName : string.Empty,
            ReceiptDate = grn.ReceiptDate,
            PostingDate = grn.PostingDate,
            Status = grn.Status,
            TotalValue = grn.TotalValue
        }).OrderByDescending(grn => grn.ReceiptDate).ToList();
    }

    public async Task<GoodsReceiptDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, ct)
            ?? throw new BusinessRuleException("Təchizatçı tapılmadı.");

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            throw new BusinessRuleException("Qəbul sənədində ən azı bir sətir olmalıdır.");
        }

        foreach (var l in dto.Lines)
        {
            if (l.ReceivedQuantity <= 0)
                throw new BusinessRuleException("Qəbul edilən say 0-dan böyük olmalıdır.");
            if (l.UnitCost < 0)
                throw new BusinessRuleException("Vahid maya dəyəri mənfi ola bilməz.");
        }

        var totalValue = dto.Lines.Sum(l => l.ReceivedQuantity * l.UnitCost);

        var grn = new GoodsReceipt
        {
            TenantId = tenantId,
            ReceiptNumber = $"GRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            SupplierId = supplier.Id,
            PurchaseOrderId = (dto.PurchaseOrderId.HasValue && dto.PurchaseOrderId.Value != Guid.Empty) ? dto.PurchaseOrderId : null,
            ReceiptDate = DateTime.SpecifyKind(dto.ReceiptDate, DateTimeKind.Utc),
            PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
            WarehouseId = dto.WarehouseId,
            WaybillNumber = dto.WaybillNumber,
            TotalValue = totalValue,
            Status = DocumentStatus.Draft
        };

        foreach (var l in dto.Lines)
        {
            grn.Lines.Add(new GoodsReceiptLine
            {
                TenantId = tenantId,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ItemId = l.ItemId,
                Description = string.IsNullOrWhiteSpace(l.Description) ? "Goods Receipt Item" : l.Description,
                ReceivedQuantity = l.ReceivedQuantity,
                UnitCost = l.UnitCost,
                TotalCost = l.ReceivedQuantity * l.UnitCost,
                WarehouseId = dto.WarehouseId
            });
        }

        await _grnRepo.AddAsync(grn, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new GoodsReceiptDto
        {
            Id = grn.Id,
            ReceiptNumber = grn.ReceiptNumber,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            ReceiptDate = grn.ReceiptDate,
            PostingDate = grn.PostingDate,
            Status = grn.Status,
            TotalValue = grn.TotalValue
        };
    }

    public async Task<GoodsReceiptDto> PostGoodsReceiptAsync(Guid receiptId, CancellationToken ct = default)
    {
        var grn = await _grnRepo.GetByIdAsync(receiptId, ct, g => g.Lines)
            ?? throw new BusinessRuleException("Qəbul sənədi tapılmadı.");

        if (grn.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(grn.ReceiptNumber);
        }

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var inventoryAccountId = company.DefaultStockAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "1100" || a.Type == AccountType.Stock) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new BusinessRuleException("Anbar (Inventory) hesabı təyin edilməyib.");

        var grniAccountId = company.DefaultGRNIAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "2200" || a.Type == AccountType.GRNI) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new BusinessRuleException("GRNI / Accrued Purchases hesabı təyin edilməyib.");

        // 1. Process Stock Ledger movements for each line via Valuation Engine
        foreach (var line in grn.Lines)
        {
            await _stockEngine.ProcessIncomingStockAsync(
                line.ItemId,
                line.WarehouseId ?? grn.WarehouseId,
                line.ReceivedQuantity,
                line.UnitCost,
                DocumentType.GoodsReceipt,
                grn.Id,
                grn.ReceiptNumber,
                grn.PostingDate,
                ct);
        }

        // 2. Post to General Ledger:
        // Dr Inventory Asset (TotalValue)
        // Cr GRNI / Accrued Purchases (TotalValue)
        var batch = new PostingBatch
        {
            SourceDocumentType = DocumentType.GoodsReceipt,
            SourceDocumentId = grn.Id,
            SourceDocumentNumber = grn.ReceiptNumber,
            PostingDate = grn.PostingDate,
            Description = $"Goods Receipt {grn.ReceiptNumber} (GRNI Accrual)"
        };

        // Dr Inventory
        batch.Entries.Add(new LedgerEntry
        {
            AccountId = inventoryAccountId,
            DebitBase = grn.TotalValue,
            CreditBase = 0,
            TransactionAmount = grn.TotalValue,
            LineDescription = $"Inventory Receipt {grn.ReceiptNumber}"
        });

        // Cr GRNI
        batch.Entries.Add(new LedgerEntry
        {
            AccountId = grniAccountId,
            DebitBase = 0,
            CreditBase = grn.TotalValue,
            TransactionAmount = -grn.TotalValue,
            PartyId = grn.SupplierId,
            PartyType = "Supplier",
            LineDescription = $"GRNI Liability for {grn.ReceiptNumber}"
        });

        await _postingEngine.PostBatchAsync(batch, ct);

        grn.Status = DocumentStatus.Posted;
        await _grnRepo.UpdateAsync(grn, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var supplier = await _supplierRepo.GetByIdAsync(grn.SupplierId, ct);

        return new GoodsReceiptDto
        {
            Id = grn.Id,
            ReceiptNumber = grn.ReceiptNumber,
            SupplierId = grn.SupplierId,
            SupplierName = supplier?.Name ?? "",
            ReceiptDate = grn.ReceiptDate,
            PostingDate = grn.PostingDate,
            Status = grn.Status,
            TotalValue = grn.TotalValue
        };
    }

    public async Task<List<SupplierInvoiceDto>> GetSupplierInvoicesAsync(CancellationToken ct = default)
    {
        var invoices = await _invoiceRepo.GetAllAsync(ct);
        var suppliers = (await _supplierRepo.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        return invoices.Select(inv => new SupplierInvoiceDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            SupplierInvoiceNumber = inv.SupplierInvoiceNumber,
            SupplierId = inv.SupplierId,
            SupplierName = suppliers.TryGetValue(inv.SupplierId, out var sName) ? sName : string.Empty,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            PostingDate = inv.PostingDate,
            DocumentStatus = inv.DocumentStatus,
            SettlementStatus = inv.SettlementStatus,
            ThreeWayMatchStatus = inv.ThreeWayMatchStatus,
            SubTotal = inv.SubTotal,
            TaxTotal = inv.TaxTotal,
            GrandTotal = inv.GrandTotal,
            PaidAmount = inv.PaidAmount,
            OutstandingAmount = inv.OutstandingAmount
        }).OrderByDescending(inv => inv.InvoiceDate).ToList();
    }

    public async Task<SupplierInvoiceDto> CreateSupplierInvoiceAsync(CreateSupplierInvoiceDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var supplier = await _supplierRepo.GetByIdAsync(dto.SupplierId, ct)
            ?? throw new BusinessRuleException("Təchizatçı tapılmadı.");

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            throw new BusinessRuleException("Alış fakturasında ən azı bir sətir olmalıdır.");
        }

        foreach (var l in dto.Lines)
        {
            if (l.Quantity <= 0)
                throw new BusinessRuleException("Faktura sayı 0-dan böyük olmalıdır.");
            if (l.UnitPrice < 0)
                throw new BusinessRuleException("Vahid qiymət mənfi ola bilməz.");
        }

        var supplierInvoiceNumber = !string.IsNullOrWhiteSpace(dto.SupplierInvoiceNumber)
            ? dto.SupplierInvoiceNumber
            : (!string.IsNullOrWhiteSpace(dto.SupplierInvoiceReference)
                ? dto.SupplierInvoiceReference
                : $"PINV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}");

        // Duplicate Invoice Check (Supplier + InvoiceNumber)
        var exists = await _invoiceRepo.ExistsAsync(i => 
            i.SupplierId == dto.SupplierId && 
            i.SupplierInvoiceNumber == supplierInvoiceNumber, ct);

        if (exists)
        {
            throw new BusinessRuleException($"Bu təchizatçı üzrə '{supplierInvoiceNumber}' nömrəli faktura artıq mövcuddur.");
        }

        var subTotal = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var taxTotal = subTotal * 0.18m;
        var grandTotal = subTotal + taxTotal;

        var invoice = new SupplierInvoice
        {
            TenantId = tenantId,
            InvoiceNumber = $"PINV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            SupplierInvoiceNumber = supplierInvoiceNumber,
            SupplierId = supplier.Id,
            PurchaseOrderId = (dto.PurchaseOrderId.HasValue && dto.PurchaseOrderId.Value != Guid.Empty) ? dto.PurchaseOrderId : null,
            GoodsReceiptId = (dto.GoodsReceiptId.HasValue && dto.GoodsReceiptId.Value != Guid.Empty) ? dto.GoodsReceiptId : null,
            InvoiceDate = DateTime.SpecifyKind(dto.InvoiceDate, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
            DocumentStatus = DocumentStatus.Draft,
            SettlementStatus = SettlementStatus.Unpaid,
            ThreeWayMatchStatus = ThreeWayMatchStatus.Pending,
            Currency = dto.Currency,
            ExchangeRate = dto.ExchangeRate,
            SubTotal = subTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            OutstandingAmount = grandTotal,
            IsGRNIBased = dto.IsGRNIBased,
            Notes = dto.Notes
        };

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        var defaultExpenseAccountId = (company?.DefaultStockAccountId.HasValue == true && company.DefaultStockAccountId.Value != Guid.Empty)
            ? company.DefaultStockAccountId
            : ((company?.DefaultGRNIAccountId.HasValue == true && company.DefaultGRNIAccountId.Value != Guid.Empty) ? company.DefaultGRNIAccountId : null);

        if (!defaultExpenseAccountId.HasValue)
        {
            var fallbackAccount = (await _accountRepo.GetAllAsync(ct))
                .FirstOrDefault(a => (a.Category == AccountCategory.Expense || a.Category == AccountCategory.Asset) && a.IsLeaf && a.IsActive);
            defaultExpenseAccountId = fallbackAccount?.Id;
        }

        foreach (var l in dto.Lines)
        {
            var lineSub = l.Quantity * l.UnitPrice;
            var lineTax = lineSub * 0.18m;
            var accountId = (l.ExpenseOrAssetAccountId.HasValue && l.ExpenseOrAssetAccountId.Value != Guid.Empty)
                ? l.ExpenseOrAssetAccountId.Value
                : (defaultExpenseAccountId.HasValue && defaultExpenseAccountId.Value != Guid.Empty ? defaultExpenseAccountId : null);

            if (!accountId.HasValue || accountId == Guid.Empty)
            {
                throw new BusinessRuleException("Alış qaiməsi üçün xərc/stok hesabı (Expense/Asset Account) təyin edilməyib və standart hesab tapılmadı.");
            }

            invoice.Lines.Add(new SupplierInvoiceLine
            {
                TenantId = tenantId,
                ItemId = (l.ItemId.HasValue && l.ItemId.Value != Guid.Empty) ? l.ItemId : null,
                Description = string.IsNullOrWhiteSpace(l.Description) ? "Supplier Invoice Item" : l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineSubTotal = lineSub,
                TaxCodeId = (l.TaxCodeId.HasValue && l.TaxCodeId.Value != Guid.Empty) ? l.TaxCodeId : null,
                TaxAmount = lineTax,
                LineTotal = lineSub + lineTax,
                ExpenseOrAssetAccountId = accountId,
                CostCenterId = (l.CostCenterId.HasValue && l.CostCenterId.Value != Guid.Empty) ? l.CostCenterId : null,
                ProjectId = (l.ProjectId.HasValue && l.ProjectId.Value != Guid.Empty) ? l.ProjectId : null,
                DepartmentId = (l.DepartmentId.HasValue && l.DepartmentId.Value != Guid.Empty) ? l.DepartmentId : null
            });
        }

        // Run 3-Way Match Check before saving to avoid duplicate SaveChanges & UpdateAsync tracking issues
        var matchResult = await _matchService.EvaluateMatchAsync(invoice, ct);
        invoice.ThreeWayMatchStatus = matchResult.Status;

        await _invoiceRepo.AddAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SupplierInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SupplierInvoiceNumber = invoice.SupplierInvoiceNumber,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            DocumentStatus = invoice.DocumentStatus,
            SettlementStatus = invoice.SettlementStatus,
            ThreeWayMatchStatus = invoice.ThreeWayMatchStatus,
            SubTotal = invoice.SubTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = 0,
            OutstandingAmount = invoice.OutstandingAmount
        };
    }

    public async Task<ThreeWayMatchResultDto> EvaluateThreeWayMatchAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ct, i => i.Lines)
            ?? throw new BusinessRuleException("Faktura tapılmadı.");

        return await _matchService.EvaluateMatchAsync(invoice, ct);
    }

    public async Task<SupplierInvoiceDto> PostSupplierInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ct, i => i.Lines)
            ?? throw new BusinessRuleException("Faktura tapılmadı.");

        if (invoice.DocumentStatus == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(invoice.InvoiceNumber);
        }

        if (invoice.ThreeWayMatchStatus == ThreeWayMatchStatus.OnHoldToleranceExceeded)
        {
            throw new BusinessRuleException("3-Way Match toleransı aşıldığı üçün bu fakturanı birbaşa post etmək olmaz. Təsdiq tələb olunur.");
        }

        var supplier = await _supplierRepo.GetByIdAsync(invoice.SupplierId, ct)
            ?? throw new BusinessRuleException("Təchizatçı tapılmadı.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var apAccountId = supplier.PayableAccountId 
            ?? company.DefaultPayableAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "2100" || a.Type == AccountType.Payable) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new BusinessRuleException("Kreditor borclar (AP) hesabı təyin edilməyib.");

        var vatAccountId = company.DefaultInputVatAccountId
            ?? (await _accountRepo.FindAsync(a => a.Code == "1250" && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? (await _accountRepo.FindAsync(a => a.Type == AccountType.Tax && a.Category == AccountCategory.Asset && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? (await _accountRepo.FindAsync(a => (a.Code == "1250" || a.Type == AccountType.Tax) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new BusinessRuleException("Əvəzləşdirilən ƏDV hesabı təyin edilməyib.");

        var grniAccountId = company.DefaultGRNIAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "2200" || a.Type == AccountType.GRNI) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new BusinessRuleException("GRNI hesabı təyin edilməyib.");

        // Post to GL:
        // If GRNI-based:
        // Dr GRNI (SubTotal)
        // Dr Input VAT (TaxTotal)
        // Cr Accounts Payable (GrandTotal)
        // If direct expense/asset:
        // Dr Expense/Asset (SubTotal)
        // Dr Input VAT (TaxTotal)
        // Cr Accounts Payable (GrandTotal)
        var batch = new PostingBatch
        {
            SourceDocumentType = DocumentType.SupplierInvoice,
            SourceDocumentId = invoice.Id,
            SourceDocumentNumber = invoice.InvoiceNumber,
            PostingDate = invoice.PostingDate,
            Description = $"Supplier Invoice {invoice.SupplierInvoiceNumber} - {supplier.Name}"
        };

        // Debits
        if (invoice.IsGRNIBased)
        {
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = grniAccountId,
                DebitBase = invoice.SubTotal * invoice.ExchangeRate,
                CreditBase = 0,
                TransactionCurrency = invoice.Currency,
                TransactionAmount = invoice.SubTotal,
                ExchangeRate = invoice.ExchangeRate,
                PartyId = supplier.Id,
                PartyType = "Supplier",
                LineDescription = $"Clear GRNI for {invoice.SupplierInvoiceNumber}"
            });
        }
        else
        {
            foreach (var line in invoice.Lines)
            {
                var expenseAccountId = (line.ExpenseOrAssetAccountId.HasValue && line.ExpenseOrAssetAccountId.Value != Guid.Empty)
                    ? line.ExpenseOrAssetAccountId.Value
                    : (company.DefaultStockAccountId ?? grniAccountId);

                batch.Entries.Add(new LedgerEntry
                {
                    AccountId = expenseAccountId,
                    DebitBase = line.LineSubTotal * invoice.ExchangeRate,
                    CreditBase = 0,
                    TransactionCurrency = invoice.Currency,
                    TransactionAmount = line.LineSubTotal,
                    ExchangeRate = invoice.ExchangeRate,
                    CostCenterId = line.CostCenterId,
                    ProjectId = line.ProjectId,
                    LineDescription = line.Description
                });
            }
        }

        // Dr Input VAT
        if (invoice.TaxTotal > 0)
        {
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = vatAccountId,
                DebitBase = invoice.TaxTotal * invoice.ExchangeRate,
                CreditBase = 0,
                TransactionCurrency = invoice.Currency,
                TransactionAmount = invoice.TaxTotal,
                ExchangeRate = invoice.ExchangeRate,
                LineDescription = $"Input VAT on Invoice {invoice.SupplierInvoiceNumber}"
            });
        }

        // Cr Accounts Payable (GrandTotal)
        batch.Entries.Add(new LedgerEntry
        {
            AccountId = apAccountId,
            DebitBase = 0,
            CreditBase = invoice.GrandTotal * invoice.ExchangeRate,
            TransactionCurrency = invoice.Currency,
            TransactionAmount = -invoice.GrandTotal,
            ExchangeRate = invoice.ExchangeRate,
            PartyId = supplier.Id,
            PartyType = "Supplier",
            LineDescription = $"Payable to {supplier.Name}"
        });

        await _postingEngine.PostBatchAsync(batch, ct);

        invoice.DocumentStatus = DocumentStatus.Posted;
        await _invoiceRepo.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SupplierInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SupplierInvoiceNumber = invoice.SupplierInvoiceNumber,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            DocumentStatus = invoice.DocumentStatus,
            SettlementStatus = invoice.SettlementStatus,
            ThreeWayMatchStatus = invoice.ThreeWayMatchStatus,
            SubTotal = invoice.SubTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount
        };
    }
}
