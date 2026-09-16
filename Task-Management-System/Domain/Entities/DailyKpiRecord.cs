using System;

namespace Domain.Entities
{
    /// <summary>
    /// BMG International "Dəyər-Zərər" KPI və Performans Qiymətləndirmə Sistemi üzrə gündəlik qeyd.
    /// Gündəlik 3 meyar üzrə toplanan ballar:
    /// 1. Vəzifə Öhdəlikləri: +1 və ya 0
    /// 2. Korporativ Etika və İntizam: -1 və ya 0
    /// 3. Əlavə Performans (Bonus): +1 və ya 0
    /// Yekun Gündəlik Bal: [-1 .. +2]
    /// </summary>
    public class DailyKpiRecord : BaseEntity
    {
        public Guid EmployeeId { get; set; }
        public AppUser Employee { get; set; } = default!;

        public Guid EvaluatorId { get; set; }
        public AppUser Evaluator { get; set; } = default!;

        public Guid? DivisionId { get; set; }
        public Division? Division { get; set; }

        public DateOnly EvaluationDate { get; set; }

        // 1. Vəzifə Öhdəlikləri (0 və ya +1)
        public int JobDutiesScore { get; set; }

        // 2. Korporativ Etika və İntizam (0 və ya -1)
        public int DisciplineScore { get; set; }

        // 3. Əlavə Performans (Bonus) (0 və ya +1)
        public int BonusScore { get; set; }

        // Yekun Bal = JobDutiesScore + DisciplineScore + BonusScore (-1 .. +2)
        public int TotalScore { get; set; }

        // İntizam pozuntusu olduqda (-1) mütləq səbəb
        public string? DisciplinePenaltyReason { get; set; }

        // Əlavə performans bonusu üçün əsaslandırma
        public string? BonusReason { get; set; }

        // Menecerin ümumi gündəlik rəyi / şərhi
        public string? Comments { get; set; }

        // Admin və ya HR tərəfindən düzəliş olunubmu
        public bool IsAdminEdited { get; set; }

        // Admin / HR tərəfindən edilən düzəlişin səbəbi (audit üçün)
        public string? AdminEditReason { get; set; }
    }
}
