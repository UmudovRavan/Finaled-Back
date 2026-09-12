using System;

namespace Contract.DTOs
{
    public class EmployeeWorkloadDTO
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = default!;
        public int ActiveTaskCount { get; set; }
        public int TasksDueToday { get; set; }
        public int OverdueTasks { get; set; }
        public int UrgentTasks { get; set; }
        public int HighTasks { get; set; }
        public string WorkloadLevel { get; set; } = "Low"; // "Low", "Medium", "High", "Overloaded"
        public int WorkloadScore { get; set; }
        public bool IsOverloaded { get; set; }
    }

    public class WorkloadWarningDTO
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public int ActiveTaskCount { get; set; }
        public int TasksDueToday { get; set; }
        public string WorkloadLevel { get; set; } = "Low";
        public bool IsWarning { get; set; }
        public string? WarningMessage { get; set; }
    }
}
