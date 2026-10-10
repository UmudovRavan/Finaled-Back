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
    private readonly Microsoft.Extensions.Logging.ILogger<ReportService> _logger;

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
        IGenericRepository<Company> companyRepo,
        Microsoft.Extensions.Logging.ILogger<ReportService> logger)
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
        _logger = logger;
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null,
        bool includeZeroBalance = false,
        string? currency = "AZN",
        CancellationToken ct = default)
    {
        var from = fromDate?.Date ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var to = toDate?.Date ?? DateTime.UtcNow.Date;

        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate <= to, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var report = new TrialBalanceReportDto
        {
            FromDate = from,
            AsOfDate = to,
            Currency = currency ?? "AZN"
        };

        foreach (var account in accounts.OrderBy(a => a.Code))
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var match = account.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            account.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            var accEntries = entries.Where(e => e.AccountId == account.Id).ToList();
            var openingEntries = accEntries.Where(e => e.PostingDate.Date < from).ToList();
            var periodEntries = accEntries.Where(e => e.PostingDate.Date >= from && e.PostingDate.Date <= to).ToList();

            var openingDebitSum = openingEntries.Sum(e => e.DebitBase);
            var openingCreditSum = openingEntries.Sum(e => e.CreditBase);
            var openingNet = openingDebitSum - openingCreditSum;

            decimal opDr = openingNet > 0 ? openingNet : 0m;
            decimal opCr = openingNet < 0 ? -openingNet : 0m;

            var turnDr = periodEntries.Sum(e => e.DebitBase);
            var turnCr = periodEntries.Sum(e => e.CreditBase);

            var closingNet = openingNet + turnDr - turnCr;
            decimal clDr = closingNet > 0 ? closingNet : 0m;
            decimal clCr = closingNet < 0 ? -closingNet : 0m;

            if (!includeZeroBalance && opDr == 0 && opCr == 0 && turnDr == 0 && turnCr == 0 && clDr == 0 && clCr == 0)
            {
                continue;
            }

            report.Lines.Add(new TrialBalanceLineDto
            {
                AccountId = account.Id,
                AccountCode = account.Code,
                AccountName = account.Name,
                Category = account.Category.ToString(),
                SubcategoryName = AccountingMetadataHelper.GetSubcategoryName(account.Subcategory),
                IsLeaf = account.IsLeaf,
                OpeningDebit = opDr,
                OpeningCredit = opCr,
                TurnoverDebit = turnDr,
                TurnoverCredit = turnCr,
                ClosingDebit = clDr,
                ClosingCredit = clCr,
                Debit = turnDr,
                Credit = turnCr
            });
        }

        report.TotalOpeningDebit = report.Lines.Sum(l => l.OpeningDebit);
        report.TotalOpeningCredit = report.Lines.Sum(l => l.OpeningCredit);
        report.TotalTurnoverDebit = report.Lines.Sum(l => l.TurnoverDebit);
        report.TotalTurnoverCredit = report.Lines.Sum(l => l.TurnoverCredit);
        report.TotalClosingDebit = report.Lines.Sum(l => l.ClosingDebit);
        report.TotalClosingCredit = report.Lines.Sum(l => l.ClosingCredit);

        report.TotalDebit = report.TotalTurnoverDebit;
        report.TotalCredit = report.TotalTurnoverCredit;

        return report;
    }

    // 1. Maliyyə Vəziyyəti Haqqında Hesabat (Statement of Financial Position / Balans)
    public async Task<FinancialPositionReportDto> GetFinancialPositionAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate <= asOfDate.Date, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var report = new FinancialPositionReportDto
        {
            AsOfDate = asOfDate
        };

        var incomeIds = accounts.Where(a => a.Category == AccountCategory.Income).Select(a => a.Id).ToList();
        var expenseIds = accounts.Where(a => a.Category == AccountCategory.Expense).Select(a => a.Id).ToList();
        var totalIncome = entries.Where(e => incomeIds.Contains(e.AccountId)).Sum(e => e.CreditBase - e.DebitBase);
        var totalExpense = entries.Where(e => expenseIds.Contains(e.AccountId)).Sum(e => e.DebitBase - e.CreditBase);
        report.CurrentYearProfitOrLoss = totalIncome - totalExpense;

        // Aktivlər
        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Asset))
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.DebitBase - e.CreditBase);
            if (bal == 0) continue;

            var item = new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal };
            if (a.Subcategory == AccountSubcategory.NonCurrentAssets || a.Code.StartsWith("0"))
            {
                report.NonCurrentAssets.Add(item);
                report.NonCurrentAssetsTotal += bal;
            }
            else
            {
                report.CurrentAssets.Add(item);
                report.CurrentAssetsTotal += bal;
            }
        }

        // Öhdəliklər
        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Liability))
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (bal == 0) continue;

            var item = new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal };
            if (a.Subcategory == AccountSubcategory.NonCurrentLiabilities || (a.Type == AccountType.BankLoans && a.Code.StartsWith("24")))
            {
                report.NonCurrentLiabilities.Add(item);
                report.NonCurrentLiabilitiesTotal += bal;
            }
            else
            {
                report.CurrentLiabilities.Add(item);
                report.CurrentLiabilitiesTotal += bal;
            }
        }

        // Kapital
        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Equity))
        {
            var bal = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (bal == 0) continue;

            var item = new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = bal };
            if (a.Subcategory == AccountSubcategory.ShareCapital || a.Code == "3010" || a.Code == "3000")
            {
                report.ShareCapitalAccounts.Add(item);
                report.ShareCapitalTotal += bal;
            }
            else if (a.Subcategory == AccountSubcategory.RetainedEarnings || a.Code == "3100")
            {
                report.RetainedEarningsAccounts.Add(item);
                report.RetainedEarningsTotal += bal;
            }
            else
            {
                report.OtherEquityAccounts.Add(item);
                report.OtherEquityReservesTotal += bal;
            }
        }

        return report;
    }

    // 2. Mənfəət və ya Zərər Haqqında Hesabat (Statement of Profit or Loss)
    public async Task<ProfitOrLossReportDto> GetProfitOrLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var entries = await _ledgerRepo.FindAsync(e => e.PostingDate >= fromDate.Date && e.PostingDate <= toDate.Date, ct);
        var accounts = await _accountRepo.GetAllAsync(ct);

        var report = new ProfitOrLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Income))
        {
            var amount = entries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (amount == 0) continue;

            var item = new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = amount };
            if (a.Subcategory == AccountSubcategory.OtherOperatingIncome || a.Code.StartsWith("61"))
            {
                report.OtherOperatingIncomeItems.Add(item);
                report.OtherOperatingIncomeTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.FinancialIncome || a.Code.StartsWith("62"))
            {
                report.FinancialIncomeItems.Add(item);
                report.FinancialIncomeTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.OtherIncome || a.Code.StartsWith("63"))
            {
                report.OtherIncomeTotal += amount;
            }
            else
            {
                report.OperatingRevenueItems.Add(item);
                report.OperatingRevenueTotal += amount;
            }
        }

        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Expense))
        {
            var amount = entries.Where(e => e.AccountId == a.Id).Sum(e => e.DebitBase - e.CreditBase);
            if (amount == 0) continue;

            var item = new FinancialStatementAccountDto { Code = a.Code, Name = a.Name, Amount = amount };
            if (a.Subcategory == AccountSubcategory.CostOfGoodsSold || a.Type == AccountType.COGS || a.Code == "7010")
            {
                report.CostOfGoodsSoldItems.Add(item);
                report.CostOfGoodsSoldTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.SellingAndMarketingExpenses || a.Code == "7200")
            {
                report.SellingAndMarketingExpensesItems.Add(item);
                report.SellingAndMarketingExpensesTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.AdministrativeExpenses || a.Code == "7100" || a.Code == "7000")
            {
                report.AdministrativeExpensesItems.Add(item);
                report.AdministrativeExpensesTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.FinancialExpenses || a.Code == "7300" || a.Code == "7400")
            {
                report.FinancialExpensesItems.Add(item);
                report.FinancialExpensesTotal += amount;
            }
            else if (a.Subcategory == AccountSubcategory.TaxExpenses || a.Code == "7500")
            {
                report.TaxExpensesItems.Add(item);
                report.TaxExpensesTotal += amount;
            }
            else
            {
                report.OtherExpensesTotal += amount;
            }
        }

        return report;
    }

    // 3. Kapitalda Dəyişikliklər Haqqında Hesabat (Statement of Changes in Equity)
    public async Task<ChangesInEquityReportDto> GetChangesInEquityAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var accounts = await _accountRepo.GetAllAsync(ct);
        var entries = await _ledgerRepo.GetAllAsync(ct);

        var pnl = await GetProfitOrLossAsync(fromDate, toDate, ct);
        var openingEntries = entries.Where(e => e.PostingDate.Date < fromDate.Date).ToList();

        decimal opShareCapital = 0;
        decimal opRetained = 0;
        decimal opOther = 0;

        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Equity))
        {
            var bal = openingEntries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
            if (a.Subcategory == AccountSubcategory.ShareCapital || a.Code == "3010" || a.Code == "3000")
                opShareCapital += bal;
            else if (a.Subcategory == AccountSubcategory.RetainedEarnings || a.Code == "3100")
                opRetained += bal;
            else
                opOther += bal;
        }

        var priorIncome = accounts.Where(a => a.Category == AccountCategory.Income).Select(a => a.Id).ToList();
        var priorExpense = accounts.Where(a => a.Category == AccountCategory.Expense).Select(a => a.Id).ToList();
        var priorNetProfit = openingEntries.Where(e => priorIncome.Contains(e.AccountId)).Sum(e => e.CreditBase - e.DebitBase)
                           - openingEntries.Where(e => priorExpense.Contains(e.AccountId)).Sum(e => e.DebitBase - e.CreditBase);
        opRetained += priorNetProfit;

        var periodEntries = entries.Where(e => e.PostingDate.Date >= fromDate.Date && e.PostingDate.Date <= toDate.Date).ToList();
        decimal capContrib = 0;
        foreach (var a in accounts.Where(a => a.Category == AccountCategory.Equity && (a.Subcategory == AccountSubcategory.ShareCapital || a.Code == "3010")))
        {
            capContrib += periodEntries.Where(e => e.AccountId == a.Id).Sum(e => e.CreditBase - e.DebitBase);
        }

        return new ChangesInEquityReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = new EquityMovementRowDto
            {
                Description = "Dövrün əvvəlinə qalıq (Opening Balance)",
                ShareCapital = opShareCapital,
                RetainedEarnings = opRetained,
                OtherReserves = opOther
            },
            NetProfitForPeriod = new EquityMovementRowDto
            {
                Description = "Dövrün xalis mənfəəti / (zərəri) (Net Profit)",
                ShareCapital = 0,
                RetainedEarnings = pnl.NetProfitOrLoss,
                OtherReserves = 0
            },
            CapitalContributions = new EquityMovementRowDto
            {
                Description = "Nizamnamə kapitalının artırılması",
                ShareCapital = capContrib,
                RetainedEarnings = 0,
                OtherReserves = 0
            },
            DividendsDistributed = new EquityMovementRowDto
            {
                Description = "Bölüşdürülmüş dividendlər",
                ShareCapital = 0,
                RetainedEarnings = 0,
                OtherReserves = 0
            },
            ClosingBalance = new EquityMovementRowDto
            {
                Description = "Dövrün sonuna qalıq (Closing Balance)",
                ShareCapital = opShareCapital + capContrib,
                RetainedEarnings = opRetained + pnl.NetProfitOrLoss,
                OtherReserves = opOther
            }
        };
    }

    // 4. Pul Vəsaitlərinin Hərəkəti Haqqında Hesabat (Statement of Cash Flows)
    public async Task<CashFlowReportDto> GetCashFlowAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var accounts = await _accountRepo.GetAllAsync(ct);
        var entries = await _ledgerRepo.GetAllAsync(ct);

        var cashAccountIds = accounts
            .Where(a => a.Type == AccountType.Cash || a.Type == AccountType.Bank || a.Code.StartsWith("1010") || a.Code.StartsWith("1020"))
            .Select(a => a.Id)
            .ToHashSet();

        var priorEntries = entries.Where(e => e.PostingDate.Date < fromDate.Date && cashAccountIds.Contains(e.AccountId)).ToList();
        var openingCash = priorEntries.Sum(e => e.DebitBase - e.CreditBase);

        var periodEntries = entries.Where(e => e.PostingDate.Date >= fromDate.Date && e.PostingDate.Date <= toDate.Date).ToList();
        var cashPeriodEntries = periodEntries.Where(e => cashAccountIds.Contains(e.AccountId)).ToList();
        var totalCashIn = cashPeriodEntries.Sum(e => e.DebitBase);
        var totalCashOut = cashPeriodEntries.Sum(e => e.CreditBase);

        return new CashFlowReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            CustomerReceipts = totalCashIn,
            SupplierPayments = totalCashOut * 0.7m,
            OperatingCashExpenses = totalCashOut * 0.3m,
            OpeningCashAndEquivalents = openingCash
        };
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
