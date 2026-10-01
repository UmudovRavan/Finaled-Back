using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Contract.DTOs.Inventory;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Contract.DTOs.Reports;
using AltensorAccounting.Contract.DTOs.Treasury;
using AltensorAccounting.Contract.DTOs.Webhooks;

namespace AltensorAccounting.Application.Interfaces;

public interface IAccountingService
{
    // Chart of Accounts
    Task<List<AccountDto>> GetAccountsAsync(CancellationToken ct = default);
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default);

    // Fiscal Periods
    Task<FiscalYearDto> CreateFiscalYearAsync(CreateFiscalYearDto dto, CancellationToken ct = default);
    Task<List<FiscalYearDto>> GetFiscalYearsAsync(CancellationToken ct = default);
    Task ClosePeriodAsync(Guid periodId, CancellationToken ct = default);

    // Manual Journals
    Task<ManualJournalDto> CreateManualJournalAsync(CreateManualJournalDto dto, CancellationToken ct = default);
    Task<ManualJournalDto> PostManualJournalAsync(Guid journalId, CancellationToken ct = default);
    Task<ManualJournalDto> ReverseManualJournalAsync(Guid journalId, string reason, DateTime reversalDate, CancellationToken ct = default);

    // Customers
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken ct = default);
    Task<List<CustomerDto>> GetCustomersAsync(CancellationToken ct = default);

    // Invoices & Payments
    Task<CustomerInvoiceDto> CreateCustomerInvoiceAsync(CreateCustomerInvoiceDto dto, CancellationToken ct = default);
    Task<CustomerInvoiceDto> PostCustomerInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default);
    Task<PaymentDto> PostPaymentAsync(Guid paymentId, CancellationToken ct = default);
}

public interface IProcurementService
{
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken ct = default);
    Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default);

    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default);
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<GoodsReceiptDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto, CancellationToken ct = default);
    Task<GoodsReceiptDto> PostGoodsReceiptAsync(Guid receiptId, CancellationToken ct = default);

    Task<SupplierInvoiceDto> CreateSupplierInvoiceAsync(CreateSupplierInvoiceDto dto, CancellationToken ct = default);
    Task<ThreeWayMatchResultDto> EvaluateThreeWayMatchAsync(Guid invoiceId, CancellationToken ct = default);
    Task<SupplierInvoiceDto> PostSupplierInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<ItemDto> CreateItemAsync(CreateItemDto dto, CancellationToken ct = default);
    Task<List<ItemDto>> GetItemsAsync(CancellationToken ct = default);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, CancellationToken ct = default);
    Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken ct = default);

    Task<StockTransactionDto> CreateStockTransactionAsync(CreateStockTransactionDto dto, CancellationToken ct = default);
    Task<StockTransactionDto> PostStockTransactionAsync(Guid transactionId, CancellationToken ct = default);
    Task<List<StockLedgerEntryDto>> GetStockLedgerAsync(Guid? itemId, Guid? warehouseId, CancellationToken ct = default);
}

public interface ITreasuryService
{
    Task<BankAccountDto> CreateBankAccountAsync(CreateBankAccountDto dto, CancellationToken ct = default);
    Task<List<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default);
    Task<CashDeskDto> CreateCashDeskAsync(CreateCashDeskDto dto, CancellationToken ct = default);
    Task<BankStatementDto> ImportBankStatementAsync(ImportBankStatementDto dto, CancellationToken ct = default);
    Task<PaymentRunDto> CreatePaymentRunAsync(CreatePaymentRunDto dto, CancellationToken ct = default);
    Task<PaymentRunDto> PostPaymentRunAsync(Guid runId, CancellationToken ct = default);
}

public interface IReportService
{
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<IncomeStatementReportDto> GetIncomeStatementAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<AgingReportDto> GetAgingReportAsync(string partyType, DateTime asOfDate, CancellationToken ct = default);
    Task<SubledgerReconciliationReportDto> GetSubledgerReconciliationAsync(DateTime asOfDate, CancellationToken ct = default);
}

public interface IUserSyncService
{
    Task SyncUserCreatedAsync(UserCreatedIntegrationEvent @event, CancellationToken ct = default);
}
