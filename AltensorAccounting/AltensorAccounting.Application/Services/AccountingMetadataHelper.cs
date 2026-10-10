using System;
using System.Collections.Generic;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Application.Services;

public static class AccountingMetadataHelper
{
    public static string GetCategoryName(AccountCategory category) => category switch
    {
        AccountCategory.Asset => "Aktiv",
        AccountCategory.Liability => "Öhdəlik",
        AccountCategory.Equity => "Kapital",
        AccountCategory.Income => "Gəlir",
        AccountCategory.Expense => "Xərc",
        _ => category.ToString()
    };

    public static string GetSubcategoryName(AccountSubcategory? subcategory) => subcategory switch
    {
        AccountSubcategory.NonCurrentAssets => "Uzunmüddətli aktivlər",
        AccountSubcategory.CurrentAssets => "Dövriyyə aktivləri",
        AccountSubcategory.NonCurrentLiabilities => "Uzunmüddətli öhdəliklər",
        AccountSubcategory.CurrentLiabilities => "Qısamüddətli öhdəliklər",
        AccountSubcategory.ShareCapital => "Nizamnamə kapitalı",
        AccountSubcategory.RetainedEarnings => "Bölüşdürülməmiş mənfəət / zərər",
        AccountSubcategory.OtherEquityAndReserves => "Digər kapital və ehtiyatlar",
        AccountSubcategory.OperatingRevenue => "Əsas fəaliyyət gəlirləri",
        AccountSubcategory.OtherOperatingIncome => "Digər əməliyyat gəlirləri",
        AccountSubcategory.FinancialIncome => "Maliyyə gəlirləri",
        AccountSubcategory.OtherIncome => "Digər gəlirlər",
        AccountSubcategory.CostOfGoodsSold => "Satışın Maya Dəyəri (COGS)",
        AccountSubcategory.SellingAndMarketingExpenses => "Satış və marketinq xərcləri",
        AccountSubcategory.AdministrativeExpenses => "İnzibati xərclər",
        AccountSubcategory.FinancialExpenses => "Maliyyə xərcləri",
        AccountSubcategory.TaxExpenses => "Vergi xərcləri",
        AccountSubcategory.OtherExpenses => "Digər xərclər",
        _ => string.Empty
    };

    public static string GetAccountTypeName(AccountType type) => type switch
    {
        AccountType.Standard => "Standart hesab",
        AccountType.Receivable => "Alıcıların debitor borcları (AR)",
        AccountType.Payable => "Təchizatçılara kreditor borclar (AP)",
        AccountType.Bank => "Bank hesabı",
        AccountType.Cash => "Kassa",
        AccountType.Stock => "Mallar və materiallar (Stok)",
        AccountType.GRNI => "Fakturalaşdırılmamış mallar (GRNI)",
        AccountType.COGS => "Satışın Maya Dəyəri (COGS)",
        AccountType.Tax => "Vergi hesabı",
        AccountType.RetainedEarnings => "Bölüşdürülməmiş mənfəət / zərər",
        AccountType.FixedAsset => "Əsas vəsaitlər",
        AccountType.AccumulatedDepreciation => "Yığılmış amortizasiya",
        AccountType.AccountablePersons => "Təhtəlhesab məbləğlər",
        AccountType.AdvancesGiven => "Verilmiş avanslar",
        AccountType.AdvancesReceived => "Alınmış avanslar",
        AccountType.BankLoans => "Bank kreditləri",
        _ => type.ToString()
    };

    public static bool ValidateSubcategoryForCategory(AccountCategory category, AccountSubcategory subcategory)
    {
        return category switch
        {
            AccountCategory.Asset => subcategory is AccountSubcategory.NonCurrentAssets or AccountSubcategory.CurrentAssets,
            AccountCategory.Liability => subcategory is AccountSubcategory.NonCurrentLiabilities or AccountSubcategory.CurrentLiabilities,
            AccountCategory.Equity => subcategory is AccountSubcategory.ShareCapital or AccountSubcategory.RetainedEarnings or AccountSubcategory.OtherEquityAndReserves,
            AccountCategory.Income => subcategory is AccountSubcategory.OperatingRevenue or AccountSubcategory.OtherOperatingIncome or AccountSubcategory.FinancialIncome or AccountSubcategory.OtherIncome,
            AccountCategory.Expense => subcategory is AccountSubcategory.CostOfGoodsSold or AccountSubcategory.SellingAndMarketingExpenses or AccountSubcategory.AdministrativeExpenses or AccountSubcategory.FinancialExpenses or AccountSubcategory.TaxExpenses or AccountSubcategory.OtherExpenses,
            _ => false
        };
    }

