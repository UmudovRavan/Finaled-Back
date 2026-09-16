using System;
using System.Collections.Generic;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Admin / HR Kabineti üçün şirkət və şöbələr üzrə ümumi analitika.
    /// </summary>
    public class CompanyKpiAnalyticsDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public int TotalEvaluationsCount { get; set; }
        public int EvaluatedEmployeesCount { get; set; }

        public int CompanyCumulativeScore { get; set; }
        public double CompanyAverageDailyScore { get; set; }

        public int TotalDutiesCompletedCount { get; set; }
        public int TotalDisciplineViolationsCount { get; set; }
        public int TotalBonusCount { get; set; }

        // Ən yüksək performans göstərən ilk 5 əməkdaş
        public List<KpiLeaderboardItemDTO> TopPerformers { get; set; } = new();

        // İntizam qaydasını pozanların son hadisələri
        public List<DailyKpiDTO> RecentDisciplineViolations { get; set; } = new();

        // Şöbələr üzrə müqayisə
        public List<DivisionKpiAnalyticsDTO> DivisionBreakdown { get; set; } = new();

        // Günlük trend (hər günün ortalama balı)
        public List<DailyTrendDTO> DailyTrends { get; set; } = new();
    }

    public class DivisionKpiAnalyticsDTO
    {
        public Guid DivisionId { get; set; }
        public string DivisionName { get; set; } = default!;
        public string? ManagerName { get; set; }
        public int EmployeeCount { get; set; }
        public int CumulativeScore { get; set; }
        public double AverageScore { get; set; }
        public int DisciplineViolationsCount { get; set; }
        public int BonusCount { get; set; }
    }

    public class DailyTrendDTO
    {
        public DateOnly Date { get; set; }
        public double AverageScore { get; set; }
        public int TotalEvaluations { get; set; }
        public int ViolationsCount { get; set; }
        public int BonusesCount { get; set; }
    }
}
