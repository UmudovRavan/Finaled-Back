namespace AltensorAccounting.Domain.Enums;

public enum AccountCategory
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public enum AccountSubcategory
{
    // Aktivlər (Asset 1-2)
    NonCurrentAssets = 1,             // 1. Uzunmüddətli aktivlər
    CurrentAssets = 2,                // 2. Dövriyyə aktivləri

    // Öhdəliklər (Liability 3-4)
    NonCurrentLiabilities = 3,        // 3. Uzunmüddətli öhdəliklər
    CurrentLiabilities = 4,           // 4. Qısamüddətli öhdəliklər

    // Kapital (Equity 5-7)
    ShareCapital = 5,                 // 5. Nizamnamə kapitalı
    RetainedEarnings = 6,             // 6. Bölüşdürülməmiş mənfəət / zərər
    OtherEquityAndReserves = 7,       // 7. Digər kapital və ehtiyatlar

    // Gəlirlər (Income 8-11)
    OperatingRevenue = 8,             // 8. Əsas fəaliyyət gəlirləri
    OtherOperatingIncome = 9,         // 9. Digər əməliyyat gəlirləri
    FinancialIncome = 10,             // 10. Maliyyə gəlirləri
    OtherIncome = 11,                 // 11. Digər gəlirlər

    // Xərclər (Expense 12-17)
    CostOfGoodsSold = 12,             // 12. Satışın maya dəyəri (COGS)
    SellingAndMarketingExpenses = 13, // 13. Satış və marketinq xərcləri
    AdministrativeExpenses = 14,      // 14. İnzibati xərclər
    FinancialExpenses = 15,           // 15. Maliyyə xərcləri
    TaxExpenses = 16,                 // 16. Vergi xərcləri
    OtherExpenses = 17                // 17. Digər xərclər
}

public enum AccountType
{
    Standard = 0,
    Receivable = 1,
    Payable = 2,
    Bank = 3,
    Cash = 4,
    Stock = 5,
    GRNI = 6,                          // Goods Received Not Invoiced / Accrued Purchases
    COGS = 7,                          // Satışın Maya Dəyəri (COGS)
    Tax = 8,
    RetainedEarnings = 9,
    FixedAsset = 12,
    AccumulatedDepreciation = 16,      // Yığılmış amortizasiya
    AccountablePersons = 17,           // Təhtəlhesab məbləğlər
    AdvancesGiven = 18,                // Verilmiş avanslar
    AdvancesReceived = 19,             // Alınmış avanslar
    BankLoans = 20,                    // Bank kreditləri

    // Köhnə data üçün geriyə uyğunluq (deprecated/obsolete)
    [Obsolete("Kateqoriya və ya Subkateqoriyadan istifadə edin")]
    Revenue = 10,
    [Obsolete("Kateqoriya və ya Subkateqoriyadan istifadə edin")]
    Expense = 11,
    [Obsolete("Kateqoriya və ya Subkateqoriyadan istifadə edin")]
    CurrentAsset = 13,
    [Obsolete("Kateqoriya və ya Subkateqoriyadan istifadə edin")]
    CurrentLiability = 14,
    [Obsolete("Kateqoriya və ya Subkateqoriyadan istifadə edin")]
    LongTermLiability = 15
}

public enum FiscalPeriodStatus
{
    Open = 1,
    SoftClosed = 2,
    Closed = 3,
    Locked = 4
}

public enum DocumentType
{
    ManualJournal = 1,
    CustomerInvoice = 2,
    SupplierInvoice = 3,
    CustomerPayment = 4,
    SupplierPayment = 5,
    GoodsReceipt = 6,
    StockIssue = 7,
    StockTransfer = 8,
    StockAdjustment = 9,
    CustomerCreditNote = 10,
    SupplierDebitNote = 11,
    LandedCost = 12,
    FXRevaluation = 13,
    YearEndClose = 14,
    SalesOrder = 15,
    DeliveryNote = 16,
    OpeningBalance = 17
}

public enum SalesOrderStatus
{
    Draft = 1,
    Confirmed = 2,
    PartiallyDelivered = 3,
    FullyDelivered = 4,
    Invoiced = 5,
    Cancelled = 6
}

public enum DeliveryNoteStatus
{
    Draft = 1,
    Posted = 2,
    Invoiced = 3,
    Cancelled = 4
}

public enum DocumentStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Posted = 4,
    Cancelled = 5
}

public enum SettlementStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4
}

public enum PaymentType
{
    CustomerReceipt = 1,
    CustomerAdvance = 2,
    SupplierPayment = 3,
    SupplierAdvance = 4,
    InternalTransfer = 5
}

public enum DimensionType
{
    CostCenter = 1,
    Project = 2,
    Department = 3,
    Branch = 4,
    Custom = 5
}

public enum ItemType
{
    StockItem = 1,
    NonStockItem = 2,
    Service = 3
}

public enum ValuationMethod
{
    MovingAverage = 1,
    FIFO = 2
}

public enum StockTransactionType
{
    Receipt = 1,
    Issue = 2,
    Transfer = 3,
    Adjustment = 4
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    PartiallyReceived = 4,
    FullyReceived = 5,
    Completed = 6,
    Cancelled = 7
}

public enum ThreeWayMatchStatus
{
    NotApplicable = 0,
    Pending = 1,
    Matched = 2,
    DiscrepancyWithinTolerance = 3,
    OnHoldToleranceExceeded = 4,
    OverrideApproved = 5
}

public enum ReconciliationStatus
{
    Unmatched = 1,
    Matched = 2,
    ManuallyAdjusted = 3
}