    public static List<AccountTypeOptionDto> GetActiveAccountTypeOptions()
    {
        return new List<AccountTypeOptionDto>
        {
            new() { Id = (int)AccountType.Standard, Code = nameof(AccountType.Standard), Name = "Standart hesab", Description = "Ümumi təyinatlı balans və ya xərc/gəlir hesabı" },
            new() { Id = (int)AccountType.Receivable, Code = nameof(AccountType.Receivable), Name = "Alıcıların debitor borcları (AR)", Description = "Müştərilərlə hesablaşmaların aparıldığı nəzarət hesabı" },
            new() { Id = (int)AccountType.Payable, Code = nameof(AccountType.Payable), Name = "Təchizatçılara kreditor borclar (AP)", Description = "Təchizatçılarla hesablaşmaların aparıldığı nəzarət hesabı" },
            new() { Id = (int)AccountType.Bank, Code = nameof(AccountType.Bank), Name = "Bank hesabı", Description = "Bank hesablaşma və valyuta hesabları" },
            new() { Id = (int)AccountType.Cash, Code = nameof(AccountType.Cash), Name = "Kassa", Description = "Nağd pul və kassa mədaxil/məxaric əməliyyatları" },
            new() { Id = (int)AccountType.Stock, Code = nameof(AccountType.Stock), Name = "Mallar və materiallar (Stok)", Description = "Anbar malları və material ehtiyatları" },
            new() { Id = (int)AccountType.GRNI, Code = nameof(AccountType.GRNI), Name = "Fakturalaşdırılmamış mallar (GRNI)", Description = "Qəbul edilmiş, lakin qaiməsi gəlməmiş mallar üzrə keçid öhdəliyi" },
            new() { Id = (int)AccountType.COGS, Code = nameof(AccountType.COGS), Name = "Satışın Maya Dəyəri (COGS)", Description = "Satılmış malların və göstərilmiş xidmətlərin birbaşa maya dəyəri" },
            new() { Id = (int)AccountType.Tax, Code = nameof(AccountType.Tax), Name = "Vergi hesabı", Description = "ƏDV və digər büdcə vergi öhdəlikləri və aktivləri" },
            new() { Id = (int)AccountType.RetainedEarnings, Code = nameof(AccountType.RetainedEarnings), Name = "Bölüşdürülməmiş mənfəət / zərər", Description = "İllik maliyyə nəticəsi və yığılmış kapital fərqi" },
            new() { Id = (int)AccountType.FixedAsset, Code = nameof(AccountType.FixedAsset), Name = "Əsas vəsaitlər", Description = "Torpaq, tikili, avadanlıq və digər uzunmüddətli əsas vəsaitlər" },
            new() { Id = (int)AccountType.AccumulatedDepreciation, Code = nameof(AccountType.AccumulatedDepreciation), Name = "Yığılmış amortizasiya", Description = "Əsas vəsaitlərin köhnəlməsi və yığılmış amortizasiyası (kontr-aktiv)" },
            new() { Id = (int)AccountType.AccountablePersons, Code = nameof(AccountType.AccountablePersons), Name = "Təhtəlhesab məbləğlər", Description = "Təhtəlhesab şəxslərə verilmiş və hesabatı gözlənilən avanslar" },
            new() { Id = (int)AccountType.AdvancesGiven, Code = nameof(AccountType.AdvancesGiven), Name = "Verilmiş avanslar", Description = "Təchizatçılara və podratçılara qabaqcadan ödənilmiş avanslar" },
            new() { Id = (int)AccountType.AdvancesReceived, Code = nameof(AccountType.AdvancesReceived), Name = "Alınmış avanslar", Description = "Müştərilərdən sifariş üçün qabaqcadan alınmış ödənişlər" },
            new() { Id = (int)AccountType.BankLoans, Code = nameof(AccountType.BankLoans), Name = "Bank kreditləri", Description = "Banklardan və maliyyə təşkilatlarından alınmış qısa və uzunmüddətli kreditlər" }
        };
    }

