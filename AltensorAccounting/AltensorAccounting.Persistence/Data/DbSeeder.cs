using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Persistence.Data;

public static class DbSeeder
{
    public static async Task SeedTenantAccountingDefaultsAsync(AppDbContext context, Guid tenantId, ILogger logger)
    {
        // 1. Company Defaults
        var company = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (company == null)
        {
            company = new Company
            {
                TenantId = tenantId,
                Name = "Standart Müəssisə",
                TaxNumber = "1234567891",
                BaseCurrency = "AZN",
                Country = "Azerbaijan"
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();
        }

        // 2. Default Chart of Accounts for Azerbaijan Accounting Standards
        var existingAccounts = await context.Accounts.IgnoreQueryFilters().Where(a => a.TenantId == tenantId).ToListAsync();
        if (!existingAccounts.Any())
        {
            logger.LogInformation("Seeding standard Chart of Accounts for Tenant {TenantId}...", tenantId);

            var accounts = new List<Account>
            {
                // Assets (1000)
                new Account { TenantId = tenantId, Code = "1000", Name = "Dövriyyə Aktivləri", Category = AccountCategory.Asset, Type = AccountType.CurrentAsset, IsLeaf = false },
                new Account { TenantId = tenantId, Code = "1010", Name = "Kassa", Category = AccountCategory.Asset, Type = AccountType.Cash, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "1020", Name = "Bank Hesablaşma Hesabı", Category = AccountCategory.Asset, Type = AccountType.Bank, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "1100", Name = "Mallar və Materiallar (Stok)", Category = AccountCategory.Asset, Type = AccountType.Stock, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "1200", Name = "Alıcıların Debitor Borcları (AR)", Category = AccountCategory.Asset, Type = AccountType.Receivable, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "1250", Name = "Əvəzləşdirilən ƏDV (Input VAT)", Category = AccountCategory.Asset, Type = AccountType.Tax, IsLeaf = true, IsControlAccount = true },

                // Liabilities (2000)
                new Account { TenantId = tenantId, Code = "2000", Name = "Qısamüddətli Öhdəliklər", Category = AccountCategory.Liability, Type = AccountType.CurrentLiability, IsLeaf = false },
                new Account { TenantId = tenantId, Code = "2100", Name = "Təchizatçılara Kreditor Borclar (AP)", Category = AccountCategory.Liability, Type = AccountType.Payable, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "2200", Name = "Fakturalaşdırılmamış Mallar (GRNI)", Category = AccountCategory.Liability, Type = AccountType.GRNI, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "2250", Name = "Büdcəyə Hesablanmış ƏDV (Output VAT)", Category = AccountCategory.Liability, Type = AccountType.Tax, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "2300", Name = "Alınmış Müştəri Avansları", Category = AccountCategory.Liability, Type = AccountType.Payable, IsLeaf = true, IsControlAccount = true },

                // Equity (3000)
                new Account { TenantId = tenantId, Code = "3000", Name = "Kapital", Category = AccountCategory.Equity, Type = AccountType.Standard, IsLeaf = false },
                new Account { TenantId = tenantId, Code = "3010", Name = "Nizamnamə Kapitalı", Category = AccountCategory.Equity, Type = AccountType.Standard, IsLeaf = true },
                new Account { TenantId = tenantId, Code = "3100", Name = "Bölüşdürülməmiş Mənfəət / Zərər", Category = AccountCategory.Equity, Type = AccountType.RetainedEarnings, IsLeaf = true, IsControlAccount = true },

                // Revenue (6000)
                new Account { TenantId = tenantId, Code = "6000", Name = "Əsas Əməliyyat Gəlirləri", Category = AccountCategory.Income, Type = AccountType.Revenue, IsLeaf = false },
                new Account { TenantId = tenantId, Code = "6010", Name = "Malların və Xidmətlərin Satış Gəliri", Category = AccountCategory.Income, Type = AccountType.Revenue, IsLeaf = true },

                // Expense (7000)
                new Account { TenantId = tenantId, Code = "7000", Name = "Əməliyyat Xərcləri", Category = AccountCategory.Expense, Type = AccountType.Expense, IsLeaf = false },
                new Account { TenantId = tenantId, Code = "7010", Name = "Satılmış Malların Maya Dəyəri (COGS)", Category = AccountCategory.Expense, Type = AccountType.COGS, IsLeaf = true, IsControlAccount = true },
                new Account { TenantId = tenantId, Code = "7100", Name = "Ümumi və İnzibati Xərclər", Category = AccountCategory.Expense, Type = AccountType.Expense, IsLeaf = true },
                new Account { TenantId = tenantId, Code = "7200", Name = "Satış Xərcləri", Category = AccountCategory.Expense, Type = AccountType.Expense, IsLeaf = true },
                new Account { TenantId = tenantId, Code = "7300", Name = "Bank Xidmət Xərcləri", Category = AccountCategory.Expense, Type = AccountType.Expense, IsLeaf = true },
                new Account { TenantId = tenantId, Code = "7400", Name = "Məzənnə Fərqi Xərci / Zərəri", Category = AccountCategory.Expense, Type = AccountType.Expense, IsLeaf = true }
            };

            await context.Accounts.AddRangeAsync(accounts);
            await context.SaveChangesAsync();

            // Link Company Default Control Accounts
            company.DefaultReceivableAccountId = accounts.First(a => a.Code == "1200").Id;
            company.DefaultPayableAccountId = accounts.First(a => a.Code == "2100").Id;
            company.DefaultStockAccountId = accounts.First(a => a.Code == "1100").Id;
            company.DefaultGRNIAccountId = accounts.First(a => a.Code == "2200").Id;
            company.DefaultCOGSAccountId = accounts.First(a => a.Code == "7010").Id;
            company.DefaultRevenueAccountId = accounts.First(a => a.Code == "6010").Id;
            company.DefaultRetainedEarningsAccountId = accounts.First(a => a.Code == "3100").Id;
            company.DefaultInputVatAccountId = accounts.First(a => a.Code == "1250").Id;
            company.DefaultOutputVatAccountId = accounts.First(a => a.Code == "2250").Id;
            company.DefaultFXGainLossAccountId = accounts.First(a => a.Code == "7400").Id;

            await context.SaveChangesAsync();
            logger.LogInformation("Standard Chart of Accounts successfully seeded for Tenant {TenantId}.", tenantId);
        }
        else if (company.DefaultRevenueAccountId == null || company.DefaultRevenueAccountId == Guid.Empty)
        {
            var revAcc = existingAccounts.FirstOrDefault(a => a.Code == "6010");
            if (revAcc != null)
            {
                company.DefaultRevenueAccountId = revAcc.Id;
                await context.SaveChangesAsync();
                logger.LogInformation("Company DefaultRevenueAccountId patched for Tenant {TenantId}.", tenantId);
            }
        }
    }

    public static async Task EnsureAllCompaniesHaveDefaultAccountsAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            var companies = await context.Companies.IgnoreQueryFilters().ToListAsync();
            if (!companies.Any()) return;

            foreach (var company in companies)
            {
                var tenantAccounts = await context.Accounts
                    .IgnoreQueryFilters()
                    .Where(a => a.TenantId == company.TenantId)
                    .ToListAsync();

                if (!tenantAccounts.Any()) continue;

                bool modified = false;

                Guid? GetAccId(string code) => tenantAccounts.FirstOrDefault(a => a.Code == code)?.Id;

                if (!company.DefaultRevenueAccountId.HasValue || company.DefaultRevenueAccountId == Guid.Empty)
                {
                    var id = GetAccId("6010");
                    if (id.HasValue) { company.DefaultRevenueAccountId = id; modified = true; }
                }

                if (!company.DefaultStockAccountId.HasValue || company.DefaultStockAccountId == Guid.Empty)
                {
                    var id = GetAccId("1100");
                    if (id.HasValue) { company.DefaultStockAccountId = id; modified = true; }
                }

                if (!company.DefaultGRNIAccountId.HasValue || company.DefaultGRNIAccountId == Guid.Empty)
                {
                    var id = GetAccId("2200");
                    if (id.HasValue) { company.DefaultGRNIAccountId = id; modified = true; }
                }

                if (!company.DefaultReceivableAccountId.HasValue || company.DefaultReceivableAccountId == Guid.Empty)
                {
                    var id = GetAccId("1200");
                    if (id.HasValue) { company.DefaultReceivableAccountId = id; modified = true; }
                }

                if (!company.DefaultPayableAccountId.HasValue || company.DefaultPayableAccountId == Guid.Empty)
                {
                    var id = GetAccId("2100");
                    if (id.HasValue) { company.DefaultPayableAccountId = id; modified = true; }
                }

                if (!company.DefaultCOGSAccountId.HasValue || company.DefaultCOGSAccountId == Guid.Empty)
                {
                    var id = GetAccId("7010");
                    if (id.HasValue) { company.DefaultCOGSAccountId = id; modified = true; }
                }

                if (!company.DefaultInputVatAccountId.HasValue || company.DefaultInputVatAccountId == Guid.Empty)
                {
                    var id = GetAccId("1250");
                    if (id.HasValue) { company.DefaultInputVatAccountId = id; modified = true; }
                }

                if (!company.DefaultOutputVatAccountId.HasValue || company.DefaultOutputVatAccountId == Guid.Empty)
                {
                    var id = GetAccId("2250");
                    if (id.HasValue) { company.DefaultOutputVatAccountId = id; modified = true; }
                }

                if (!company.DefaultRetainedEarningsAccountId.HasValue || company.DefaultRetainedEarningsAccountId == Guid.Empty)
                {
                    var id = GetAccId("3100");
                    if (id.HasValue) { company.DefaultRetainedEarningsAccountId = id; modified = true; }
                }

                if (!company.DefaultFXGainLossAccountId.HasValue || company.DefaultFXGainLossAccountId == Guid.Empty)
                {
                    var id = GetAccId("7400");
                    if (id.HasValue) { company.DefaultFXGainLossAccountId = id; modified = true; }
                }

                if (modified)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation("Company defaults successfully patched for Tenant {TenantId}.", company.TenantId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to automatically patch company defaults: {Message}", ex.Message);
        }
    }
}
