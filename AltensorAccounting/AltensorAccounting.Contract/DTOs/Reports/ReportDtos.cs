using System;
using System.Collections.Generic;

namespace AltensorAccounting.Contract.DTOs.Reports;

public class TrialBalanceReportDto
{
    public DateTime? FromDate { get; set; }
    public DateTime AsOfDate { get; set; }
    public string? QuickPeriod { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal TotalOpeningDebit { get; set; }
    public decimal TotalOpeningCredit { get; set; }
    public decimal TotalTurnoverDebit { get; set; }
    public decimal TotalTurnoverCredit { get; set; }
    public decimal TotalClosingDebit { get; set; }
    public decimal TotalClosingCredit { get; set; }

    // Legacy fields for backward compatibility
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => Math.Abs(TotalClosingDebit - TotalClosingCredit) < 0.001m;
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
}

public class TrialBalanceLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = default!;
    public string AccountName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string? SubcategoryName { get; set; }
    public int Level { get; set; } = 0;
    public bool IsLeaf { get; set; } = true;

    // 3 Mərhələli qalıq görünüşü (DR / CR)
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal TurnoverDebit { get; set; }
    public decimal TurnoverCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }

    // Legacy
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal NetBalance => ClosingDebit - ClosingCredit;
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

// 1. Maliyyə Vəziyyəti Haqqında Hesabat (Statement of Financial Position / Balans)
public class FinancialPositionReportDto
{
    public DateTime AsOfDate { get; set; }
    public string Currency { get; set; } = "AZN";

    // Aktivlər
    public decimal NonCurrentAssetsTotal { get; set; }
    public List<FinancialStatementAccountDto> NonCurrentAssets { get; set; } = new();

    public decimal CurrentAssetsTotal { get; set; }
    public List<FinancialStatementAccountDto> CurrentAssets { get; set; } = new();

    public decimal TotalAssets => NonCurrentAssetsTotal + CurrentAssetsTotal;

    // Öhdəliklər
    public decimal NonCurrentLiabilitiesTotal { get; set; }
    public List<FinancialStatementAccountDto> NonCurrentLiabilities { get; set; } = new();

    public decimal CurrentLiabilitiesTotal { get; set; }
    public List<FinancialStatementAccountDto> CurrentLiabilities { get; set; } = new();

    public decimal TotalLiabilities => NonCurrentLiabilitiesTotal + CurrentLiabilitiesTotal;

    // Kapital
    public decimal ShareCapitalTotal { get; set; }
    public List<FinancialStatementAccountDto> ShareCapitalAccounts { get; set; } = new();

    public decimal RetainedEarningsTotal { get; set; }
    public List<FinancialStatementAccountDto> RetainedEarningsAccounts { get; set; } = new();

    public decimal OtherEquityReservesTotal { get; set; }
    public List<FinancialStatementAccountDto> OtherEquityAccounts { get; set; } = new();

    public decimal CurrentYearProfitOrLoss { get; set; }
    public decimal TotalEquity => ShareCapitalTotal + RetainedEarningsTotal + OtherEquityReservesTotal + CurrentYearProfitOrLoss;

    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool EquationHolds => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.001m;
}

