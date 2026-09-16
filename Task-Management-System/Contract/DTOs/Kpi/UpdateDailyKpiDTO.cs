using System;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Admin / HR tərəfindən istisna hallarda balın redaktə edilməsi modeli.
    /// </summary>
    public class UpdateDailyKpiDTO
    {
        public int JobDutiesScore { get; set; }
        public int DisciplineScore { get; set; }
        public int BonusScore { get; set; }
        public string? DisciplinePenaltyReason { get; set; }
        public string? BonusReason { get; set; }
        public string? Comments { get; set; }

        // Redaktə səbəbi (Audit üçün məcburidir)
        public string AdminEditReason { get; set; } = default!;
    }
}
