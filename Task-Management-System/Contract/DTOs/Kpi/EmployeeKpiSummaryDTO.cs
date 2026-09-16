using System;
using System.Collections.Generic;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Əməkdaş Kabineti üçün xülasə məlumatı:
    /// - Bu günün balı və statusu
    /// - Həftəlik və aylıq yekun bal
    /// - Mənfiyə düşmə səbəbləri və cərimə tarixçəsi
    /// </summary>
    public class EmployeeKpiSummaryDTO
    {
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeEmail { get; set; }
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }

        // Bu gün üçün nəticə (əgər qiymətləndirilibsə)
        public DailyKpiDTO? TodayScore { get; set; }

        // Cari həftəlik yekun bal
        public int WeeklyTotalScore { get; set; }
        public double WeeklyAverageScore { get; set; }

        // Cari aylıq yekun bal (Aylıq Yekun Bal - Cumulative Score)
        public int MonthlyTotalScore { get; set; }
        public double MonthlyAverageScore { get; set; }

        // İntizam qaydalarının pozulması sayı
        public int DisciplineViolationsCount { get; set; }

        // Mənfiyə düşdüyü günlər və cərimə səbəbləri
        public List<DailyKpiDTO> NegativeScoresHistory { get; set; } = new();

        // Son qiymətləndirmələr
        public List<DailyKpiDTO> RecentHistory { get; set; } = new();
    }
}