    public static List<CategorySubcategoryMappingDto> GetCategorySubcategoryMappings()
    {
        return new List<CategorySubcategoryMappingDto>
        {
            new()
            {
                CategoryId = (int)AccountCategory.Asset,
                CategoryCode = nameof(AccountCategory.Asset),
                CategoryName = "Aktiv",
                Subcategories = new()
                {
                    new() { Id = (int)AccountSubcategory.NonCurrentAssets, Code = nameof(AccountSubcategory.NonCurrentAssets), Name = "Uzunmüddətli aktivlər" },
                    new() { Id = (int)AccountSubcategory.CurrentAssets, Code = nameof(AccountSubcategory.CurrentAssets), Name = "Dövriyyə aktivləri" }
                }
            },
            new()
            {
                CategoryId = (int)AccountCategory.Liability,
                CategoryCode = nameof(AccountCategory.Liability),
                CategoryName = "Öhdəlik",
                Subcategories = new()
                {
                    new() { Id = (int)AccountSubcategory.NonCurrentLiabilities, Code = nameof(AccountSubcategory.NonCurrentLiabilities), Name = "Uzunmüddətli öhdəliklər" },
                    new() { Id = (int)AccountSubcategory.CurrentLiabilities, Code = nameof(AccountSubcategory.CurrentLiabilities), Name = "Qısamüddətli öhdəliklər" }
                }
            },
            new()
            {
                CategoryId = (int)AccountCategory.Equity,
                CategoryCode = nameof(AccountCategory.Equity),
                CategoryName = "Kapital",
                Subcategories = new()
                {
                    new() { Id = (int)AccountSubcategory.ShareCapital, Code = nameof(AccountSubcategory.ShareCapital), Name = "Nizamnamə kapitalı" },
                    new() { Id = (int)AccountSubcategory.RetainedEarnings, Code = nameof(AccountSubcategory.RetainedEarnings), Name = "Bölüşdürülməmiş mənfəət / zərər" },
                    new() { Id = (int)AccountSubcategory.OtherEquityAndReserves, Code = nameof(AccountSubcategory.OtherEquityAndReserves), Name = "Digər kapital və ehtiyatlar" }
                }
            },
            new()
            {
                CategoryId = (int)AccountCategory.Income,
                CategoryCode = nameof(AccountCategory.Income),
                CategoryName = "Gəlir",
                Subcategories = new()
                {
                    new() { Id = (int)AccountSubcategory.OperatingRevenue, Code = nameof(AccountSubcategory.OperatingRevenue), Name = "Əsas fəaliyyət gəlirləri" },
                    new() { Id = (int)AccountSubcategory.OtherOperatingIncome, Code = nameof(AccountSubcategory.OtherOperatingIncome), Name = "Digər əməliyyat gəlirləri" },
                    new() { Id = (int)AccountSubcategory.FinancialIncome, Code = nameof(AccountSubcategory.FinancialIncome), Name = "Maliyyə gəlirləri" },
                    new() { Id = (int)AccountSubcategory.OtherIncome, Code = nameof(AccountSubcategory.OtherIncome), Name = "Digər gəlirlər" }
                }
            },
            new()
            {
                CategoryId = (int)AccountCategory.Expense,
                CategoryCode = nameof(AccountCategory.Expense),
                CategoryName = "Xərc",
                Subcategories = new()
                {
                    new() { Id = (int)AccountSubcategory.CostOfGoodsSold, Code = nameof(AccountSubcategory.CostOfGoodsSold), Name = "Satışın Maya Dəyəri (COGS)" },
                    new() { Id = (int)AccountSubcategory.SellingAndMarketingExpenses, Code = nameof(AccountSubcategory.SellingAndMarketingExpenses), Name = "Satış və marketinq xərcləri" },
                    new() { Id = (int)AccountSubcategory.AdministrativeExpenses, Code = nameof(AccountSubcategory.AdministrativeExpenses), Name = "İnzibati xərclər" },
                    new() { Id = (int)AccountSubcategory.FinancialExpenses, Code = nameof(AccountSubcategory.FinancialExpenses), Name = "Maliyyə xərcləri" },
                    new() { Id = (int)AccountSubcategory.TaxExpenses, Code = nameof(AccountSubcategory.TaxExpenses), Name = "Vergi xərcləri" },
                    new() { Id = (int)AccountSubcategory.OtherExpenses, Code = nameof(AccountSubcategory.OtherExpenses), Name = "Digər xərclər" }
                }
            }
        };
    }
}
