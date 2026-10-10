using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Contract.DTOs.Inventory;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Contract.DTOs.Reports;
using AltensorAccounting.Contract.DTOs.Sales;
using AltensorAccounting.Contract.DTOs.MasterData;
using AltensorAccounting.Contract.DTOs.Treasury;
using AltensorAccounting.Contract.DTOs.Webhooks;

namespace AltensorAccounting.Application.Interfaces;

public interface IAccountingService
{
    // Chart of Accounts
    Task<List<AccountDto>> GetAccountsAsync(CancellationToken ct = default);
    Task<List<AccountTreeNodeDto>> GetAccountTreeAsync(CancellationToken ct = default);
    Task<List<AccountBalanceRowDto>> GetAccountBalancesAsync(DateTime? fromDate, DateTime? toDate, string? search, bool includeZeroBalance, string? currency, CancellationToken ct = default);
    Task<List<AccountTypeOptionDto>> GetAccountTypesAsync(CancellationToken ct = default);
    Task<List<CategorySubcategoryMappingDto>> GetSubcategoriesAsync(CancellationToken ct = default);
    Task<AccountDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct = default);

    // Initial Balances (Configuration / Setup)
    Task SetInitialBalancesAsync(SetInitialBalancesDto dto, CancellationToken ct = default);

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
    Task<List<CustomerInvoiceDto>> GetCustomerInvoicesAsync(CancellationToken ct = default);
    Task<CustomerInvoiceDto?> GetCustomerInvoiceByIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<CustomerInvoiceDto> PostCustomerInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default);
    Task<PaymentDto> PostPaymentAsync(Guid paymentId, CancellationToken ct = default);

    // Company Profile & Header (Dashboard)
    Task<CompanyProfileDto> GetCompanyProfileAsync(CancellationToken ct = default);
    Task<CompanyProfileDto> UpdateCompanyProfileAsync(UpdateCompanyProfileDto dto, CancellationToken ct = default);
    Task<CompanyHeaderDto> GetCompanyHeaderAsync(CancellationToken ct = default);

    // Tenant Defaults & Seed Template
    Task SeedTemplateAsync(CancellationToken ct = default);
    Task<CompanyDefaultAccountsDto> GetDefaultAccountsAsync(CancellationToken ct = default);
    Task<CompanyDefaultAccountsDto> UpdateDefaultAccountsAsync(CompanyDefaultAccountsDto dto, CancellationToken ct = default);
}

public interface IProcurementService
{
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken ct = default);
    Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct = default);

    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default);
    Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken ct = default);
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<GoodsReceiptDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto, CancellationToken ct = default);
    Task<List<GoodsReceiptDto>> GetGoodsReceiptsAsync(CancellationToken ct = default);
    Task<GoodsReceiptDto> PostGoodsReceiptAsync(Guid receiptId, CancellationToken ct = default);

    Task<SupplierInvoiceDto> CreateSupplierInvoiceAsync(CreateSupplierInvoiceDto dto, CancellationToken ct = default);
    Task<List<SupplierInvoiceDto>> GetSupplierInvoicesAsync(CancellationToken ct = default);
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
    Task<List<CashDeskDto>> GetCashDesksAsync(CancellationToken ct = default);
    Task<BankStatementDto> ImportBankStatementAsync(ImportBankStatementDto dto, CancellationToken ct = default);
    Task<PaymentRunDto> CreatePaymentRunAsync(CreatePaymentRunDto dto, CancellationToken ct = default);
    Task<PaymentRunDto> PostPaymentRunAsync(Guid runId, CancellationToken ct = default);
}

public interface IReportService
{
    // Trial Balance (Dövr və 3 pilləli DR/CR qalıq görünüşü ilə)
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime? fromDate = null, DateTime? toDate = null, string? search = null, bool includeZeroBalance = false, string? currency = "AZN", CancellationToken ct = default);

    // 4 Əsas Maliyyə Hesabatı (IFRS)
    Task<FinancialPositionReportDto> GetFinancialPositionAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<ProfitOrLossReportDto> GetProfitOrLossAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<ChangesInEquityReportDto> GetChangesInEquityAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<CashFlowReportDto> GetCashFlowAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    // Əlavə hesabatlar
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<IncomeStatementReportDto> GetIncomeStatementAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<AgingReportDto> GetAgingReportAsync(string partyType, DateTime asOfDate, CancellationToken ct = default);
    Task<SubledgerReconciliationReportDto> GetSubledgerReconciliationAsync(DateTime asOfDate, CancellationToken ct = default);
}

public interface ISalesService
{
    // 1) Satış Sifarişləri (Sales Orders)
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken ct = default);
    Task<List<SalesOrderDto>> GetSalesOrdersAsync(CancellationToken ct = default);
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<SalesOrderDto> ConfirmSalesOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<SalesOrderDto> CancelSalesOrderAsync(Guid orderId, CancellationToken ct = default);

    // 2) Mal Təhvili / Göndərişi (Delivery Note / Goods Issue)
    Task<DeliveryNoteDto> CreateDeliveryNoteAsync(CreateDeliveryNoteDto dto, CancellationToken ct = default);
    Task<List<DeliveryNoteDto>> GetDeliveryNotesAsync(CancellationToken ct = default);
    Task<DeliveryNoteDto?> GetDeliveryNoteByIdAsync(Guid deliveryId, CancellationToken ct = default);
    Task<DeliveryNoteDto> PostDeliveryNoteAsync(Guid deliveryId, CancellationToken ct = default);
}

public interface IMasterDataService
{
    Task<MasterDataSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<List<MasterDataTabItemDto>> GetTabsAsync(CancellationToken ct = default);
}

public interface IUserSyncService
{
    Task SyncUserCreatedAsync(UserCreatedIntegrationEvent @event, CancellationToken ct = default);
}
