using System;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Birbaşa rəhbər menecer tərəfindən tabelikdəki əməkdaşa cari gün üçün daxil edilən KPI balı.
    /// Qaydalar:
    /// - JobDutiesScore: 0 və ya 1
    /// - DisciplineScore: 0 və ya -1 (əgər -1 verilibsə, DisciplinePenaltyReason məcburidir)
    /// - BonusScore: 0 və ya 1
    /// </summary>
    public class CreateDailyKpiDTO
    {
        public Guid EmployeeId { get; set; }

        // Vəzifə Öhdəlikləri: Tam icra edildikdə 1, edilmədikdə 0
        public int JobDutiesScore { get; set; }

        // Korporativ Etika və İntizam: Pozuntu olduqda -1, olmadıqda 0
        public int DisciplineScore { get; set; }

        // Əlavə Performans (Bonus Bal): Vəzifə öhdəliyindən əlavə iş görüldükdə 1, olmadıqda 0
        public int BonusScore { get; set; }

        // İntizam qaydaları pozulduqda (-1) mütləq səbəb qeyd olunmalıdır
        public string? DisciplinePenaltyReason { get; set; }

        // Bonus bal üçün qeyd / səbəb
        public string? BonusReason { get; set; }

        // Rəhbərin əməkdaş haqqında ümumi rəyi / şərhi
        public string? Comments { get; set; }
    }
}
