using System;
using System.Collections.Generic;
using AltensorAccounting.Domain.Enums;

namespace AltensorAccounting.Contract.DTOs.Accounting;

public class CreateAccountDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AccountCategory Category { get; set; }
    public AccountType Type { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsControlAccount { get; set; } = false;
    public string Currency { get; set; } = "AZN";
}

public class AccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AccountCategory Category { get; set; }
    public AccountType Type { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsLeaf { get; set; }
    public bool IsControlAccount { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "AZN";
}

public class CreateFiscalYearDto
{
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class FiscalYearDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public List<AccountingPeriodDto> Periods { get; set; } = new();
}

public class AccountingPeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int PeriodNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; }
}

public class CreateManualJournalDto
{
    public DateTime PostingDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Description { get; set; } = default!;
    public List<ManualJournalLineInputDto> Lines { get; set; } = new();
}

public class ManualJournalLineInputDto
{
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public Guid? PartyId { get; set; }
    public string? PartyType { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Description { get; set; }
}

public class ManualJournalDto
{
    public Guid Id { get; set; }
    public string JournalNumber { get; set; } = default!;
    public DateTime PostingDate { get; set; }
    public string Description { get; set; } = default!;
    public DocumentStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsReversal { get; set; }
    public Guid? ReversalOfJournalId { get; set; }
    public List<ManualJournalLineInputDto> Lines { get; set; } = new();
}

public class CreateCustomerDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class CreateCustomerInvoiceDto
{
    public Guid CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public string? Notes { get; set; }
    public List<CustomerInvoiceLineInputDto> Lines { get; set; } = new();
}

public class CustomerInvoiceLineInputDto
{
    public Guid? ItemId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public Guid? TaxCodeId { get; set; }
    public Guid? RevenueAccountId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class CustomerInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime PostingDate { get; set; }
    public DocumentStatus DocumentStatus { get; set; }
    public SettlementStatus SettlementStatus { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string? Notes { get; set; }

    // Qaimənin daxilindəki məhsul/xidmət sətirləri:
    public List<CustomerInvoiceLineDto> Lines { get; set; } = new();
}

public class CustomerInvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineSubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? RevenueAccountId { get; set; }
}

public class CreatePaymentDto
{
    public PaymentType Type { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime PostingDate { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyType { get; set; }
    public Guid BankOrCashAccountId { get; set; }
    public string Currency { get; set; } = "AZN";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal TotalAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public List<PaymentAllocationInputDto> Allocations { get; set; } = new();
}

public class PaymentAllocationInputDto
{
    public DocumentType TargetDocumentType { get; set; }
    public Guid TargetDocumentId { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = default!;
    public PaymentType Type { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime PostingDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public DocumentStatus Status { get; set; }
}
