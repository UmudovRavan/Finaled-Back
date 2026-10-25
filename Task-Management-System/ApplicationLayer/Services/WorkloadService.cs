using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorkloadService : IWorkloadService
    {
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IAppUserRepository _appUserRepo;
        private readonly ICurrentTenantService _tenantService;

        // Kvota limitləri: Asan, Orta və Çətin statuslu tapşırıqların hərəsinə 10 tapşırıq
        public const int MaxEasyTasks = 10;
        public const int MaxMediumTasks = 10;
        public const int MaxHardTasks = 10;
        public const int MaxTotalTasks = MaxEasyTasks + MaxMediumTasks + MaxHardTasks; // 30 tapşırıq

        private static readonly CurrentSituation[] ActiveStatuses = new[]
        {
            CurrentSituation.Pending,
            CurrentSituation.Assigned,
            CurrentSituation.InProgress,
            CurrentSituation.UnderReview
        };

        public WorkloadService(
            IGenericRepository<TaskItem> taskRepo,
            IAppUserRepository appUserRepo,
            ICurrentTenantService tenantService)
        {
            _taskRepo = taskRepo;
            _appUserRepo = appUserRepo;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<EmployeeWorkloadDTO>> GetAllEmployeesWorkloadAsync()
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var employees = await _appUserRepo.GetByTenantAsync(tenantId);
            var activeTasks = await _taskRepo.GetAllAsync(q => q.Where(t => ActiveStatuses.Contains(t.Status) && t.AssignedToUserId.HasValue));

            var tasksByUser = activeTasks
                .Where(t => t.AssignedToUserId.HasValue)
                .GroupBy(t => t.AssignedToUserId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<EmployeeWorkloadDTO>();

            foreach (var emp in employees)
            {
                tasksByUser.TryGetValue(emp.Id, out var empActiveTasks);
                empActiveTasks ??= new List<TaskItem>();

                var activeCount = empActiveTasks.Count;
                var easyCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Easy);
                var mediumCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Medium);
                var hardCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Hard);

                var dueTodayCount = empActiveTasks.Count(t => t.Deadline >= today && t.Deadline < tomorrow);
                var overdueCount = empActiveTasks.Count(t => t.Deadline < DateTime.UtcNow);
                var urgentCount = empActiveTasks.Count(t => t.Priority == Priority.Urgent);
                var highCount = empActiveTasks.Count(t => t.Priority == Priority.High);

                var workloadLevel = DetermineWorkloadLevel(easyCount, mediumCount, hardCount, activeCount, dueTodayCount);
                var score = CalculateWorkloadScore(empActiveTasks);

                result.Add(new EmployeeWorkloadDTO
                {
                    UserId = emp.Id,
                    FullName = emp.FullName ?? emp.UserName,
                    Email = emp.Email,
                    ActiveTaskCount = activeCount,
                    EasyTasksCount = easyCount,
                    MediumTasksCount = mediumCount,
                    HardTasksCount = hardCount,
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

            var emp = await _appUserRepo.GetByIdAsync(userId);
            if (emp == null || emp.TenantId != tenantId)
                return null;

            var empActiveTasks = await _taskRepo.GetAllAsync(q => q.Where(t => t.AssignedToUserId == userId && ActiveStatuses.Contains(t.Status)));

            var activeCount = empActiveTasks.Count;
            var easyCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Easy);
            var mediumCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Medium);
            var hardCount = empActiveTasks.Count(t => t.Difficulty == DifficultyLevel.Hard);

            var dueTodayCount = empActiveTasks.Count(t => t.Deadline >= today && t.Deadline < tomorrow);
            var overdueCount = empActiveTasks.Count(t => t.Deadline < DateTime.UtcNow);
            var urgentCount = empActiveTasks.Count(t => t.Priority == Priority.Urgent);
            var highCount = empActiveTasks.Count(t => t.Priority == Priority.High);

            var workloadLevel = DetermineWorkloadLevel(easyCount, mediumCount, hardCount, activeCount, dueTodayCount);
            var score = CalculateWorkloadScore(empActiveTasks);

            return new EmployeeWorkloadDTO
            {
                UserId = emp.Id,
                FullName = emp.FullName ?? emp.UserName,
                Email = emp.Email,
                ActiveTaskCount = activeCount,
                EasyTasksCount = easyCount,
                MediumTasksCount = mediumCount,
                HardTasksCount = hardCount,
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
                var exceededCategory = new List<string>();
                if (workload.EasyTasksCount > MaxEasyTasks) exceededCategory.Add($"Asan ({workload.EasyTasksCount}/{MaxEasyTasks})");
                if (workload.MediumTasksCount > MaxMediumTasks) exceededCategory.Add($"Orta ({workload.MediumTasksCount}/{MaxMediumTasks})");
                if (workload.HardTasksCount > MaxHardTasks) exceededCategory.Add($"Çətin ({workload.HardTasksCount}/{MaxHardTasks})");
                if (workload.ActiveTaskCount > MaxTotalTasks) exceededCategory.Add($"Ümumi ({workload.ActiveTaskCount}/{MaxTotalTasks})");

                string reason = exceededCategory.Count > 0 ? string.Join(", ", exceededCategory) : "Gündəlik təcili tapşırıqlar həddi";

                warningMessage = $"⚠️ DİQQƏT: Əməkdaş ({workload.FullName ?? workload.Email}) hal-hazırda həddindən artıq yüklənib! " +
                                 $"Aşılmış limitlər: {reason}. " +
                                 $"Aktiv cəmi: {workload.ActiveTaskCount} (Asan: {workload.EasyTasksCount}/10, Orta: {workload.MediumTasksCount}/10, Çətin: {workload.HardTasksCount}/10). " +
                                 $"Yeni tapşırıq təyin etməzdən əvvəl nəzərə alın.";
            }
            else if (workload.WorkloadLevel == "High")
            {
                warningMessage = $"ℹ️ Məlumat: Əməkdaşın ({workload.FullName ?? workload.Email}) iş yükü kvotanın son həddindədir. " +
                                 $"Aktiv: {workload.ActiveTaskCount} (Asan: {workload.EasyTasksCount}/10, Orta: {workload.MediumTasksCount}/10, Çətin: {workload.HardTasksCount}/10).";
            }

            return new WorkloadWarningDTO
            {
                UserId = userId,
                FullName = workload.FullName,
                ActiveTaskCount = workload.ActiveTaskCount,
                EasyTasksCount = workload.EasyTasksCount,
                MediumTasksCount = workload.MediumTasksCount,
                HardTasksCount = workload.HardTasksCount,
                TasksDueToday = workload.TasksDueToday,
                WorkloadLevel = workload.WorkloadLevel,
                IsWarning = isWarning,
                WarningMessage = warningMessage
            };
        }

        /// <summary>
        /// Asan, Orta və Çətin statuslu tapşırıqların hərəsinə 10 tapşırıq limiti:
        /// 10 Asan + 10 Orta + 10 Çətin (cəmi 30) təyin edildikdə belə işçi "Overloaded" kimi görünmür!
        /// Yalnız bu limitlərdən hər hansı biri 10-u keçdikdə (və ya ümumi 30-u aşdıqda) "Overloaded" olur.
        /// </summary>
        private static string DetermineWorkloadLevel(int easyCount, int mediumCount, int hardCount, int totalActive, int dueTodayCount)
        {
            // Limitlər aşıldıqda:
            if (easyCount > MaxEasyTasks || mediumCount > MaxMediumTasks || hardCount > MaxHardTasks || totalActive > MaxTotalTasks || dueTodayCount >= 5)
            {
                return "Overloaded";
            }

            // Həddə yaxınlaşdıqda (Kvota daxilindədir, amma son həddədir):
            if (easyCount == MaxEasyTasks || mediumCount == MaxMediumTasks || hardCount == MaxHardTasks || totalActive >= 24)
            {
                return "High";
            }

            // Orta iş yükü:
            if (totalActive >= 10 || hardCount >= 4 || mediumCount >= 5)
            {
                return "Medium";
            }

            return "Low";
        }

        private static int CalculateWorkloadScore(List<TaskItem> activeTasks)
        {
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
