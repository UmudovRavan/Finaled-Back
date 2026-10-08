using System;

namespace AltensorAccounting.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; set; }

    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_ERROR") : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string message, Exception innerException, string errorCode = "BUSINESS_RULE_ERROR") : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class MissingDefaultAccountException : BusinessRuleException
{
    public MissingDefaultAccountException(string message)
        : base(message, "MISSING_DEFAULT_ACCOUNT")
    {
    }
}

public class PostingUnbalancedException : BusinessRuleException
{
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }
    public decimal Difference { get; }

    public PostingUnbalancedException(decimal totalDebit, decimal totalCredit)
        : base($"Ledger posting is unbalanced! Total Debit: {totalDebit:F2}, Total Credit: {totalCredit:F2}, Difference: {totalDebit - totalCredit:F2}")
    {
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
        Difference = totalDebit - totalCredit;
    }
}

public class PeriodLockedException : BusinessRuleException
{
    public DateTime PostingDate { get; }

    public PeriodLockedException(DateTime postingDate, string status)
        : base($"Posting rejected: Date {postingDate:yyyy-MM-dd} falls into a {status} fiscal period.")
    {
        PostingDate = postingDate;
    }
}

public class DuplicatePostingException : BusinessRuleException
{
    public string DocumentNumber { get; }

    public DuplicatePostingException(string documentNumber)
        : base($"Document '{documentNumber}' has already been posted to General Ledger and cannot be posted again.")
    {
        DocumentNumber = documentNumber;
    }
}

public class InsufficientStockException : BusinessRuleException
{
    public string ItemCode { get; }
    public decimal AvailableQty { get; }
    public decimal RequestedQty { get; }

    public InsufficientStockException(string itemCode, decimal availableQty, decimal requestedQty)
        : base($"Insufficient stock for item '{itemCode}'. Available: {availableQty}, Requested: {requestedQty}.")
    {
        ItemCode = itemCode;
        AvailableQty = availableQty;
        RequestedQty = requestedQty;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class TenantSuspendedException : Exception
{
    public TenantSuspendedException(string message) : base(message) { }
}
