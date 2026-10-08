using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Entities.Treasury;

namespace AltensorAccounting.Application.Services;

public class AccountingService : IAccountingService
{
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IGenericRepository<FiscalYear> _yearRepo;
    private readonly IGenericRepository<AccountingPeriod> _periodRepo;
    private readonly IGenericRepository<ManualJournal> _journalRepo;
    private readonly IGenericRepository<Customer> _customerRepo;
    private readonly IGenericRepository<CustomerInvoice> _invoiceRepo;
    private readonly IGenericRepository<SupplierInvoice> _suppInvoiceRepo;
    private readonly IGenericRepository<Payment> _paymentRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IGenericRepository<BankAccount> _bankRepo;
    private readonly IGenericRepository<CashDesk> _cashRepo;
    private readonly IPostingEngine _postingEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantSeeder _tenantSeeder;
    private readonly Microsoft.Extensions.Logging.ILogger<AccountingService> _logger;

    public AccountingService(
        IGenericRepository<Account> accountRepo,
        IGenericRepository<FiscalYear> yearRepo,
        IGenericRepository<AccountingPeriod> periodRepo,
        IGenericRepository<ManualJournal> journalRepo,
        IGenericRepository<Customer> customerRepo,
        IGenericRepository<CustomerInvoice> invoiceRepo,
        IGenericRepository<SupplierInvoice> suppInvoiceRepo,
        IGenericRepository<Payment> paymentRepo,
        IGenericRepository<Company> companyRepo,
        IGenericRepository<BankAccount> bankRepo,
        IGenericRepository<CashDesk> cashRepo,
        IPostingEngine postingEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        ITenantSeeder tenantSeeder,
        Microsoft.Extensions.Logging.ILogger<AccountingService> logger)
    {
        _accountRepo = accountRepo;
        _yearRepo = yearRepo;
        _periodRepo = periodRepo;
        _journalRepo = journalRepo;
        _customerRepo = customerRepo;
        _invoiceRepo = invoiceRepo;
        _suppInvoiceRepo = suppInvoiceRepo;
        _paymentRepo = paymentRepo;
        _companyRepo = companyRepo;
        _bankRepo = bankRepo;
        _cashRepo = cashRepo;
        _postingEngine = postingEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _tenantSeeder = tenantSeeder;
        _logger = logger;
    }

