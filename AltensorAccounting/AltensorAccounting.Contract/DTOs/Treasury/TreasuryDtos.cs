using System;
using System.Collections.Generic;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Contract.DTOs.Treasury;

public class CreateBankAccountDto
{
    public string BankName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public string? SwiftCode { get; set; }
    public Guid GLAccountId { get; set; }
}

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public decimal CurrentBalance { get; set; }
}

public class CreateCashDeskDto
{
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public Guid GLAccountId { get; set; }
}

public class CashDeskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public decimal CurrentBalance { get; set; }
}

public class ImportBankStatementDto
{
    public Guid BankAccountId { get; set; }
    public string StatementNumber { get; set; } = default!;
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<BankStatementLineInputDto> Lines { get; set; } = new();
}

public class BankStatementLineInputDto
{
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? CounterpartyName { get; set; }
    public string? Description { get; set; }
}

public class BankStatementDto
{
    public Guid Id { get; set; }
    public string StatementNumber { get; set; } = default!;
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
}

public class CreatePaymentRunDto
{
    public DateTime RunDate { get; set; }
    public Guid BankAccountId { get; set; }
    public List<Guid> SelectedSupplierInvoiceIds { get; set; } = new();
}

public class PaymentRunDto
{
    public Guid Id { get; set; }
    public string RunNumber { get; set; } = default!;
    public DateTime RunDate { get; set; }
    public decimal TotalProposalAmount { get; set; }
    public DocumentStatus Status { get; set; }
    public int InvoiceCount { get; set; }
}
