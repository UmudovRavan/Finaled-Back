using System;
using AltensorAccounting.Domain.Common;

namespace AltensorAccounting.Domain.Entities.Accounting;

public class InitialBalanceAuditLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ActNumber { get; set; } = default!; // Təsdiq edilmiş akt/protokol nömrəsi
    public DateTime ActDate { get; set; }
    public string? AttachmentUrl { get; set; } // İmzalanmış sənədin skan/fayl linki
    public string AuthorizedBy { get; set; } = default!; // Səlahiyyətli şəxs / admin
    public string? Notes { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public Guid? JournalBatchId { get; set; }
    public int AccountCount { get; set; }
}
