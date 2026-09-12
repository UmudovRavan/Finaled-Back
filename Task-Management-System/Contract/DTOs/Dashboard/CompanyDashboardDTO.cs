using System;
using System.Collections.Generic;

namespace Contract.DTOs.Dashboard
{
    public class CompanyDashboardDTO
    {
        public DashboardSummaryDTO Summary { get; set; } = new();
        public List<DivisionDashboardDTO> Divisions { get; set; } = new();
        public List<ProjectDashboardDTO> StandAloneProjects { get; set; } = new();
        public List<EmployeeWorkloadDTO> EmployeeWorkloads { get; set; } = new();
        public List<TaskDTO> RecentUrgentTasks { get; set; } = new();
        public List<TaskDTO> RecentOverdueTasks { get; set; } = new();
    }

    public class DashboardSummaryDTO
    {
        public int TotalDivisions { get; set; }
        public int TotalProjects { get; set; }
        public int TotalLevels { get; set; }
        public int TotalTasks { get; set; }
        public int TotalActiveTasks { get; set; }
        public int TotalCompletedTasks { get; set; }
        public int TotalOverdueTasks { get; set; }
        public int TotalUrgentTasks { get; set; }
        public int TotalEmployees { get; set; }
        public int OverloadedEmployeesCount { get; set; }
        public double CompletionRate { get; set; }
    }

    public class DivisionDashboardDTO
    {
        public Guid DivisionId { get; set; }
        public string DivisionName { get; set; } = default!;
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }
        public List<ProjectDashboardDTO> Projects { get; set; } = new();
    }

    public class ProjectDashboardDTO
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = default!;
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public int TotalLevels { get; set; }
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }
        public List<LevelDashboardDTO> Levels { get; set; } = new();
    }

    public class LevelDashboardDTO
    {
        public Guid LevelId { get; set; }
        public string LevelName { get; set; } = default!;
        public int Order { get; set; }
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
    }

    public class DashboardFilterDTO
    {
        public Guid? DivisionId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? LevelId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public bool? IsOverdue { get; set; }
        public string? SearchQuery { get; set; }
    }
}