// 2. Mənfəət və ya Zərər Haqqında Hesabat (Statement of Profit or Loss)
public class ProfitOrLossReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Currency { get; set; } = "AZN";

    // Əsas fəaliyyət gəlirləri
    public decimal OperatingRevenueTotal { get; set; }
    public List<FinancialStatementAccountDto> OperatingRevenueItems { get; set; } = new();

    // Satışın Maya Dəyəri (COGS)
    public decimal CostOfGoodsSoldTotal { get; set; }
    public List<FinancialStatementAccountDto> CostOfGoodsSoldItems { get; set; } = new();

    // Ümumi mənfəət / zərər (Gross Profit)
    public decimal GrossProfit => OperatingRevenueTotal - CostOfGoodsSoldTotal;

    // Digər əməliyyat gəlirləri
    public decimal OtherOperatingIncomeTotal { get; set; }
    public List<FinancialStatementAccountDto> OtherOperatingIncomeItems { get; set; } = new();

    // Satış və marketinq xərcləri
    public decimal SellingAndMarketingExpensesTotal { get; set; }
    public List<FinancialStatementAccountDto> SellingAndMarketingExpensesItems { get; set; } = new();

    // İnzibati xərclər
    public decimal AdministrativeExpensesTotal { get; set; }
    public List<FinancialStatementAccountDto> AdministrativeExpensesItems { get; set; } = new();

    // Əməliyyat mənfəəti (Operating Profit / EBIT)
    public decimal OperatingProfit => GrossProfit + OtherOperatingIncomeTotal - SellingAndMarketingExpensesTotal - AdministrativeExpensesTotal;

    // Maliyyə gəlirləri
    public decimal FinancialIncomeTotal { get; set; }
    public List<FinancialStatementAccountDto> FinancialIncomeItems { get; set; } = new();

    // Maliyyə xərcləri
    public decimal FinancialExpensesTotal { get; set; }
    public List<FinancialStatementAccountDto> FinancialExpensesItems { get; set; } = new();

    // Vergidən əvvəlki mənfəət (Profit Before Tax)
    public decimal ProfitBeforeTax => OperatingProfit + FinancialIncomeTotal - FinancialExpensesTotal;

    // Vergi xərcləri
    public decimal TaxExpensesTotal { get; set; }
    public List<FinancialStatementAccountDto> TaxExpensesItems { get; set; } = new();

    // Digər gəlir / xərclər
    public decimal OtherIncomeTotal { get; set; }
    public decimal OtherExpensesTotal { get; set; }

    // Xalis mənfəət / zərər (Net Profit / Loss for the Period)
    public decimal NetProfitOrLoss => ProfitBeforeTax - TaxExpensesTotal + OtherIncomeTotal - OtherExpensesTotal;
}

// 3. Kapitalda Dəyişikliklər Haqqında Hesabat (Statement of Changes in Equity)
public class ChangesInEquityReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Currency { get; set; } = "AZN";

    public EquityMovementRowDto OpeningBalance { get; set; } = new();
    public EquityMovementRowDto NetProfitForPeriod { get; set; } = new();
    public EquityMovementRowDto CapitalContributions { get; set; } = new();
    public EquityMovementRowDto DividendsDistributed { get; set; } = new();
    public EquityMovementRowDto ClosingBalance { get; set; } = new();
}

public class EquityMovementRowDto
{
    public string Description { get; set; } = default!;
    public decimal ShareCapital { get; set; }
    public decimal RetainedEarnings { get; set; }
    public decimal OtherReserves { get; set; }
    public decimal TotalEquity => ShareCapital + RetainedEarnings + OtherReserves;
}

// 4. Pul Vəsaitlərinin Hərəkəti Haqqında Hesabat (Statement of Cash Flows)
public class CashFlowReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Currency { get; set; } = "AZN";

    // Əməliyyat Fəaliyyəti (Operating Activities)
    public decimal CustomerReceipts { get; set; }
    public decimal SupplierPayments { get; set; }
    public decimal OperatingCashExpenses { get; set; }
    public decimal NetCashFromOperatingActivities => CustomerReceipts - SupplierPayments - OperatingCashExpenses;

    // İnvestisiya Fəaliyyəti (Investing Activities)
    public decimal FixedAssetPurchases { get; set; }
    public decimal FixedAssetSales { get; set; }
    public decimal NetCashFromInvestingActivities => FixedAssetSales - FixedAssetPurchases;

    // Maliyyələşdirmə Fəaliyyəti (Financing Activities)
    public decimal LoansReceived { get; set; }
    public decimal LoansRepaid { get; set; }
    public decimal CapitalInjections { get; set; }
    public decimal NetCashFromFinancingActivities => LoansReceived - LoansRepaid + CapitalInjections;

    // Xülasə
    public decimal NetIncreaseInCash => NetCashFromOperatingActivities + NetCashFromInvestingActivities + NetCashFromFinancingActivities;
    public decimal OpeningCashAndEquivalents { get; set; }
    public decimal ClosingCashAndEquivalents => OpeningCashAndEquivalents + NetIncreaseInCash;
}

