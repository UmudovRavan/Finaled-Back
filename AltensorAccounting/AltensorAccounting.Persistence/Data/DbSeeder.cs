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
    public record StandardAccountDefinition(
        string Code,
        string Name,
        AccountCategory Category,
        AccountType Type,
        bool IsLeaf,
        bool IsControl = false);

    public static readonly StandardAccountDefinition[] StandardChart = new[]
    {
        // Assets (1000)
        new StandardAccountDefinition("1000", "Dövriyyə Aktivləri", AccountCategory.Asset, AccountType.CurrentAsset, false),
        new StandardAccountDefinition("1010", "Kassa", AccountCategory.Asset, AccountType.Cash, true, true),
        new StandardAccountDefinition("1020", "Bank Hesablaşma Hesabı", AccountCategory.Asset, AccountType.Bank, true, true),
        new StandardAccountDefinition("1100", "Mallar və Materiallar (Stok)", AccountCategory.Asset, AccountType.Stock, true, true),
        new StandardAccountDefinition("1200", "Alıcıların Debitor Borcları (AR)", AccountCategory.Asset, AccountType.Receivable, true, true),
        new StandardAccountDefinition("1250", "Əvəzləşdirilən ƏDV (Input VAT)", AccountCategory.Asset, AccountType.Tax, true, true),

        // Liabilities (2000)
        new StandardAccountDefinition("2000", "Qısamüddətli Öhdəliklər", AccountCategory.Liability, AccountType.CurrentLiability, false),
        new StandardAccountDefinition("2100", "Təchizatçılara Kreditor Borclar (AP)", AccountCategory.Liability, AccountType.Payable, true, true),
        new StandardAccountDefinition("2200", "Fakturalaşdırılmamış Mallar (GRNI)", AccountCategory.Liability, AccountType.GRNI, true, true),
        new StandardAccountDefinition("2250", "Büdcəyə Hesablanmış ƏDV (Output VAT)", AccountCategory.Liability, AccountType.Tax, true, true),
        new StandardAccountDefinition("2300", "Alınmış Müştəri Avansları", AccountCategory.Liability, AccountType.Payable, true, true),

        // Equity (3000)
        new StandardAccountDefinition("3000", "Kapital", AccountCategory.Equity, AccountType.Standard, false),
        new StandardAccountDefinition("3010", "Nizamnamə Kapitalı", AccountCategory.Equity, AccountType.Standard, true),
        new StandardAccountDefinition("3100", "Bölüşdürülməmiş Mənfəət / Zərər", AccountCategory.Equity, AccountType.RetainedEarnings, true, true),

        // Revenue (6000)
        new StandardAccountDefinition("6000", "Əsas Əməliyyat Gəlirləri", AccountCategory.Income, AccountType.Revenue, false),
        new StandardAccountDefinition("6010", "Malların və Xidmətlərin Satış Gəliri", AccountCategory.Income, AccountType.Revenue, true),

        // Expense (7000)
        new StandardAccountDefinition("7000", "Əməliyyat Xərcləri", AccountCategory.Expense, AccountType.Expense, false),
        new StandardAccountDefinition("7010", "Satılmış Malların Maya Dəyəri (COGS)", AccountCategory.Expense, AccountType.COGS, true, true),
        new StandardAccountDefinition("7100", "Ümumi və İnzibati Xərclər", AccountCategory.Expense, AccountType.Expense, true),
        new StandardAccountDefinition("7200", "Satış Xərcləri", AccountCategory.Expense, AccountType.Expense, true),
        new StandardAccountDefinition("7300", "Bank Xidmət Xərcləri", AccountCategory.Expense, AccountType.Expense, true),
        new StandardAccountDefinition("7400", "Məzənnə Fərqi Xərci / Zərəri", AccountCategory.Expense, AccountType.Expense, true)
    };

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

        // 2. Default Chart of Accounts for Azerbaijan Accounting Standards (Idempotent: adds missing accounts)
        var existingAccounts = await context.Accounts
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .ToListAsync();

        var byCode = existingAccounts.GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First());

        bool accountsAdded = false;
        foreach (var def in StandardChart)
        {
            if (!byCode.ContainsKey(def.Code))
            {
                var acc = new Account
                {
                    TenantId = tenantId,
                    Code = def.Code,
                    Name = def.Name,
                    Category = def.Category,
                    Type = def.Type,
                    IsLeaf = def.IsLeaf,
                    IsControlAccount = def.IsControl,
                    IsActive = true
                };
                await context.Accounts.AddAsync(acc);
                byCode[def.Code] = acc;
                accountsAdded = true;
            }
        }

        if (accountsAdded)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Added missing standard accounts for Tenant {TenantId}.", tenantId);
        }

        // 3. Link Company Default Control Accounts (Idempotent: fills any empty defaults)
        bool companyUpdated = false;

        void EnsureDefault(ref Guid? current, string code)
        {
            if ((!current.HasValue || current.Value == Guid.Empty) && byCode.TryGetValue(code, out var acc))
            {
                current = acc.Id;
                companyUpdated = true;
            }
        }

        var recv = company.DefaultReceivableAccountId; EnsureDefault(ref recv, "1200"); company.DefaultReceivableAccountId = recv;
        var pay = company.DefaultPayableAccountId; EnsureDefault(ref pay, "2100"); company.DefaultPayableAccountId = pay;
        var stock = company.DefaultStockAccountId; EnsureDefault(ref stock, "1100"); company.DefaultStockAccountId = stock;
        var grni = company.DefaultGRNIAccountId; EnsureDefault(ref grni, "2200"); company.DefaultGRNIAccountId = grni;
        var cogs = company.DefaultCOGSAccountId; EnsureDefault(ref cogs, "7010"); company.DefaultCOGSAccountId = cogs;
        var rev = company.DefaultRevenueAccountId; EnsureDefault(ref rev, "6010"); company.DefaultRevenueAccountId = rev;
        var re = company.DefaultRetainedEarningsAccountId; EnsureDefault(ref re, "3100"); company.DefaultRetainedEarningsAccountId = re;
        var inVat = company.DefaultInputVatAccountId; EnsureDefault(ref inVat, "1250"); company.DefaultInputVatAccountId = inVat;
        var outVat = company.DefaultOutputVatAccountId; EnsureDefault(ref outVat, "2250"); company.DefaultOutputVatAccountId = outVat;
        var fx = company.DefaultFXGainLossAccountId; EnsureDefault(ref fx, "7400"); company.DefaultFXGainLossAccountId = fx;

        if (companyUpdated)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Company Default Accounts linked for Tenant {TenantId}.", tenantId);
        }

        // 4. Ensure Fiscal Year and Accounting Periods exist for tenant
        var currentYear = DateTime.UtcNow.Year;
        var hasFiscalYear = await context.FiscalYears.IgnoreQueryFilters().AnyAsync(y => y.TenantId == tenantId);
        if (!hasFiscalYear)
        {
            var fiscalYear = new FiscalYear
            {
                TenantId = tenantId,
                Name = $"FY-{currentYear}",
                StartDate = DateTime.SpecifyKind(new DateTime(currentYear, 1, 1), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(new DateTime(currentYear, 12, 31), DateTimeKind.Utc),
                IsClosed = false
            };

            for (int month = 1; month <= 12; month++)
            {
                var startDate = DateTime.SpecifyKind(new DateTime(currentYear, month, 1), DateTimeKind.Utc);
                var endDate = DateTime.SpecifyKind(startDate.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

                fiscalYear.Periods.Add(new AccountingPeriod
                {
                    TenantId = tenantId,
                    Name = $"{currentYear}-{month:D2}",
                    PeriodNumber = month,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = FiscalPeriodStatus.Open
                });
            }

            await context.FiscalYears.AddAsync(fiscalYear);
            await context.SaveChangesAsync();
            logger.LogInformation("Fiscal Year FY-{Year} and 12 monthly accounting periods seeded for Tenant {TenantId}.", currentYear, tenantId);
        }
    }

    public static async Task EnsureAllCompaniesHaveDefaultAccountsAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            var companyTenantIds = await context.Companies.IgnoreQueryFilters().Select(c => c.TenantId).ToListAsync();
            var userTenantIds = await context.Users.IgnoreQueryFilters().Select(u => u.TenantId).ToListAsync();
            var accountTenantIds = await context.Accounts.IgnoreQueryFilters().Select(a => a.TenantId).ToListAsync();

            var allTenantIds = companyTenantIds
                .Concat(userTenantIds)
                .Concat(accountTenantIds)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (!allTenantIds.Any()) return;

            foreach (var tenantId in allTenantIds)
            {
                await SeedTenantAccountingDefaultsAsync(context, tenantId, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to automatically patch tenant defaults on startup: {Message}", ex.Message);
        }
    }
}
