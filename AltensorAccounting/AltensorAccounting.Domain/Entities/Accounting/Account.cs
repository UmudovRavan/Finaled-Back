using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class Account : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = default!; // e.g. "1010", "2010"
    public string Name { get; set; } = default!; // e.g. "Debitor Borclar", "Kassa"
    public AccountCategory Category { get; set; } // Asset, Liability, Equity, Income, Expense
    public AccountType Type { get; set; } = AccountType.Standard; // Purpose: Receivable, Payable, Bank, Cash, Stock, etc.

    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public ICollection<Account> SubAccounts { get; set; } = new List<Account>();

    public bool IsLeaf { get; set; } = true; // Yalnız leaf hesablara post oluna bilər
    public bool IsControlAccount { get; set; } = false; // AR/AP/Stock kimi sistem tərəfindən idarə olunan hesablar
    public bool IsActive { get; set; } = true;

    public string Currency { get; set; } = "AZN";
}
