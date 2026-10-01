namespace AltensorAccounting.Domain.Enums;

public enum AccountCategory
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public enum AccountType
{
    Standard = 0,
    Receivable = 1,
    Payable = 2,
    Bank = 3,
    Cash = 4,
    Stock = 5,
    GRNI = 6,               // Goods Received Not Invoiced / Accrued Purchases
    COGS = 7,               // Cost of Goods Sold
    Tax = 8,
    RetainedEarnings = 9,
    Revenue = 10,
    Expense = 11,
    FixedAsset = 12,
    CurrentAsset = 13,
    CurrentLiability = 14,
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
    YearEndClose = 14
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
