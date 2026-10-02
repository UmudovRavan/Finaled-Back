using AltensorAccounting.Domain.Common;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Entities.Procurement;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Domain.Entities.Treasury;

public class BankAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string BankName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!; // IBAN
    public string Currency { get; set; } = "AZN";
    public string? SwiftCode { get; set; }

    public Guid? GLAccountId { get; set; } // GL Asset account mapping
    public Account? GLAccount { get; set; }

    public decimal CurrentBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class CashDesk : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!; // e.g. "Baş Kassa"
    public string Currency { get; set; } = "AZN";

    public Guid? GLAccountId { get; set; } // GL Cash account mapping
    public Account? GLAccount { get; set; }

    public decimal CurrentBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class BankStatement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = default!;

    public string StatementNumber { get; set; } = default!;
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }

    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}

public class BankStatementLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BankStatementId { get; set; }
    public BankStatement BankStatement { get; set; } = default!;

    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; } // Positive = Deposit, Negative = Withdrawal
    public string? Reference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Description { get; set; }

    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Unmatched;
    public Guid? MatchedPaymentId { get; set; }
}

public class PaymentRun : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string RunNumber { get; set; } = default!;
    public DateTime RunDate { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = default!;

    public decimal TotalProposalAmount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft; // Draft -> Approved -> Posted

    public ICollection<PaymentRunItem> Items { get; set; } = new List<PaymentRunItem>();
}

public class PaymentRunItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentRunId { get; set; }
    public PaymentRun PaymentRun { get; set; } = default!;

    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;

    public Guid SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = default!;

    public decimal InvoiceOutstandingAmount { get; set; }
    public decimal ProposedPaymentAmount { get; set; }
    public bool IsSelected { get; set; } = true;
}
