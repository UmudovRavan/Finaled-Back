using AltensorAccounting.Domain.Common;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class Company : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string TaxNumber { get; set; } = default!; // VÖEN
    public string BaseCurrency { get; set; } = "AZN";
    public string Country { get; set; } = "Azerbaijan";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    // Azərbaycan spesifik və hüquqi/vergi parametrləri
    public string LegalForm { get; set; } = "MMC"; // MMC, ASC, QSC, FÖ və s.
    public string TaxRegime { get; set; } = "ƏDV ödəyicisi"; // ƏDV ödəyicisi, Sadələşdirilmiş vergi, Mənfəət vergisi
    public bool IsVatPayer { get; set; } = true;
    public decimal VatRate { get; set; } = 18.0m;
    public int FiscalYearStartMonth { get; set; } = 1;
    public int FiscalYearEndMonth { get; set; } = 12;
    public string? LogoUrl { get; set; }

    // Rəhbərlik
    public string? DirectorName { get; set; }
    public string? ChiefAccountantName { get; set; }

    // Bank Rekvizitləri
    public string? BankName { get; set; }
    public string? BankCode { get; set; } // Bank Kodu
    public string? BankAccountNumber { get; set; } // Hesablaşma hesabı
    public string? Iban { get; set; }
    public string? SwiftBic { get; set; }
    public string? CorrespondentAccount { get; set; } // Müxbir hesab

    // İnteqrasiya və Rəsmi Kodlar
    public string? StatisticalCode { get; set; } // Statistika kodu
    public string? AsanLoginId { get; set; }

    // Default Control Accounts
    public Guid? DefaultReceivableAccountId { get; set; }
    public Guid? DefaultPayableAccountId { get; set; }
    public Guid? DefaultStockAccountId { get; set; }
    public Guid? DefaultGRNIAccountId { get; set; }
    public Guid? DefaultCOGSAccountId { get; set; }
    public Guid? DefaultRetainedEarningsAccountId { get; set; }
    public Guid? DefaultInputVatAccountId { get; set; }
    public Guid? DefaultOutputVatAccountId { get; set; }
    public Guid? DefaultRevenueAccountId { get; set; }
    public Guid? DefaultFXGainLossAccountId { get; set; }
}
