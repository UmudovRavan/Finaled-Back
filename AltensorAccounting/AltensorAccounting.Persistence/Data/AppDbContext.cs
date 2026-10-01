using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Entities.Treasury;
using Microsoft.EntityFrameworkCore;

namespace AltensorAccounting.Persistence.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenantService _tenantService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    // Common
    public DbSet<User> Users => Set<User>();

    // Accounting Core
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PostingBatch> PostingBatches => Set<PostingBatch>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<ManualJournal> ManualJournals => Set<ManualJournal>();
    public DbSet<ManualJournalLine> ManualJournalLines => Set<ManualJournalLine>();
    public DbSet<AccountingDimension> AccountingDimensions => Set<AccountingDimension>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerInvoice> CustomerInvoices => Set<CustomerInvoice>();
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines => Set<CustomerInvoiceLine>();
    public DbSet<CustomerCreditNote> CustomerCreditNotes => Set<CustomerCreditNote>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    // Procurement
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceLine> SupplierInvoiceLines => Set<SupplierInvoiceLine>();
    public DbSet<SupplierDebitNote> SupplierDebitNotes => Set<SupplierDebitNote>();

    // Inventory
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemUomConversion> ItemUomConversions => Set<ItemUomConversion>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockTransactionLine> StockTransactionLines => Set<StockTransactionLine>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<CostLayer> CostLayers => Set<CostLayer>();
    public DbSet<LandedCostVoucher> LandedCostVouchers => Set<LandedCostVoucher>();
    public DbSet<LandedCostItem> LandedCostItems => Set<LandedCostItem>();
    public DbSet<StockReconciliation> StockReconciliations => Set<StockReconciliation>();
    public DbSet<StockReconciliationLine> StockReconciliationLines => Set<StockReconciliationLine>();

    // Treasury
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CashDesk> CashDesks => Set<CashDesk>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<PaymentRun> PaymentRuns => Set<PaymentRun>();
    public DbSet<PaymentRunItem> PaymentRunItems => Set<PaymentRunItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Global Query Filters for ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { modelBuilder, this });
            }
        }

        // Account self-referencing relationship
        modelBuilder.Entity<Account>()
            .HasOne(a => a.ParentAccount)
            .WithMany(a => a.SubAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ledger Entry Indexes for high performance financial reporting
        modelBuilder.Entity<LedgerEntry>()
            .HasIndex(e => new { e.TenantId, e.PostingDate, e.AccountId });

        modelBuilder.Entity<StockLedgerEntry>()
            .HasIndex(e => new { e.TenantId, e.ItemId, e.WarehouseId, e.TransactionTime });
    }

    private static void ConfigureTenantFilter<T>(ModelBuilder modelBuilder, AppDbContext context) where T : class, ITenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => 
            context._tenantService.IsPlatformSuperAdmin || 
            (context._tenantService.TenantId != null && e.TenantId == context._tenantService.TenantId.Value));
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.TenantId == Guid.Empty && tenantId.HasValue)
            {
                entry.Entity.TenantId = tenantId.Value;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Modified))
        {
            if (!_tenantService.IsPlatformSuperAdmin)
            {
                entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