    // Chart of Accounts
    public async Task<List<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _accountRepo.GetAllAsync(ct);
        return accounts.Select(a => new AccountDto
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            Category = a.Category,
            Type = a.Type,
            ParentAccountId = a.ParentAccountId,
            IsLeaf = a.IsLeaf,
            IsControlAccount = a.IsControlAccount,
            IsActive = a.IsActive,
            Currency = a.Currency
        }).OrderBy(a => a.Code).ToList();
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        if (await _accountRepo.ExistsAsync(a => a.Code == dto.Code, ct))
        {
            throw new BusinessRuleException($"'{dto.Code}' kodlu hesab artıq mövcuddur.");
        }

        var account = new Account
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            Category = dto.Category,
            Type = dto.Type,
            ParentAccountId = dto.ParentAccountId,
            IsControlAccount = dto.IsControlAccount,
            IsLeaf = true,
            Currency = dto.Currency
        };

        if (dto.ParentAccountId.HasValue)
        {
            var parent = await _accountRepo.GetByIdAsync(dto.ParentAccountId.Value, ct);
            if (parent != null && parent.IsLeaf)
            {
                parent.IsLeaf = false; // Parent cannot be leaf anymore
                await _accountRepo.UpdateAsync(parent, ct);
            }
        }

        await _accountRepo.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AccountDto
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            Category = account.Category,
            Type = account.Type,
            ParentAccountId = account.ParentAccountId,
            IsLeaf = account.IsLeaf,
            IsControlAccount = account.IsControlAccount,
            Currency = account.Currency
        };
    }

    // Fiscal Periods
    public async Task<FiscalYearDto> CreateFiscalYearAsync(CreateFiscalYearDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var year = new FiscalYear
        {
            TenantId = tenantId,
            Name = dto.Name,
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc),
            IsClosed = false
        };

        // Create 12 monthly periods automatically
        for (int i = 1; i <= 12; i++)
        {
            var periodStart = DateTime.SpecifyKind(new DateTime(dto.StartDate.Year, i, 1), DateTimeKind.Utc);
            var periodEnd = DateTime.SpecifyKind(periodStart.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

            year.Periods.Add(new AccountingPeriod
            {
                TenantId = tenantId,
                Name = $"{dto.StartDate.Year}-{i:D2}",
                PeriodNumber = i,
                StartDate = periodStart,
                EndDate = periodEnd,
                Status = FiscalPeriodStatus.Open
            });
        }

        await _yearRepo.AddAsync(year, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new FiscalYearDto
        {
            Id = year.Id,
            Name = year.Name,
            StartDate = year.StartDate,
            EndDate = year.EndDate,
            IsClosed = year.IsClosed,
            Periods = year.Periods.Select(p => new AccountingPeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                PeriodNumber = p.PeriodNumber,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status
            }).ToList()
        };
    }

    public async Task<List<FiscalYearDto>> GetFiscalYearsAsync(CancellationToken ct = default)
    {
        var years = await _yearRepo.GetAllAsync(ct);
        return years.Select(y => new FiscalYearDto
        {
            Id = y.Id,
            Name = y.Name,
            StartDate = y.StartDate,
            EndDate = y.EndDate,
            IsClosed = y.IsClosed,
            Periods = y.Periods.Select(p => new AccountingPeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                PeriodNumber = p.PeriodNumber,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status
            }).ToList()
        }).ToList();
    }

    public async Task ClosePeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await _periodRepo.GetByIdAsync(periodId, ct)
            ?? throw new BusinessRuleException("Maliyyə dövrü tapılmadı.");

        period.Status = FiscalPeriodStatus.Closed;
        await _periodRepo.UpdateAsync(period, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // Manual Journals
    public async Task<ManualJournalDto> CreateManualJournalAsync(CreateManualJournalDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
        {
            throw new PostingUnbalancedException(totalDebit, totalCredit);
        }

        var journal = new ManualJournal
        {
            TenantId = tenantId,
            JournalNumber = $"JV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
            ReferenceNumber = dto.ReferenceNumber,
            Description = dto.Description,
            Status = DocumentStatus.Draft,
            TotalAmount = totalDebit
        };

        foreach (var l in dto.Lines)
        {
            journal.Lines.Add(new ManualJournalLine
            {
                TenantId = tenantId,
                AccountId = l.AccountId,
                Debit = l.Debit,
                Credit = l.Credit,
                Currency = l.Currency,
                ExchangeRate = l.ExchangeRate,
                PartyId = l.PartyId,
                PartyType = l.PartyType,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId,
                DepartmentId = l.DepartmentId,
                Description = l.Description
            });
        }

        await _journalRepo.AddAsync(journal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ManualJournalDto
        {
            Id = journal.Id,
            JournalNumber = journal.JournalNumber,
            PostingDate = journal.PostingDate,
            Description = journal.Description,
            Status = journal.Status,
            TotalAmount = journal.TotalAmount,
            Lines = dto.Lines
        };
    }

    public async Task<ManualJournalDto> PostManualJournalAsync(Guid journalId, CancellationToken ct = default)
    {
        var journal = await _journalRepo.GetByIdAsync(journalId, ct, j => j.Lines)
            ?? throw new BusinessRuleException("Journal tapılmadı.");

        if (journal.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(journal.JournalNumber);
        }

        // Post to General Ledger via PostingEngine
        var batch = new PostingBatch
        {
            SourceDocumentType = DocumentType.ManualJournal,
            SourceDocumentId = journal.Id,
            SourceDocumentNumber = journal.JournalNumber,
            PostingDate = journal.PostingDate,
            Description = journal.Description
        };

        foreach (var l in journal.Lines)
        {
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = l.AccountId,
                DebitBase = l.Debit * l.ExchangeRate,
                CreditBase = l.Credit * l.ExchangeRate,
                TransactionCurrency = l.Currency,
                TransactionAmount = l.Debit > 0 ? l.Debit : -l.Credit,
                ExchangeRate = l.ExchangeRate,
                PartyId = l.PartyId,
                PartyType = l.PartyType,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId,
                DepartmentId = l.DepartmentId,
                LineDescription = l.Description ?? journal.Description
            });
        }

        await _postingEngine.PostBatchAsync(batch, ct);

        journal.Status = DocumentStatus.Posted;
        await _journalRepo.UpdateAsync(journal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ManualJournalDto
        {
            Id = journal.Id,
            JournalNumber = journal.JournalNumber,
            PostingDate = journal.PostingDate,
            Description = journal.Description,
            Status = journal.Status,
            TotalAmount = journal.TotalAmount
        };
    }

    public async Task<ManualJournalDto> ReverseManualJournalAsync(Guid journalId, string reason, DateTime reversalDate, CancellationToken ct = default)
    {
        var journal = await _journalRepo.GetByIdAsync(journalId, ct, j => j.Lines)
            ?? throw new BusinessRuleException("Journal tapılmadı.");

        if (journal.Status != DocumentStatus.Posted)
        {
            throw new BusinessRuleException("Yalnız Posted statuslu journal reverse oluna bilər.");
        }

        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var utcReversalDate = DateTime.SpecifyKind(reversalDate, DateTimeKind.Utc);

        var reversalJournal = new ManualJournal
        {
            TenantId = tenantId,
            JournalNumber = $"REV-JV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            PostingDate = utcReversalDate,
            ReferenceNumber = journal.JournalNumber,
            Description = $"Reversal of {journal.JournalNumber}: {reason}",
            Status = DocumentStatus.Posted,
            TotalAmount = journal.TotalAmount,
            IsReversal = true,
            ReversalOfJournalId = journal.Id,
            ReversalReason = reason
        };

        foreach (var l in journal.Lines)
        {
            reversalJournal.Lines.Add(new ManualJournalLine
            {
                TenantId = tenantId,
                AccountId = l.AccountId,
                Debit = l.Credit,  // SWAPPED
                Credit = l.Debit,  // SWAPPED
                Currency = l.Currency,
                ExchangeRate = l.ExchangeRate,
                PartyId = l.PartyId,
                PartyType = l.PartyType,
                CostCenterId = l.CostCenterId,
                ProjectId = l.ProjectId,
                DepartmentId = l.DepartmentId,
                Description = $"Reversal: {l.Description}"
            });
        }

        await _journalRepo.AddAsync(reversalJournal, ct);

        // Reverse through posting engine
        var batch = new PostingBatch
        {
            SourceDocumentType = DocumentType.ManualJournal,
            SourceDocumentId = reversalJournal.Id,
            SourceDocumentNumber = reversalJournal.JournalNumber,
            PostingDate = utcReversalDate,
            Description = reversalJournal.Description
        };

        foreach (var l in reversalJournal.Lines)
        {
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = l.AccountId,
                DebitBase = l.Debit * l.ExchangeRate,
                CreditBase = l.Credit * l.ExchangeRate,
                TransactionCurrency = l.Currency,
                TransactionAmount = l.Debit > 0 ? l.Debit : -l.Credit,
                ExchangeRate = l.ExchangeRate,
                PartyId = l.PartyId,
                PartyType = l.PartyType,
                LineDescription = l.Description
            });
        }

        await _postingEngine.PostBatchAsync(batch, ct);

        journal.Status = DocumentStatus.Cancelled;
        await _journalRepo.UpdateAsync(journal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ManualJournalDto
        {
            Id = reversalJournal.Id,
            JournalNumber = reversalJournal.JournalNumber,
            PostingDate = reversalJournal.PostingDate,
            Description = reversalJournal.Description,
            Status = reversalJournal.Status,
            TotalAmount = reversalJournal.TotalAmount,
            IsReversal = true,
            ReversalOfJournalId = journal.Id
        };
    }

    // Customers
    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var existing = (await _customerRepo.FindAsync(c => c.Code == dto.Code, ct)).FirstOrDefault();
        if (existing != null)
        {
            throw new BusinessRuleException($"'{dto.Code}' kodlu müştəri artıq mövcuddur.");
        }

        var customer = new Customer
        {
            TenantId = tenantId,
            Code = dto.Code,
            Name = dto.Name,
            TaxNumber = dto.TaxNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            ReceivableAccountId = (dto.ReceivableAccountId.HasValue && dto.ReceivableAccountId.Value != Guid.Empty) ? dto.ReceivableAccountId : null,
            CreditLimit = dto.CreditLimit,
            PaymentTermsDays = dto.PaymentTermsDays,
            IsActive = true
        };

        await _customerRepo.AddAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[AltensorAccounting] Müştəri yaradıldı: Code={Code}, Name={Name}, TenantId={TenantId}", customer.Code, customer.Name, tenantId);

        return new CustomerDto
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            TaxNumber = customer.TaxNumber,
            Email = customer.Email,
            ReceivableAccountId = customer.ReceivableAccountId,
            OutstandingBalance = 0m
        };
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepo.GetAllAsync(ct);
        return customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            TaxNumber = c.TaxNumber,
            Email = c.Email,
            ReceivableAccountId = c.ReceivableAccountId,
            OutstandingBalance = 0m
        }).ToList();
    }

    // Customer Invoices
    public async Task<CustomerInvoiceDto> CreateCustomerInvoiceAsync(CreateCustomerInvoiceDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId, ct)
            ?? throw new BusinessRuleException("Müştəri tapılmadı.");

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            throw new BusinessRuleException("Fakturada ən azı bir sətir olmalıdır.");
        }

        foreach (var l in dto.Lines)
        {
            if (l.Quantity <= 0)
                throw new BusinessRuleException("Məhsul və ya xidmət sayı 0-dan böyük olmalıdır.");
            if (l.UnitPrice < 0)
                throw new BusinessRuleException("Vahid qiymət mənfi ola bilməz.");
        }

        var subTotal = dto.Lines.Sum(l => (l.Quantity * l.UnitPrice) * (1 - (l.DiscountPercent / 100m)));
        var taxTotal = subTotal * 0.18m; // Default VAT 18%
        var grandTotal = subTotal + taxTotal;

        var invoice = new CustomerInvoice
        {
            TenantId = tenantId,
            InvoiceNumber = $"SINV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            CustomerId = customer.Id,
            InvoiceDate = DateTime.SpecifyKind(dto.InvoiceDate, DateTimeKind.Utc),
            DueDate = DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Utc),
            PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
            DocumentStatus = DocumentStatus.Draft,
            SettlementStatus = SettlementStatus.Unpaid,
            Currency = dto.Currency,
            ExchangeRate = dto.ExchangeRate,
            SubTotal = subTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            OutstandingAmount = grandTotal,
            Notes = dto.Notes
        };

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        var defaultRevenueAccount = (company?.DefaultRevenueAccountId.HasValue == true && company.DefaultRevenueAccountId.Value != Guid.Empty)
            ? company.DefaultRevenueAccountId
            : null;

        if (!defaultRevenueAccount.HasValue)
        {
            var incomeAccount = (await _accountRepo.FindAsync(a => (a.Code == "6010" || a.Type == AccountType.Revenue || a.Category == AccountCategory.Income) && a.IsLeaf && a.IsActive, ct))
                .FirstOrDefault();
            defaultRevenueAccount = incomeAccount?.Id;
        }

        foreach (var l in dto.Lines)
        {
            var lineSubTotal = (l.Quantity * l.UnitPrice) * (1 - (l.DiscountPercent / 100m));
            var lineTax = lineSubTotal * 0.18m;
            var accountId = (l.RevenueAccountId.HasValue && l.RevenueAccountId.Value != Guid.Empty)
                ? l.RevenueAccountId.Value
                : (defaultRevenueAccount.HasValue && defaultRevenueAccount.Value != Guid.Empty ? defaultRevenueAccount : null);

            if (!accountId.HasValue || accountId == Guid.Empty)
            {
                throw new BusinessRuleException("Faktura üçün gəlir hesabı (Revenue Account) təyin edilməyib və standart gəlir hesabı tapılmadı.");
            }

            invoice.Lines.Add(new CustomerInvoiceLine
            {
                TenantId = tenantId,
                ItemId = (l.ItemId.HasValue && l.ItemId.Value != Guid.Empty) ? l.ItemId : null,
                Description = string.IsNullOrWhiteSpace(l.Description) ? "Xidmət / Məhsul" : l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineSubTotal = lineSubTotal,
                TaxCodeId = (l.TaxCodeId.HasValue && l.TaxCodeId.Value != Guid.Empty) ? l.TaxCodeId : null,
                TaxAmount = lineTax,
                LineTotal = lineSubTotal + lineTax,
                RevenueAccountId = accountId,
                CostCenterId = (l.CostCenterId.HasValue && l.CostCenterId.Value != Guid.Empty) ? l.CostCenterId : null,
                ProjectId = (l.ProjectId.HasValue && l.ProjectId.Value != Guid.Empty) ? l.ProjectId : null,
                DepartmentId = (l.DepartmentId.HasValue && l.DepartmentId.Value != Guid.Empty) ? l.DepartmentId : null
            });
        }

        await _invoiceRepo.AddAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CustomerInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            DocumentStatus = invoice.DocumentStatus,
            SettlementStatus = invoice.SettlementStatus,
            SubTotal = invoice.SubTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = 0,
            OutstandingAmount = invoice.OutstandingAmount,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(l => new CustomerInvoiceLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineSubTotal = l.LineSubTotal,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                RevenueAccountId = l.RevenueAccountId
            }).ToList()
        };
    }

    public async Task<List<CustomerInvoiceDto>> GetCustomerInvoicesAsync(CancellationToken ct = default)
    {
        var invoices = await _invoiceRepo.GetAllAsync(ct);
        var customers = (await _customerRepo.GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);

        return invoices.Select(inv => new CustomerInvoiceDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            CustomerId = inv.CustomerId,
            CustomerName = customers.TryGetValue(inv.CustomerId, out var cName) ? cName : string.Empty,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            PostingDate = inv.PostingDate,
            DocumentStatus = inv.DocumentStatus,
            SettlementStatus = inv.SettlementStatus,
            SubTotal = inv.SubTotal,
            TaxTotal = inv.TaxTotal,
            GrandTotal = inv.GrandTotal,
            PaidAmount = inv.PaidAmount,
            OutstandingAmount = inv.OutstandingAmount,
            Notes = inv.Notes
        }).OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<CustomerInvoiceDto?> GetCustomerInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ct, i => i.Lines);
        if (invoice == null) return null;

        var customer = await _customerRepo.GetByIdAsync(invoice.CustomerId, ct);

        return new CustomerInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = customer?.Name ?? string.Empty,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            DocumentStatus = invoice.DocumentStatus,
            SettlementStatus = invoice.SettlementStatus,
            SubTotal = invoice.SubTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(l => new CustomerInvoiceLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineSubTotal = l.LineSubTotal,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                RevenueAccountId = l.RevenueAccountId
            }).ToList()
        };
    }

    public async Task<CustomerInvoiceDto> PostCustomerInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ct, i => i.Lines)
            ?? throw new BusinessRuleException("Faktura tapılmadı.");

        if (invoice.DocumentStatus == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(invoice.InvoiceNumber);
        }

        if (invoice.Lines == null || !invoice.Lines.Any())
        {
            throw new BusinessRuleException("Fakturada ən azı bir sətir olmalıdır.");
        }

        var customer = await _customerRepo.GetByIdAsync(invoice.CustomerId, ct)
            ?? throw new BusinessRuleException("Müştəri tapılmadı.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        if (company == null || !company.DefaultOutputVatAccountId.HasValue || !company.DefaultReceivableAccountId.HasValue || !company.DefaultRevenueAccountId.HasValue)
        {
            await _tenantSeeder.SeedTenantDefaultsAsync(invoice.TenantId, ct);
            company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
                ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");
        }

        var arAccountId = customer.ReceivableAccountId 
            ?? company.DefaultReceivableAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "1200" || a.Type == AccountType.Receivable) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new MissingDefaultAccountException("Debitor borclar (AR) üçün default hesab təyin edilməyib.");

        var vatAccountId = company.DefaultOutputVatAccountId
            ?? (await _accountRepo.FindAsync(a => a.Code == "2250" && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? (await _accountRepo.FindAsync(a => a.Type == AccountType.Tax && a.Category == AccountCategory.Liability && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new MissingDefaultAccountException("Hesablanmış ƏDV üçün default hesab təyin edilməyib.");

        var defaultRevAccountId = company.DefaultRevenueAccountId 
            ?? (await _accountRepo.FindAsync(a => (a.Code == "6010" || a.Type == AccountType.Revenue) && a.IsActive, ct)).FirstOrDefault()?.Id;

        // Post to GL:
        // Dr Accounts Receivable (GrandTotal)
        // Cr Revenue (SubTotal)
        // Cr Output VAT (TaxTotal)
        var batch = new PostingBatch
        {
            SourceDocumentType = DocumentType.CustomerInvoice,
            SourceDocumentId = invoice.Id,
            SourceDocumentNumber = invoice.InvoiceNumber,
            PostingDate = invoice.PostingDate,
            Description = $"Customer Invoice {invoice.InvoiceNumber} - {customer.Name}"
        };

        // Dr AR
        batch.Entries.Add(new LedgerEntry
        {
            AccountId = arAccountId,
            DebitBase = invoice.GrandTotal * invoice.ExchangeRate,
            CreditBase = 0,
            TransactionCurrency = invoice.Currency,
            TransactionAmount = invoice.GrandTotal,
            ExchangeRate = invoice.ExchangeRate,
            PartyId = customer.Id,
            PartyType = "Customer",
            LineDescription = $"Receivable from {customer.Name}"
        });

        // Cr Revenue
        foreach (var line in invoice.Lines)
        {
            var revAccId = (line.RevenueAccountId.HasValue && line.RevenueAccountId.Value != Guid.Empty
                ? line.RevenueAccountId.Value
                : defaultRevAccountId)
                ?? throw new MissingDefaultAccountException("Gəlir (Revenue) hesabı təyin edilməyib.");

            batch.Entries.Add(new LedgerEntry
            {
                AccountId = revAccId,
                DebitBase = 0,
                CreditBase = line.LineSubTotal * invoice.ExchangeRate,
                TransactionCurrency = invoice.Currency,
                TransactionAmount = -line.LineSubTotal,
                ExchangeRate = invoice.ExchangeRate,
                CostCenterId = line.CostCenterId,
                ProjectId = line.ProjectId,
                DepartmentId = line.DepartmentId,
                LineDescription = line.Description
            });
        }

        // Cr Output VAT
        if (invoice.TaxTotal > 0)
        {
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = vatAccountId,
                DebitBase = 0,
                CreditBase = invoice.TaxTotal * invoice.ExchangeRate,
                TransactionCurrency = invoice.Currency,
                TransactionAmount = -invoice.TaxTotal,
                ExchangeRate = invoice.ExchangeRate,
                LineDescription = $"Output VAT on Invoice {invoice.InvoiceNumber}"
            });
        }

        await _postingEngine.PostBatchAsync(batch, ct);

        invoice.DocumentStatus = DocumentStatus.Posted;
        await _invoiceRepo.UpdateAsync(invoice, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CustomerInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            DocumentStatus = invoice.DocumentStatus,
            SettlementStatus = invoice.SettlementStatus,
            SubTotal = invoice.SubTotal,
            TaxTotal = invoice.TaxTotal,
            GrandTotal = invoice.GrandTotal,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(l => new CustomerInvoiceLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                LineSubTotal = l.LineSubTotal,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                RevenueAccountId = l.RevenueAccountId
            }).ToList()
        };
    }

    // Payments
    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        if (dto.TotalAmount <= 0)
        {
            throw new BusinessRuleException("Ödəniş məbləği 0-dan böyük olmalıdır.");
        }

        foreach (var a in dto.Allocations)
        {
            if (a.AllocatedAmount <= 0)
                throw new BusinessRuleException("Bölüşdürülən məbləğ 0-dan böyük olmalıdır.");
        }

        // ─── SENIOR FIX: Resolve GL Account for BankOrCashAccountId ───
        Guid effectiveGlAccountId = Guid.Empty;

        if (dto.BankOrCashAccountId != Guid.Empty)
        {
            // 1. Birbaşa Accounts (GL) cədvəlində mövcudluğunu yoxla
            var directAccount = await _accountRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
            if (directAccount != null && directAccount.IsActive)
            {
                effectiveGlAccountId = directAccount.Id;
            }
            else
            {
                // 2. Əgər GL Account tapılmadısa, bəlkə bu Treasury BankAccount ID-sidir?
                var bankAccount = await _bankRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
                if (bankAccount != null)
                {
                    if (bankAccount.GLAccountId.HasValue && bankAccount.GLAccountId.Value != Guid.Empty)
                    {
                        var linkedGl = await _accountRepo.GetByIdAsync(bankAccount.GLAccountId.Value, ct);
                        if (linkedGl != null && linkedGl.IsActive)
                        {
                            effectiveGlAccountId = linkedGl.Id;
                        }
                    }

                    if (effectiveGlAccountId == Guid.Empty)
                    {
                        // Bank hesabına GL bağlanmayıbsa və ya aktiv deyilsə, standart 1020 hesabına yönləndir
                        var defaultBankGl = (await _accountRepo.FindAsync(a => (a.Code == "1020" || a.Code.StartsWith("102")) && a.IsActive, ct)).FirstOrDefault();
                        if (defaultBankGl != null) effectiveGlAccountId = defaultBankGl.Id;
                    }
                }
                else
                {
                    // 3. Bəlkə bu Treasury CashDesk (Kassa) ID-sidir?
                    var cashDesk = await _cashRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
                    if (cashDesk != null)
                    {
                        if (cashDesk.GLAccountId.HasValue && cashDesk.GLAccountId.Value != Guid.Empty)
                        {
                            var linkedGl = await _accountRepo.GetByIdAsync(cashDesk.GLAccountId.Value, ct);
                            if (linkedGl != null && linkedGl.IsActive)
                            {
                                effectiveGlAccountId = linkedGl.Id;
                            }
                        }

                        if (effectiveGlAccountId == Guid.Empty)
                        {
                            // Kassa hesabına GL bağlanmayıbsa və ya aktiv deyilsə, standart 1010 hesabına yönləndir
                            var defaultCashGl = (await _accountRepo.FindAsync(a => (a.Code == "1010" || a.Code.StartsWith("101")) && a.IsActive, ct)).FirstOrDefault();
                            if (defaultCashGl != null) effectiveGlAccountId = defaultCashGl.Id;
                        }
                    }
                }
            }
        }

        // Əgər hələ də tapılmadısa, son fallback olaraq sistemdəki ilk aktiv Aktiv (Asset/Bank/Cash) hesabını tap
        if (effectiveGlAccountId == Guid.Empty)
        {
            var fallbackGl = (await _accountRepo.FindAsync(a => (a.Code == "1020" || a.Code == "1010" || a.Category == AccountCategory.Asset) && a.IsActive, ct)).FirstOrDefault();
            if (fallbackGl != null)
            {
                effectiveGlAccountId = fallbackGl.Id;
            }
            else
            {
                throw new BusinessRuleException("Seçilmiş Bank/Kassa hesabı üçün Mühasibatlıq Hesabı (GL Account) tapılmadı. Zəhmət olmasa Hesablar Planında 1020 və ya 1010 hesabını yoxlayın.");
            }
        }

        var allocatedTotal = dto.Allocations.Sum(a => a.AllocatedAmount);
        var unallocated = dto.TotalAmount - allocatedTotal;

        var payment = new Payment
        {
            TenantId = tenantId,
            PaymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Type = dto.Type,
            PaymentDate = DateTime.SpecifyKind(dto.PaymentDate, DateTimeKind.Utc),
            PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
            PartyId = dto.PartyId,
            PartyType = dto.PartyType,
            BankOrCashAccountId = effectiveGlAccountId,
            Currency = dto.Currency,
            ExchangeRate = dto.ExchangeRate,
            TotalAmount = dto.TotalAmount,
            AllocatedAmount = allocatedTotal,
            UnallocatedAmount = unallocated > 0 ? unallocated : 0,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            Status = DocumentStatus.Draft
        };

        foreach (var alloc in dto.Allocations)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                TenantId = tenantId,
                TargetDocumentType = alloc.TargetDocumentType,
                TargetDocumentId = alloc.TargetDocumentId,
                AllocatedAmount = alloc.AllocatedAmount
            });
        }

        await _paymentRepo.AddAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            Type = payment.Type,
            PaymentDate = payment.PaymentDate,
            PostingDate = payment.PostingDate,
            TotalAmount = payment.TotalAmount,
            AllocatedAmount = payment.AllocatedAmount,
            UnallocatedAmount = payment.UnallocatedAmount,
            Status = payment.Status
        };
    }

    public async Task<PaymentDto> PostPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId, ct, p => p.Allocations)
            ?? throw new BusinessRuleException("Ödəniş tapılmadı.");

        if (payment.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(payment.PaymentNumber);
        }

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var batch = new PostingBatch
        {
            SourceDocumentType = (payment.Type == PaymentType.CustomerReceipt || payment.Type == PaymentType.CustomerAdvance)
                ? DocumentType.CustomerPayment
                : DocumentType.SupplierPayment,
            SourceDocumentId = payment.Id,
            SourceDocumentNumber = payment.PaymentNumber,
            PostingDate = payment.PostingDate,
            Description = $"Payment {payment.PaymentNumber}"
        };

        if (payment.Type == PaymentType.CustomerReceipt || payment.Type == PaymentType.CustomerAdvance)
        {
            var arAccountId = company.DefaultReceivableAccountId
                ?? (await _accountRepo.FindAsync(a => (a.Code == "1200" || a.Type == AccountType.Receivable) && a.IsActive, ct)).FirstOrDefault()?.Id
                ?? throw new MissingDefaultAccountException("Debitor borclar (AR) hesabı təyin edilməyib.");

            // Dr Bank/Cash
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = payment.BankOrCashAccountId,
                DebitBase = payment.TotalAmount * payment.ExchangeRate,
                CreditBase = 0,
                TransactionCurrency = payment.Currency,
                TransactionAmount = payment.TotalAmount,
                ExchangeRate = payment.ExchangeRate,
                PartyId = payment.PartyId,
                PartyType = "Customer",
                LineDescription = "Receipt from customer"
            });

            // Cr AR
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = arAccountId,
                DebitBase = 0,
                CreditBase = payment.TotalAmount * payment.ExchangeRate,
                TransactionCurrency = payment.Currency,
                TransactionAmount = -payment.TotalAmount,
                ExchangeRate = payment.ExchangeRate,
                PartyId = payment.PartyId,
                PartyType = "Customer",
                LineDescription = "AR Settlement / Customer Advance"
            });

            // Update allocated customer invoices
            foreach (var alloc in payment.Allocations.Where(a => a.TargetDocumentType == DocumentType.CustomerInvoice))
            {
                var inv = await _invoiceRepo.GetByIdAsync(alloc.TargetDocumentId, ct);
                if (inv != null)
                {
                    inv.PaidAmount += alloc.AllocatedAmount;
                    inv.OutstandingAmount = Math.Max(0, inv.GrandTotal - inv.PaidAmount);
                    inv.SettlementStatus = inv.OutstandingAmount <= 0 ? SettlementStatus.Paid : SettlementStatus.PartiallyPaid;
                    await _invoiceRepo.UpdateAsync(inv, ct);
                }
            }
        }
        else if (payment.Type == PaymentType.SupplierPayment || payment.Type == PaymentType.SupplierAdvance)
        {
            var apAccountId = company.DefaultPayableAccountId
                ?? (await _accountRepo.FindAsync(a => (a.Code == "2100" || a.Type == AccountType.Payable) && a.IsActive, ct)).FirstOrDefault()?.Id
                ?? throw new MissingDefaultAccountException("Kreditor borclar (AP) hesabı təyin edilməyib.");

            // Dr AP
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = apAccountId,
                DebitBase = payment.TotalAmount * payment.ExchangeRate,
                CreditBase = 0,
                TransactionCurrency = payment.Currency,
                TransactionAmount = payment.TotalAmount,
                ExchangeRate = payment.ExchangeRate,
                PartyId = payment.PartyId,
                PartyType = "Supplier",
                LineDescription = "AP Settlement / Supplier Advance"
            });

            // Cr Bank/Cash
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = payment.BankOrCashAccountId,
                DebitBase = 0,
                CreditBase = payment.TotalAmount * payment.ExchangeRate,
                TransactionCurrency = payment.Currency,
                TransactionAmount = -payment.TotalAmount,
                ExchangeRate = payment.ExchangeRate,
                PartyId = payment.PartyId,
                PartyType = "Supplier",
                LineDescription = "Payment to supplier"
            });

            // Update allocated supplier invoices
            foreach (var alloc in payment.Allocations.Where(a => a.TargetDocumentType == DocumentType.SupplierInvoice))
            {
                var suppInv = await _suppInvoiceRepo.GetByIdAsync(alloc.TargetDocumentId, ct);
                if (suppInv != null)
                {
                    suppInv.PaidAmount += alloc.AllocatedAmount;
                    suppInv.OutstandingAmount = Math.Max(0, suppInv.GrandTotal - suppInv.PaidAmount);
                    suppInv.SettlementStatus = suppInv.OutstandingAmount <= 0 ? SettlementStatus.Paid : SettlementStatus.PartiallyPaid;
                    await _suppInvoiceRepo.UpdateAsync(suppInv, ct);
                }
            }
        }

        await _postingEngine.PostBatchAsync(batch, ct);

        payment.Status = DocumentStatus.Posted;
        await _paymentRepo.UpdateAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            Type = payment.Type,
            PaymentDate = payment.PaymentDate,
            PostingDate = payment.PostingDate,
            TotalAmount = payment.TotalAmount,
            AllocatedAmount = payment.AllocatedAmount,
            UnallocatedAmount = payment.UnallocatedAmount,
            Status = payment.Status
        };
    }

    public async Task SeedTemplateAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");
        await _tenantSeeder.SeedTenantDefaultsAsync(tenantId, ct);
    }

    public async Task<CompanyDefaultAccountsDto> GetDefaultAccountsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        if (company == null)
        {
            await _tenantSeeder.SeedTenantDefaultsAsync(tenantId, ct);
            company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
                ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");
        }

        return new CompanyDefaultAccountsDto
        {
            DefaultReceivableAccountId = company.DefaultReceivableAccountId,
            DefaultPayableAccountId = company.DefaultPayableAccountId,
            DefaultStockAccountId = company.DefaultStockAccountId,
            DefaultGRNIAccountId = company.DefaultGRNIAccountId,
            DefaultCOGSAccountId = company.DefaultCOGSAccountId,
            DefaultRetainedEarningsAccountId = company.DefaultRetainedEarningsAccountId,
            DefaultInputVatAccountId = company.DefaultInputVatAccountId,
            DefaultOutputVatAccountId = company.DefaultOutputVatAccountId,
            DefaultRevenueAccountId = company.DefaultRevenueAccountId,
            DefaultFXGainLossAccountId = company.DefaultFXGainLossAccountId
        };
    }

    public async Task<CompanyDefaultAccountsDto> UpdateDefaultAccountsAsync(CompanyDefaultAccountsDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        if (company == null)
        {
            await _tenantSeeder.SeedTenantDefaultsAsync(tenantId, ct);
            company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
                ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");
        }

        company.DefaultReceivableAccountId = dto.DefaultReceivableAccountId;
        company.DefaultPayableAccountId = dto.DefaultPayableAccountId;
        company.DefaultStockAccountId = dto.DefaultStockAccountId;
        company.DefaultGRNIAccountId = dto.DefaultGRNIAccountId;
        company.DefaultCOGSAccountId = dto.DefaultCOGSAccountId;
        company.DefaultRetainedEarningsAccountId = dto.DefaultRetainedEarningsAccountId;
        company.DefaultInputVatAccountId = dto.DefaultInputVatAccountId;
        company.DefaultOutputVatAccountId = dto.DefaultOutputVatAccountId;
        company.DefaultRevenueAccountId = dto.DefaultRevenueAccountId;
        company.DefaultFXGainLossAccountId = dto.DefaultFXGainLossAccountId;

        await _companyRepo.UpdateAsync(company, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return dto;
    }
}
