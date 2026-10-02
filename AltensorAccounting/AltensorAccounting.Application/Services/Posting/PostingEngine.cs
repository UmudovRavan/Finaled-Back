using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Application.Services.Posting;

public class PostingEngine : IPostingEngine
{
    private readonly IGenericRepository<PostingBatch> _batchRepo;
    private readonly IGenericRepository<AccountingPeriod> _periodRepo;
    private readonly ICurrentTenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostingEngine> _logger;

    public PostingEngine(
        IGenericRepository<PostingBatch> batchRepo,
        IGenericRepository<AccountingPeriod> periodRepo,
        ICurrentTenantService tenantService,
        IUnitOfWork unitOfWork,
        ILogger<PostingEngine> logger)
    {
        _batchRepo = batchRepo;
        _periodRepo = periodRepo;
        _tenantService = tenantService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PostingBatch> PostBatchAsync(PostingBatch batch, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId 
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        batch.TenantId = tenantId;

        // 1. Period Lock Validation
        var date = batch.PostingDate.Date;
        var period = (await _periodRepo.FindAsync(p => p.StartDate <= date && p.EndDate >= date, ct))
            .FirstOrDefault();

        if (period == null)
        {
            throw new BusinessRuleException($"Posting tarixi ({batch.PostingDate:yyyy-MM-dd}) üçün heç bir maliyyə dövrü təyin edilməyib.");
        }

        if (period.Status != FiscalPeriodStatus.Open)
        {
            throw new PeriodLockedException(batch.PostingDate, period.Status.ToString());
        }

        // 2. Double-Entry Invariant (ΣDebitBase == ΣCreditBase)
        var totalDebit = batch.Entries.Sum(e => e.DebitBase);
        var totalCredit = batch.Entries.Sum(e => e.CreditBase);

        if (Math.Abs(totalDebit - totalCredit) > 0.001m)
        {
            throw new PostingUnbalancedException(totalDebit, totalCredit);
        }

        batch.TotalDebitBase = totalDebit;
        batch.TotalCreditBase = totalCredit;

        // 3. Idempotency Check (Check if source document already posted)
        if (batch.SourceDocumentId != Guid.Empty)
        {
            var alreadyPosted = await _batchRepo.ExistsAsync(b => 
                b.SourceDocumentType == batch.SourceDocumentType &&
                b.SourceDocumentId == batch.SourceDocumentId &&
                !b.IsReversed, ct);

            if (alreadyPosted)
            {
                throw new DuplicatePostingException(batch.SourceDocumentNumber ?? batch.SourceDocumentId.ToString());
            }
        }

        // Generate batch number if empty
        if (string.IsNullOrWhiteSpace(batch.BatchNumber))
        {
            batch.BatchNumber = $"PB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        }

        // Ensure TenantId on all entries
        foreach (var entry in batch.Entries)
        {
            entry.TenantId = tenantId;
            entry.PostingDate = batch.PostingDate;
            entry.SourceDocumentType = batch.SourceDocumentType;
            entry.SourceDocumentId = batch.SourceDocumentId;
        }

        await _batchRepo.AddAsync(batch, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[AltensorAccounting] Ledger Batch {BatchNumber} posted. Debit={Debit}, Credit={Credit}, Lines={Lines}, TenantId={TenantId}",
            batch.BatchNumber, batch.TotalDebitBase, batch.TotalCreditBase, batch.Entries.Count, tenantId);

        return batch;
    }

    public async Task<PostingBatch> ReverseBatchAsync(Guid originalBatchId, string reason, DateTime reversalDate, CancellationToken ct = default)
    {
        var tenantId = _tenantService.TenantId
            ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

        var originalBatch = await _batchRepo.GetByIdAsync(originalBatchId, ct, b => b.Entries)
            ?? throw new BusinessRuleException("Orijinal posting batch tapılmadı.");

        if (originalBatch.IsReversed)
        {
            throw new BusinessRuleException("Bu posting batch artıq reverse olunub.");
        }

        // Period check for reversal date
        var date = reversalDate.Date;
        var period = (await _periodRepo.FindAsync(p => p.StartDate <= date && p.EndDate >= date, ct))
            .FirstOrDefault();

        if (period == null)
        {
            throw new BusinessRuleException($"Reversal tarixi ({reversalDate:yyyy-MM-dd}) üçün heç bir maliyyə dövrü təyin edilməyib.");
        }

        if (period.Status != FiscalPeriodStatus.Open)
        {
            throw new PeriodLockedException(reversalDate, period.Status.ToString());
        }

        var reversalBatch = new PostingBatch
        {
            TenantId = tenantId,
            BatchNumber = $"REV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            PostingDate = reversalDate,
            SourceDocumentType = originalBatch.SourceDocumentType,
            SourceDocumentId = originalBatch.SourceDocumentId,
            SourceDocumentNumber = originalBatch.SourceDocumentNumber,
            Description = $"Reversal of {originalBatch.BatchNumber}: {reason}",
            TotalDebitBase = originalBatch.TotalCreditBase,
            TotalCreditBase = originalBatch.TotalDebitBase,
            ReversalBatchId = originalBatch.Id
        };

        foreach (var originalEntry in originalBatch.Entries)
        {
            reversalBatch.Entries.Add(new LedgerEntry
            {
                TenantId = tenantId,
                PostingDate = reversalDate,
                AccountId = originalEntry.AccountId,
                DebitBase = originalEntry.CreditBase, // SWAPPED
                CreditBase = originalEntry.DebitBase, // SWAPPED
                TransactionCurrency = originalEntry.TransactionCurrency,
                TransactionAmount = -originalEntry.TransactionAmount,
                ExchangeRate = originalEntry.ExchangeRate,
                PartyId = originalEntry.PartyId,
                PartyType = originalEntry.PartyType,
                CostCenterId = originalEntry.CostCenterId,
                ProjectId = originalEntry.ProjectId,
                DepartmentId = originalEntry.DepartmentId,
                BranchId = originalEntry.BranchId,
                LineDescription = $"Reversal: {originalEntry.LineDescription}"
            });
        }

        originalBatch.IsReversed = true;
        originalBatch.ReversalBatchId = reversalBatch.Id;

        await _batchRepo.UpdateAsync(originalBatch, ct);
        await _batchRepo.AddAsync(reversalBatch, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[AltensorAccounting] Ledger Batch {BatchNumber} reversed. ReversalBatch={ReversalBatchNumber}, Reason={Reason}, TenantId={TenantId}",
            originalBatch.BatchNumber, reversalBatch.BatchNumber, reason, tenantId);

        return reversalBatch;
    }
}
