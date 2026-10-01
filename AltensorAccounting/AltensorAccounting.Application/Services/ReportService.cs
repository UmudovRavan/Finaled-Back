using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Reports;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Entities.Treasury;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Application.Services;

public class ReportService : IReportService
{
    private readonly IGenericRepository<Account> _accountRepo;
    private readonly IGenericRepository<LedgerEntry> _ledgerRepo;
    private readonly IGenericRepository<CustomerInvoice> _custInvRepo;
    private readonly IGenericRepository<SupplierInvoice> _suppInvRepo;
    private readonly IGenericRepository<Customer> _customerRepo;
    private readonly IGenericRepository<Supplier> _supplierRepo;
    private readonly IGenericRepository<StockLedgerEntry> _stockLedgerRepo;
    private readonly IGenericRepository<BankAccount> _bankRepo;
    private readonly IGenericRepository<BankStatement> _statementRepo;
    private readonly IGenericRepository<Company> _companyRepo;

    public ReportService(
        IGenericRepository<Account> accountRepo,
        IGenericRepository<LedgerEntry> ledgerRepo,
        IGenericRepository<CustomerInvoice> custInvRepo,
        IGenericRepository<SupplierInvoice> suppInvRepo,
        IGenericRepository<Customer> customerRepo,
        IGenericRepository<Supplier> supplierRepo,
        IGenericRepository<StockLedgerEntry> stockLedgerRepo,
        IGenericRepository<BankAccount> bankRepo,
        IGenericRepository<BankStatement> statementRepo,
        IGenericRepository<Company> companyRepo)
    {
        _accountRepo = accountRepo;
        _ledgerRepo = ledgerRepo;
        _custInvRepo = custInvRepo;
        _suppInvRepo = suppInvRepo;
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _stockLedgerRepo = stockLedgerRepo;
        _bankRepo = bankRepo;
        _statementRepo = statementRepo;
        _companyRepo = companyRepo;
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate <= asOfDate.Date, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var report = new TrialBalanceReportDto
        {
            AsOfDate = asOfDate
        };

        foreach (var account in accounts.OrderBy(a => a.Code))
        {
            var accEntries = entries.Where(e => e.AccountId == account.Id).ToList();
            var debit = accEntries.Sum(e => e.DebitBase);
            var credit = accEntries.Sum(e => e.CreditBase);

            if (debit > 0 || credit > 0)
            {
                report.Lines.Add(new TrialBalanceLineDto
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    Category = account.Category.ToString(),
                    Debit = debit,
                    Credit = credit
                });
            }
        }

        report.TotalDebit = report.Lines.Sum(l => l.Debit);
        report.TotalCredit = report.Lines.Sum(l => l.Credit);

        return report;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate <= asOfDate.Date, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var assets = accounts.Where(a => a.Category == AccountCategory.Asset).ToList();
        var liabilities = accounts.Where(a => a.Category == AccountCategory.Liability).ToList();
        var equities = accounts.Where(a => a.Category == AccountCategory.Equity).ToList();

        // Current Year Retained Earnings = All Income - All Expense up to asOfDate
        var incomes = accounts.Where(a => a.Category == AccountCategory.Income).Select(a => a.Id).ToList();
        var expenses = accounts.Where(a => a.Category == AccountCategory.Expense).Select(a => a.Id).ToList();

        var totalIncome = entries.Where(e => incomes.Contains(e.AccountId)).Sum(e => e.CreditBase - e.DebitBase);
        var totalExpense = entries.Where(e => expenses.Contains(e.AccountId)).Sum(e => e.DebitBase - e.CreditBase);
        var currentProfit = totalIncome - totalExpense;

