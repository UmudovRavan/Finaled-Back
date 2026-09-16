using System;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Aylıq Yekun Bal (Cumulative Score) və Reytinq cədvəli elementi.
    /// </summary>
    public class KpiLeaderboardItemDTO
    {
        public int Rank { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeEmail { get; set; }
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }

        // Ay ərzində toplanan cəmi bal
        public int MonthlyCumulativeScore { get; set; }
        public double AverageDailyScore { get; set; }

        // Meyarlar üzrə statistika
        public int DaysEvaluated { get; set; }
        public int DutiesCompletedDays { get; set; }
        public int DisciplineViolationsDays { get; set; }
        public int BonusDays { get; set; }
    }
}
