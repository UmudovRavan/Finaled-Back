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
    private readonly IGenericRepository<Payment> _paymentRepo;
    private readonly IGenericRepository<Company> _companyRepo;
    private readonly IPostingEngine _postingEngine;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public TreasuryService(
        IGenericRepository<BankAccount> bankRepo,
        IGenericRepository<CashDesk> cashRepo,
        IGenericRepository<BankStatement> statementRepo,
        IGenericRepository<PaymentRun> runRepo,
        IGenericRepository<SupplierInvoice> invoiceRepo,
        IGenericRepository<Payment> paymentRepo,
        IGenericRepository<Company> companyRepo,
        IPostingEngine postingEngine,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork)
    {
        _bankRepo = bankRepo;
        _cashRepo = cashRepo;
        _statementRepo = statementRepo;
        _runRepo = runRepo;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _companyRepo = companyRepo;
        _postingEngine = postingEngine;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
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
            GLAccountId = dto.GLAccountId,
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
            GLAccountId = dto.GLAccountId,
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
        var run = await _runRepo.GetByIdAsync(runId, ct)
            ?? throw new BusinessRuleException("Ödəniş təklifi (Payment Run) tapılmadı.");

        if (run.Status == DocumentStatus.Posted)
        {
            throw new DuplicatePostingException(run.RunNumber);
        }

        var bank = await _bankRepo.GetByIdAsync(run.BankAccountId, ct)
            ?? throw new BusinessRuleException("Bank hesabı tapılmadı.");

        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault()
            ?? throw new BusinessRuleException("Şirkət parametrləri qurulmayıb.");

        var apAccountId = company.DefaultPayableAccountId
            ?? throw new BusinessRuleException("Kreditor borclar (AP) hesabı təyin edilməyib.");

        // Process each selected invoice item in the run
        foreach (var item in run.Items.Where(i => i.IsSelected))
        {
            var invoice = await _invoiceRepo.GetByIdAsync(item.SupplierInvoiceId, ct);
            if (invoice == null) continue;

            // Create individual payment voucher
            var payment = new Payment
            {
                TenantId = run.TenantId,
                PaymentNumber = $"PAY-RUN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                Type = PaymentType.SupplierPayment,
                PaymentDate = run.RunDate,
                PostingDate = run.RunDate,
                PartyId = item.SupplierId,
                PartyType = "Supplier",
                BankOrCashAccountId = bank.GLAccountId,
                Currency = bank.Currency,
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
                PostingDate = run.RunDate,
                Description = $"Payment Run {run.RunNumber} to Supplier for {invoice.SupplierInvoiceNumber}"
            };

            // Dr AP
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = apAccountId,
                DebitBase = item.ProposedPaymentAmount,
                CreditBase = 0,
                TransactionAmount = item.ProposedPaymentAmount,
                PartyId = item.SupplierId,
                PartyType = "Supplier",
                LineDescription = $"Payable clear for {invoice.SupplierInvoiceNumber}"
            });

            // Cr Bank
            batch.Entries.Add(new LedgerEntry
            {
                AccountId = bank.GLAccountId,
                DebitBase = 0,
                CreditBase = item.ProposedPaymentAmount,
                TransactionAmount = -item.ProposedPaymentAmount,
                LineDescription = $"Bank Payment for {invoice.SupplierInvoiceNumber}"
            });

            await _postingEngine.PostBatchAsync(batch, ct);

            // Update Supplier Invoice status
            invoice.PaidAmount += item.ProposedPaymentAmount;
            invoice.OutstandingAmount = invoice.GrandTotal - invoice.PaidAmount;
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
