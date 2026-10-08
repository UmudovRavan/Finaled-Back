using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Contract.DTOs.Treasury;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Entities.Treasury;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

namespace AltensorAccounting.Application.Services;

public class TreasuryService : ITreasuryService
{
    private readonly IGenericRepository<BankAccount> _bankRepo;
    private readonly IGenericRepository<CashDesk> _cashRepo;
    private readonly IGenericRepository<BankStatement> _statementRepo;
    private readonly IGenericRepository<PaymentRun> _runRepo;
    private readonly IGenericRepository<SupplierInvoice> _invoiceRepo;
    private readonly IGenericRepository<Supplier> _supplierRepo;
    private readonly IGenericRepository<Payment> _paymentRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IPostingEngine _postingEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Logging.ILogger<TreasuryService> _logger;

    public TreasuryService(
        IGenericRepository<BankAccount> bankRepo,
        IGenericRepository<CashDesk> cashRepo,
        IGenericRepository<BankStatement> statementRepo,
        IGenericRepository<PaymentRun> runRepo,
        IGenericRepository<SupplierInvoice> invoiceRepo,
        IGenericRepository<Supplier> supplierRepo,
        IGenericRepository<Payment> paymentRepo,
        IGenericRepository<Company> companyRepo,
        IGenericRepository<Account> accountRepo,
        IPostingEngine postingEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        Microsoft.Extensions.Logging.ILogger<TreasuryService> logger)
    {
        _bankRepo = bankRepo;
        _cashRepo = cashRepo;
        _statementRepo = statementRepo;
        _runRepo = runRepo;
        _invoiceRepo = invoiceRepo;
        _supplierRepo = supplierRepo;
        _paymentRepo = paymentRepo;
        _companyRepo = companyRepo;
        _accountRepo = accountRepo;
        _postingEngine = postingEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BankAccountDto> CreateBankAccountAsync(CreateBankAccountDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var bank = new BankAccount
        {
            TenantId = tenantId,
            BankName = dto.BankName,
            AccountNumber = dto.AccountNumber,
            Currency = dto.Currency,
            SwiftCode = dto.SwiftCode,
            GLAccountId = (dto.GLAccountId.HasValue && dto.GLAccountId.Value != Guid.Empty) ? dto.GLAccountId.Value : null,
            CurrentBalance = 0
        };

        await _bankRepo.AddAsync(bank, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new BankAccountDto
        {
            Id = bank.Id,
            BankName = bank.BankName,
            AccountNumber = bank.AccountNumber,
            Currency = bank.Currency,
            CurrentBalance = bank.CurrentBalance
        };
    }

    public async Task<List<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default)
    {
        var banks = await _bankRepo.GetAllAsync(ct);
        return banks.Select(b => new BankAccountDto
        {
            Id = b.Id,
            BankName = b.BankName,
            AccountNumber = b.AccountNumber,
            Currency = b.Currency,
            CurrentBalance = b.CurrentBalance
        }).ToList();
    }

    public async Task<CashDeskDto> CreateCashDeskAsync(CreateCashDeskDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var cash = new CashDesk
        {
            TenantId = tenantId,
            Name = dto.Name,
            Currency = dto.Currency,
            GLAccountId = (dto.GLAccountId.HasValue && dto.GLAccountId.Value != Guid.Empty) ? dto.GLAccountId.Value : null,
            CurrentBalance = 0
        };

        await _cashRepo.AddAsync(cash, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new CashDeskDto
        {
            Id = cash.Id,
            Name = cash.Name,
            Currency = cash.Currency,
            CurrentBalance = cash.CurrentBalance
        };
    }

    public async Task<BankStatementDto> ImportBankStatementAsync(ImportBankStatementDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var bank = await _bankRepo.GetByIdAsync(dto.BankAccountId, ct)
            ?? throw new BusinessRuleException("Bank hesabı tapılmadı.");

        var totalDeposits = dto.Lines.Where(l => l.Amount > 0).Sum(l => l.Amount);
        var totalWithdrawals = dto.Lines.Where(l => l.Amount < 0).Sum(l => Math.Abs(l.Amount));

        var statement = new BankStatement
        {
            TenantId = tenantId,
            BankAccountId = bank.Id,
            StatementNumber = dto.StatementNumber,
            StatementDate = dto.StatementDate,
            OpeningBalance = dto.OpeningBalance,
            ClosingBalance = dto.ClosingBalance,
            TotalDeposits = totalDeposits,
            TotalWithdrawals = totalWithdrawals
        };

        foreach (var l in dto.Lines)
        {
            statement.Lines.Add(new BankStatementLine
            {
                TenantId = tenantId,
                TransactionDate = l.TransactionDate,
                Amount = l.Amount,
                Reference = l.Reference,
                CounterpartyName = l.CounterpartyName,
                Description = l.Description,
                Status = ReconciliationStatus.Unmatched
            });
        }

        await _statementRepo.AddAsync(statement, ct);

        bank.CurrentBalance = dto.ClosingBalance;
        await _bankRepo.UpdateAsync(bank, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new BankStatementDto
        {
            Id = statement.Id,
            StatementNumber = statement.StatementNumber,
            StatementDate = statement.StatementDate,
            OpeningBalance = statement.OpeningBalance,
            ClosingBalance = statement.ClosingBalance,
            TotalDeposits = statement.TotalDeposits,
            TotalWithdrawals = statement.TotalWithdrawals
        };
    }

    public async Task<PaymentRunDto> CreatePaymentRunAsync(CreatePaymentRunDto dto, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var bank = await _bankRepo.GetByIdAsync(dto.BankAccountId, ct)
            ?? throw new BusinessRuleException("Bank hesabı tapılmadı.");

        var invoices = await _invoiceRepo.FindAsync(i => 
            dto.SelectedSupplierInvoiceIds.Contains(i.Id) && 
            i.DocumentStatus == DocumentStatus.Posted && 
            i.SettlementStatus != SettlementStatus.Paid, ct);

        var run = new PaymentRun
        {
            TenantId = tenantId,
            RunNumber = $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            RunDate = dto.RunDate,
            BankAccountId = bank.Id,
            TotalProposalAmount = invoices.Sum(i => i.OutstandingAmount),
            Status = DocumentStatus.Draft
        };

        foreach (var inv in invoices)
        {
            run.Items.Add(new PaymentRunItem
            {
                TenantId = tenantId,
                SupplierId = inv.SupplierId,
                SupplierInvoiceId = inv.Id,
                InvoiceOutstandingAmount = inv.OutstandingAmount,
                ProposedPaymentAmount = inv.OutstandingAmount,
                IsSelected = true
            });
        }

        await _runRepo.AddAsync(run, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentRunDto
        {
            Id = run.Id,
            RunNumber = run.RunNumber,
            RunDate = run.RunDate,
            TotalProposalAmount = run.TotalProposalAmount,
            Status = run.Status,
            InvoiceCount = run.Items.Count
        };
    }

    public async Task<PaymentRunDto> PostPaymentRunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _runRepo.GetByIdAsync(runId, ct, r => r.Items)
            ?? throw new BusinessRuleException("Ödəniş təklifi (Payment Run) tapılmadı.");

        if (run.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(run.RunNumber);
        }

        var bank = await _bankRepo.GetByIdAsync(run.BankAccountId, ct)
            ?? throw new BusinessRuleException("Bank hesabı tapılmadı.");

        var bankGlId = bank.GLAccountId 
            ?? throw new BusinessRuleException("Bank hesabı üçün GL hesabı təyin edilməyib.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var apAccountId = company.DefaultPayableAccountId
            ?? (await _accountRepo.FindAsync(a => (a.Code == "2100" || a.Type == AccountType.Payable) && a.IsActive, ct)).FirstOrDefault()?.Id
            ?? throw new MissingDefaultAccountException("Kreditor borclar (AP) hesabı təyin edilməyib.");

        var utcRunDate = DateTime.SpecifyKind(run.RunDate, DateTimeKind.Utc);

        // Process each selected invoice item in the run
        foreach (var item in run.Items.Where(i => i.IsSelected))
        {
            var invoice = await _invoiceRepo.GetByIdAsync(item.SupplierInvoiceId, ct);
            if (invoice == null) continue;

            var supplier = await _supplierRepo.GetByIdAsync(item.SupplierId, ct);
            var effectiveApAccountId = supplier?.PayableAccountId ?? apAccountId;
            var exchangeRate = invoice.ExchangeRate > 0 ? invoice.ExchangeRate : 1.0m;

            // Create individual payment voucher
            var payment = new Payment
            {
                TenantId = run.TenantId,
                PaymentNumber = $"PAY-RUN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                Type = PaymentType.SupplierPayment,
                PaymentDate = utcRunDate,
                PostingDate = utcRunDate,
                PartyId = item.SupplierId,
                PartyType = "Supplier",
                BankOrCashAccountId = bankGlId,
                Currency = bank.Currency,
                ExchangeRate = exchangeRate,
                TotalAmount = item.ProposedPaymentAmount,
                AllocatedAmount = item.ProposedPaymentAmount,
                UnallocatedAmount = 0,
                ReferenceNumber = run.RunNumber,
                Status = DocumentStatus.Posted
            };

            payment.Allocations.Add(new PaymentAllocation
            {
                TenantId = run.TenantId,
                TargetDocumentType = DocumentType.SupplierInvoice,
                TargetDocumentId = invoice.Id,
                AllocatedAmount = item.ProposedPaymentAmount
            });

            await _paymentRepo.AddAsync(payment, ct);

            // Post to GL:
            // Dr Accounts Payable (AP)
            // Cr Bank Account
            var batch = new PostingBatch
            {
                SourceDocumentType = DocumentType.SupplierPayment,
                SourceDocumentId = payment.Id,
                SourceDocumentNumber = payment.PaymentNumber,
                PostingDate = utcRunDate,
                Description = $"Payment Run {run.RunNumber} to Supplier for {invoice.SupplierInvoiceNumber}"
            };

            // Dr AP
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = effectiveApAccountId,
                DebitBase = item.ProposedPaymentAmount * exchangeRate,
                CreditBase = 0,
                TransactionCurrency = bank.Currency,
                TransactionAmount = item.ProposedPaymentAmount,
                ExchangeRate = exchangeRate,
                PartyId = item.SupplierId,
                PartyType = "Supplier",
                LineDescription = $"Payable clear for {invoice.SupplierInvoiceNumber}"
            });

            // Cr Bank
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = bankGlId,
                DebitBase = 0,
                CreditBase = item.ProposedPaymentAmount * exchangeRate,
                TransactionCurrency = bank.Currency,
                TransactionAmount = -item.ProposedPaymentAmount,
                ExchangeRate = exchangeRate,
                LineDescription = $"Bank Payment for {invoice.SupplierInvoiceNumber}"
            });

            await _postingEngine.PostBatchAsync(batch, ct);

            // Update Supplier Invoice status
            invoice.PaidAmount += item.ProposedPaymentAmount;
            invoice.OutstandingAmount = Math.Max(0, invoice.GrandTotal - invoice.PaidAmount);
            invoice.SettlementStatus = invoice.OutstandingAmount <= 0 ? SettlementStatus.Paid : SettlementStatus.PartiallyPaid;
            await _invoiceRepo.UpdateAsync(invoice, ct);
        }

        run.Status = DocumentStatus.Posted;
        await _runRepo.UpdateAsync(run, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new PaymentRunDto
        {
            Id = run.Id,
            RunNumber = run.RunNumber,
            RunDate = run.RunDate,
            TotalProposalAmount = run.TotalProposalAmount,
            Status = run.Status,
            InvoiceCount = run.Items.Count
        };
    }
}
