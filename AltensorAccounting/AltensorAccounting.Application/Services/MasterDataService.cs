using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.MasterData;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Inventory;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Entities.Treasury;

namespace AltensorAccounting.Application.Services;

public class MasterDataService : IMasterDataService
{
    private readonly IGenericRepository<Customer> _customerRepo;
    private readonly IGenericRepository<Supplier> _supplierRepo;
    private readonly IGenericRepository<Warehouse> _warehouseRepo;
    private readonly IGenericRepository<Item> _itemRepo;
    private readonly IGenericRepository<BankAccount> _bankRepo;
    private readonly IGenericRepository<TaxCode> _taxCodeRepo;
    private readonly IGenericRepository<Account> _accountRepo;

    public MasterDataService(
        IGenericRepository<Customer> customerRepo,
        IGenericRepository<Supplier> supplierRepo,
        IGenericRepository<Warehouse> warehouseRepo,
        IGenericRepository<Item> itemRepo,
        IGenericRepository<BankAccount> bankRepo,
        IGenericRepository<TaxCode> taxCodeRepo,
        IGenericRepository<Account> accountRepo)
    {
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _warehouseRepo = warehouseRepo;
        _itemRepo = itemRepo;
        _bankRepo = bankRepo;
        _taxCodeRepo = taxCodeRepo;
        _accountRepo = accountRepo;
    }

    public async Task<MasterDataSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var customers = _customerRepo.Query().Count();
        var suppliers = _supplierRepo.Query().Count();
        var warehouses = _warehouseRepo.Query().Count();
        var items = _itemRepo.Query().Count();
        var banks = _bankRepo.Query().Count();
        var taxes = _taxCodeRepo.Query().Count();
        var accounts = _accountRepo.Query().Count();

        return new MasterDataSummaryDto
        {
            CustomersCount = customers,
            SuppliersCount = suppliers,
            WarehousesCount = warehouses,
            ItemsCount = items,
            FixedAssetsCount = 0,
            BankAccountsCount = banks,
            TaxCodesCount = taxes,
            AccountsCount = accounts
        };
    }

    public async Task<List<MasterDataTabItemDto>> GetTabsAsync(CancellationToken ct = default)
    {
        var summary = await GetSummaryAsync(ct);

        return new List<MasterDataTabItemDto>
        {
            new() { Key = "customers", TitleAz = "Müştərilər", TitleEn = "Customers", Count = summary.CustomersCount, ApiEndpoint = "/api/master-data/customers" },
            new() { Key = "suppliers", TitleAz = "Təchizatçılar", TitleEn = "Suppliers", Count = summary.SuppliersCount, ApiEndpoint = "/api/master-data/suppliers" },
            new() { Key = "warehouses", TitleAz = "Anbarlar", TitleEn = "Warehouses", Count = summary.WarehousesCount, ApiEndpoint = "/api/master-data/warehouses" },
            new() { Key = "items", TitleAz = "Məhsul və Xidmətlər", TitleEn = "Items & Services", Count = summary.ItemsCount, ApiEndpoint = "/api/master-data/items" },
            new() { Key = "bank-accounts", TitleAz = "Banklar və Hesablar", TitleEn = "Banks & Accounts", Count = summary.BankAccountsCount, ApiEndpoint = "/api/master-data/bank-accounts" },
            new() { Key = "tax-codes", TitleAz = "Vergi Kodları", TitleEn = "Tax Codes", Count = summary.TaxCodesCount, ApiEndpoint = "/api/master-data/tax-codes" }
        };
    }
}