        decimal totalAssets = 0;
        var assetSection = new FinancialStatementSectionDto { SectionName = "Aktivlər (Assets)" };
        foreach (var a in assets)
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.DebitBase - e.CreditBase);
            if (bal != 0)
            {
                assetSection.Accounts.Add(new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal });
                totalAssets += bal;
            }
        }
        assetSection.SubTotal = totalAssets;

        decimal totalLiabilities = 0;
        var liabSection = new FinancialStatementSectionDto { SectionName = "Öhdəliklər (Liabilities)" };
        foreach (var a in liabilities)
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (bal != 0)
            {
                liabSection.Accounts.Add(new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal });
                totalLiabilities += bal;
            }
        }
        liabSection.SubTotal = totalLiabilities;

        decimal totalEquity = 0;
        var eqSection = new FinancialStatementSectionDto { SectionName = "Kapital (Equity)" };
        foreach (var a in equities)
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (bal != 0)
            {
                eqSection.Accounts.Add(new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal });
                totalEquity += bal;
            }
        }
        eqSection.SubTotal = totalEquity;

        return new BalanceSheetReportDto
        {
            AsOfDate = asOfDate,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            RetainedEarningsCurrentYear = currentProfit,
            AssetSections = new() { assetSection },
            LiabilitySections = new() { liabSection },
            EquitySections = new() { eqSection }
        };
    }

    public async Task<IncomeStatementReportDto> GetIncomeStatementAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate >= fromDate.Date && e.PostingDate <= toDate.Date, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var revAccounts = accounts.Where(a => a.Category == AccountCategory.Income).ToList();
        var expAccounts = accounts.Where(a => a.Category == AccountCategory.Expense).ToList();

        decimal totalRevenue = 0;
        var revSection = new FinancialStatementSectionDto { SectionName = "Gəlirlər (Revenues)" };
        foreach (var a in revAccounts)
        {
            var amount = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (amount != 0)
            {
                revSection.Accounts.Add(new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = amount });
                totalRevenue += amount;
            }
        }
        revSection.SubTotal = totalRevenue;

        decimal totalCOGS = 0;
        decimal totalOpex = 0;
        var expSection = new FinancialStatementSectionDto { SectionName = "Xərclər (Operating Expenses & COGS)" };
        foreach (var a in expAccounts)
        {
            var amount = entries.Where(e => e.AccountId == a.Id).Sum(e => e.DebitBase - e.CreditBase);
            if (amount != 0)
            {
                expSection.Accounts.Add(new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = amount });
                if (a.Type == AccountType.COGS)
                {
                    totalCOGS += amount;
                }
                else
                {
                    totalOpex += amount;
                }
            }
        }
        expSection.SubTotal = totalCOGS + totalOpex;

        return new IncomeStatementReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalRevenue = totalRevenue,
            TotalCostOfGoodsSold = totalCOGS,
            TotalOperatingExpenses = totalOpex,
            RevenueSections = new() { revSection },
            ExpenseSections = new() { expSection }
        };
    }

    public async Task<AgingReportDto> GetAgingReportAsync(string partyType, DateTime asOfDate, CancellationToken ct = default)
    {
        var report = new AgingReportDto
        {
            AsOfDate = asOfDate,
            PartyType = partyType
        };

        if (partyType.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            var invoices = await _custInvRepo.FindAsync(i => 
                i.PostingDate <= asOfDate.Date && 
                i.DocumentStatus == DocumentStatus.Posted && 
                i.OutstandingAmount > 0, ct);

            var customers = (await _customerRepo.GetAllAsync(ct)).ToDictionary(c => c.Id);

            var groups = invoices.GroupBy(i => i.CustomerId);
            foreach (var g in groups)
            {
                customers.TryGetValue(g.Key, out var customer);
                var partySummary = new AgingPartySummaryDto
                {
                    PartyId = g.Key,
                    PartyCode = customer?.Code ?? "",
                    PartyName = customer?.Name ?? "Unknown"
                };

                foreach (var inv in g)
                {
                    var daysOverdue = (asOfDate.Date - inv.DueDate.Date).Days;
                    if (daysOverdue <= 0) partySummary.CurrentNotDue += inv.OutstandingAmount;
                    else if (daysOverdue <= 30) partySummary.Days1To30 += inv.OutstandingAmount;
                    else if (daysOverdue <= 60) partySummary.Days31To60 += inv.OutstandingAmount;
                    else if (daysOverdue <= 90) partySummary.Days61To90 += inv.OutstandingAmount;
                    else partySummary.Days90Plus += inv.OutstandingAmount;

                    partySummary.TotalOutstanding += inv.OutstandingAmount;
                }

                report.Parties.Add(partySummary);
            }
        }
        else // Supplier
        {
            var invoices = await _suppInvRepo.FindAsync(i => 
                i.PostingDate <= asOfDate.Date && 
                i.DocumentStatus == DocumentStatus.Posted && 
                i.OutstandingAmount > 0, ct);

            var suppliers = (await _supplierRepo.GetAllAsync(ct)).ToDictionary(s => s.Id);

            var groups = invoices.GroupBy(i => i.SupplierId);
            foreach (var g in groups)
            {
                suppliers.TryGetValue(g.Key, out var supplier);
                var partySummary = new AgingPartySummaryDto
                {
                    PartyId = g.Key,
                    PartyCode = supplier?.Code ?? "",
                    PartyName = supplier?.Name ?? "Unknown"
                };

                foreach (var inv in g)
                {
                    var daysOverdue = (asOfDate.Date - inv.DueDate.Date).Days;
                    if (daysOverdue <= 0) partySummary.CurrentNotDue += inv.OutstandingAmount;
                    else if (daysOverdue <= 30) partySummary.Days1To30 += inv.OutstandingAmount;
                    else if (daysOverdue <= 60) partySummary.Days31To60 += inv.OutstandingAmount;
                    else if (daysOverdue <= 90) partySummary.Days61To90 += inv.OutstandingAmount;
                    else partySummary.Days90Plus += inv.OutstandingAmount;

                    partySummary.TotalOutstanding += inv.OutstandingAmount;
                }

                report.Parties.Add(partySummary);
            }
        }

        report.TotalOutstanding = report.Parties.Sum(p => p.TotalOutstanding);
        report.CurrentNotDue = report.Parties.Sum(p => p.CurrentNotDue);
        report.Days1To30 = report.Parties.Sum(p => p.Days1To30);
        report.Days31To60 = report.Parties.Sum(p => p.Days31To60);
        report.Days61To90 = report.Parties.Sum(p => p.Days61To90);
        report.Days90Plus = report.Parties.Sum(p => p.Days90Plus);

        return report;
    }

    public async Task<SubledgerReconciliationReportDto> GetSubledgerReconciliationAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var company = (await _companyRepo.GetAllAsync(ct)).FirstOrDefault();
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate <= asOfDate.Date, ct);

        // 1. AR Reconciliation
        var arInvoices = await _custInvRepo.FindAsync(i => i.PostingDate <= asOfDate.Date && i.DocumentStatus == DocumentStatus.Posted, ct);
        var arSubledgerTotal = arInvoices.Sum(i => i.OutstandingAmount);

        decimal arGLBalance = 0;
        if (company?.DefaultReceivableAccountId.HasValue == true)
        {
            arGLBalance = entries.Where(e => e.AccountId == company.DefaultReceivableAccountId.Value)
                                 .Sum(e => e.DebitBase - e.CreditBase);
        }

        // 2. AP Reconciliation
        var apInvoices = await _suppInvRepo.FindAsync(i => i.PostingDate <= asOfDate.Date && i.DocumentStatus == DocumentStatus.Posted, ct);
        var apSubledgerTotal = apInvoices.Sum(i => i.OutstandingAmount);

        decimal apGLBalance = 0;
        if (company?.DefaultPayableAccountId.HasValue == true)
        {
            apGLBalance = entries.Where(e => e.AccountId == company.DefaultPayableAccountId.Value)
                                 .Sum(e => e.CreditBase - e.DebitBase);
        }

        // 3. Stock Reconciliation
        var stockEntries = await _stockLedgerRepo.FindAsync(e => e.PostingDate <= asOfDate.Date, ct);
        var stockLedgerTotal = stockEntries.GroupBy(e => new { e.ItemId, e.WarehouseId })
                                           .Select(g => g.OrderByDescending(x => x.TransactionTime).FirstOrDefault())
                                           .Sum(x => x?.BalanceValue ?? 0);

        decimal inventoryGLBalance = 0;
        if (company?.DefaultStockAccountId.HasValue == true)
        {
            inventoryGLBalance = entries.Where(e => e.AccountId == company.DefaultStockAccountId.Value)
                                        .Sum(e => e.DebitBase - e.CreditBase);
        }

        // 4. Bank Book Reconciliation
        var banks = await _bankRepo.GetAllAsync(ct);
        decimal bankBookLedgerBalance = 0;
        foreach (var b in banks)
        {
            bankBookLedgerBalance += entries.Where(e => e.AccountId == b.GLAccountId)
                                           .Sum(e => e.DebitBase - e.CreditBase);
        }

        var latestStatements = (await _statementRepo.GetAllAsync(ct))
            .GroupBy(s => s.BankAccountId)
            .Select(g => g.OrderByDescending(s => s.StatementDate).FirstOrDefault())
            .Sum(s => s?.ClosingBalance ?? 0);

        return new SubledgerReconciliationReportDto
        {
            AsOfDate = asOfDate,
            ARSubledgerTotal = arSubledgerTotal,
            ARGLControlAccountBalance = arGLBalance,
            APSubledgerTotal = apSubledgerTotal,
            APGLControlAccountBalance = apGLBalance,
            StockLedgerTotalValue = stockLedgerTotal,
            InventoryGLAccountBalance = inventoryGLBalance,
            BankBookLedgerBalance = bankBookLedgerBalance,
            BankStatementClosingBalance = latestStatements
        };
    }
}
