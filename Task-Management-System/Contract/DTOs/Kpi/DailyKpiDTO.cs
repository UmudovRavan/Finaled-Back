using System;

namespace Contract.DTOs.Kpi
{
    public class DailyKpiDTO
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeEmail { get; set; }
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }
        public Guid EvaluatorId { get; set; }
        public string? EvaluatorName { get; set; }
        public DateOnly EvaluationDate { get; set; }
        public int JobDutiesScore { get; set; }
        public int DisciplineScore { get; set; }
        public int BonusScore { get; set; }
        public int TotalScore { get; set; }
        public string? DisciplinePenaltyReason { get; set; }
        public string? BonusReason { get; set; }
        public string? Comments { get; set; }
        public bool IsAdminEdited { get; set; }
        public string? AdminEditReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
