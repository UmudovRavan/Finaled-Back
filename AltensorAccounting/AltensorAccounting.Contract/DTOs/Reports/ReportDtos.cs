using System;
using System.Collections.Generic;

namespace AltensorAccounting.Contract.DTOs.Reports;

public class TrialBalanceReportDto
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.001m;
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
}

public class TrialBalanceLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal NetBalance => Debit - Credit;
}

public class BalanceSheetReportDto
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal RetainedEarningsCurrentYear { get; set; }
    public bool EquationHolds => Math.Abs(TotalAssets - (TotalLiabilities + TotalEquity + RetainedEarningsCurrentYear)) < 0.001m;
    public List<FinancialStatementSectionDto> AssetSections { get; set; } = new();
    public List<FinancialStatementSectionDto> LiabilitySections { get; set; } = new();
    public List<FinancialStatementSectionDto> EquitySections { get; set; } = new();
}

public class IncomeStatementReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal GrossProfit => TotalRevenue - TotalCostOfGoodsSold;
    public decimal TotalOperatingExpenses { get; set; }
    public decimal NetOperatingProfit => GrossProfit - TotalOperatingExpenses;
    public decimal NetProfit => NetOperatingProfit;
    public List<FinancialStatementSectionDto> RevenueSections { get; set; } = new();
    public List<FinancialStatementSectionDto> ExpenseSections { get; set; } = new();
}

public class FinancialStatementSectionDto
{
    public string SectionName { get; set; } = default!;
    public decimal SubTotal { get; set; }
    public List<FinancialStatementAccountDto> Accounts { get; set; } = new();
}

public class FinancialStatementAccountDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
}

public class AgingReportDto
{
    public DateTime AsOfDate { get; set; }
    public string PartyType { get; set; } = default!; // "Customer" or "Supplier"
    public decimal TotalOutstanding { get; set; }
    public decimal CurrentNotDue { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public List<AgingPartySummaryDto> Parties { get; set; } = new();
}

public class AgingPartySummaryDto
{
    public Guid PartyId { get; set; }
    public string PartyCode { get; set; } = default!;
    public string PartyName { get; set; } = default!;
    public decimal TotalOutstanding { get; set; }
    public decimal CurrentNotDue { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
}

public class SubledgerReconciliationReportDto
{
    public DateTime AsOfDate { get; set; }

    // AR Reconciliation
    public decimal ARSubledgerTotal { get; set; }
    public decimal ARGLControlAccountBalance { get; set; }
    public decimal ARDifference => ARSubledgerTotal - ARGLControlAccountBalance;
    public bool ARMatches => Math.Abs(ARDifference) < 0.001m;

    // AP Reconciliation
    public decimal APSubledgerTotal { get; set; }
    public decimal APGLControlAccountBalance { get; set; }
    public decimal APDifference => APSubledgerTotal - APGLControlAccountBalance;
    public bool APMatches => Math.Abs(APDifference) < 0.001m;

    // Stock Reconciliation
    public decimal StockLedgerTotalValue { get; set; }
    public decimal InventoryGLAccountBalance { get; set; }
    public decimal StockDifference => StockLedgerTotalValue - InventoryGLAccountBalance;
    public bool StockMatches => Math.Abs(StockDifference) < 0.001m;

    // Bank Book Reconciliation
    public decimal BankBookLedgerBalance { get; set; }
    public decimal BankStatementClosingBalance { get; set; }
    public decimal BankDifference => BankBookLedgerBalance - BankStatementClosingBalance;
    public bool BankMatches => Math.Abs(BankDifference) < 0.001m;
}
