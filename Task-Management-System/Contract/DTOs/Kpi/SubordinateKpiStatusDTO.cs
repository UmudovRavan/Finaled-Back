using System;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Rəhbər / Menecer Kabineti üçün tabelikdə olan əməkdaşların bugünkü qiymətləndirmə statusu.
    /// </summary>
    public class SubordinateKpiStatusDTO
    {
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeEmail { get; set; }
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }

        // Bu gün qiymətləndirilibmi
        public bool IsEvaluatedToday { get; set; }

        // Cari günün nəticəsi (əgər artıq qiymətləndirilibsə)
        public DailyKpiDTO? TodayKpi { get; set; }
    }
}
