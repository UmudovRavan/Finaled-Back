using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorkloadService : IWorkloadService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _tenantService;

        private static readonly CurrentSituation[] ActiveStatuses = new[]
        {
            CurrentSituation.Pending,
            CurrentSituation.Assigned,
            CurrentSituation.InProgress,
            CurrentSituation.UnderReview
        };

        public WorkloadService(
            AppDbContext context,
            ICurrentTenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<EmployeeWorkloadDTO>> GetAllEmployeesWorkloadAsync()
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var employees = await _context.AppUsers
                .Where(u => u.TenantId == tenantId)
                .Include(u => u.AssignedTasks.Where(t => !t.IsDeleted && ActiveStatuses.Contains(t.Status)))
                .ToListAsync();

            var result = new List<EmployeeWorkloadDTO>();

            foreach (var emp in employees)
            {
                var activeTasks = emp.AssignedTasks.ToList();
                var activeCount = activeTasks.Count;
                var dueTodayCount = activeTasks.Count(t => t.Deadline >= today && t.Deadline < tomorrow);
                var overdueCount = activeTasks.Count(t => t.Deadline < DateTime.UtcNow);
                var urgentCount = activeTasks.Count(t => t.Priority == Priority.Urgent);
                var highCount = activeTasks.Count(t => t.Priority == Priority.High);

                var workloadLevel = DetermineWorkloadLevel(activeCount, dueTodayCount);
                var score = CalculateWorkloadScore(activeTasks);

                result.Add(new EmployeeWorkloadDTO
                {
                    UserId = emp.Id,
                    FullName = emp.FullName ?? emp.UserName,
                    Email = emp.Email,
                    ActiveTaskCount = activeCount,
                    TasksDueToday = dueTodayCount,
                    OverdueTasks = overdueCount,
                    UrgentTasks = urgentCount,
                    HighTasks = highCount,
                    WorkloadLevel = workloadLevel,
                    WorkloadScore = score,
                    IsOverloaded = workloadLevel == "Overloaded"
                });
            }

            return result.OrderByDescending(r => r.WorkloadScore);
        }

        public async Task<EmployeeWorkloadDTO?> GetEmployeeWorkloadAsync(Guid userId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var emp = await _context.AppUsers
                .Where(u => u.Id == userId && u.TenantId == tenantId)
                .Include(u => u.AssignedTasks.Where(t => !t.IsDeleted && ActiveStatuses.Contains(t.Status)))
                .FirstOrDefaultAsync();

            if (emp == null)
                return null;

            var activeTasks = emp.AssignedTasks.ToList();
            var activeCount = activeTasks.Count;
            var dueTodayCount = activeTasks.Count(t => t.Deadline >= today && t.Deadline < tomorrow);
            var overdueCount = activeTasks.Count(t => t.Deadline < DateTime.UtcNow);
            var urgentCount = activeTasks.Count(t => t.Priority == Priority.Urgent);
            var highCount = activeTasks.Count(t => t.Priority == Priority.High);

            var workloadLevel = DetermineWorkloadLevel(activeCount, dueTodayCount);
            var score = CalculateWorkloadScore(activeTasks);

            return new EmployeeWorkloadDTO
            {
                UserId = emp.Id,
                FullName = emp.FullName ?? emp.UserName,
                Email = emp.Email,
                ActiveTaskCount = activeCount,
                TasksDueToday = dueTodayCount,
                OverdueTasks = overdueCount,
                UrgentTasks = urgentCount,
                HighTasks = highCount,
                WorkloadLevel = workloadLevel,
                WorkloadScore = score,
                IsOverloaded = workloadLevel == "Overloaded"
            };
        }

        public async Task<WorkloadWarningDTO> CheckWorkloadBeforeAssignAsync(Guid userId)
        {
            var workload = await GetEmployeeWorkloadAsync(userId);
            if (workload == null)
            {
                return new WorkloadWarningDTO
                {
                    UserId = userId,
                    IsWarning = false
                };
            }

            bool isWarning = workload.WorkloadLevel == "High" || workload.WorkloadLevel == "Overloaded";
            string? warningMessage = null;

            if (workload.WorkloadLevel == "Overloaded")
            {
                warningMessage = $"⚠️ DİQQƏT: Əməkdaş ({workload.FullName ?? workload.Email}) hal-hazırda həddindən artıq yüklənib! " +
                                 $"Aktiv tapşırıq sayı: {workload.ActiveTaskCount}, Bu günə olan tapşırıq: {workload.TasksDueToday}. " +
                                 $"Yeni tapşırıq təyin etməzdən əvvəl nəzərə alın.";
            }
            else if (workload.WorkloadLevel == "High")
            {
                warningMessage = $"ℹ️ Məlumat: Əməkdaşın ({workload.FullName ?? workload.Email}) iş yükü yüksəkdir. " +
                                 $"Aktiv tapşırıq sayı: {workload.ActiveTaskCount}.";
            }

            return new WorkloadWarningDTO
            {
                UserId = userId,
                FullName = workload.FullName,
                ActiveTaskCount = workload.ActiveTaskCount,
                TasksDueToday = workload.TasksDueToday,
                WorkloadLevel = workload.WorkloadLevel,
                IsWarning = isWarning,
                WarningMessage = warningMessage
            };
        }

        private static string DetermineWorkloadLevel(int activeCount, int dueTodayCount)
        {
            if (activeCount >= 13 || dueTodayCount >= 5)
                return "Overloaded";
            if (activeCount >= 8)
                return "High";
            if (activeCount >= 4)
                return "Medium";
            return "Low";
        }

        private static int CalculateWorkloadScore(List<TaskItem> activeTasks)
        {
            // Score based on difficulty and priority
            int score = 0;
            foreach (var t in activeTasks)
            {
                int diffMultiplier = t.Difficulty switch
                {
                    DifficultyLevel.Hard => 3,
                    DifficultyLevel.Medium => 2,
                    _ => 1
                };

                int priorityMultiplier = t.Priority switch
                {
                    Priority.Urgent => 4,
                    Priority.High => 3,
                    Priority.Normal => 2,
                    _ => 1
                };

                score += diffMultiplier * priorityMultiplier;
            }
            return score;
        }
    }
}
