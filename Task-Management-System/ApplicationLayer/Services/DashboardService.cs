using AutoMapper;
using Contract.DTOs;
using Contract.DTOs.Dashboard;
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
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentTenantService _tenantService;
        private readonly IWorkloadService _workloadService;

        private static readonly CurrentSituation[] ActiveStatuses = new[]
        {
            CurrentSituation.Pending,
            CurrentSituation.Assigned,
            CurrentSituation.InProgress,
            CurrentSituation.UnderReview
        };

        public DashboardService(
            AppDbContext context,
            IMapper mapper,
            ICurrentTenantService tenantService,
            IWorkloadService workloadService)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
            _workloadService = workloadService;
        }

        public async Task<CompanyDashboardDTO> GetCompanyDashboardAsync()
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var now = DateTime.UtcNow;

            // Load all relevant data for tenant
            var allTasks = await _context.Tasks
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Level)
                    .ThenInclude(l => l!.Project)
                        .ThenInclude(p => p.Division)
                .ToListAsync();

            var allDivisions = await _context.Divisions
                .Where(d => d.TenantId == tenantId && !d.IsDeleted)
                .Include(d => d.Manager)
                .Include(d => d.Projects.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Levels.Where(l => !l.IsDeleted))
                .ToListAsync();

            var standAloneProjects = await _context.Projects
                .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.DivisionId == null)
                .Include(p => p.Manager)
                .Include(p => p.Levels.Where(l => !l.IsDeleted))
                .ToListAsync();

            var totalTasks = allTasks.Count;
            var completedTasks = allTasks.Count(t => t.Status == CurrentSituation.Completed);
            var activeTasks = allTasks.Count(t => ActiveStatuses.Contains(t.Status));
            var overdueTasks = allTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now);
            var urgentTasks = allTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Priority == Priority.Urgent);

            var totalProjects = await _context.Projects.CountAsync(p => p.TenantId == tenantId && !p.IsDeleted);
            var totalLevels = await _context.ProjectLevels.CountAsync(l => l.TenantId == tenantId && !l.IsDeleted);
            var totalEmployees = await _context.AppUsers.CountAsync(u => u.TenantId == tenantId);

            var workloads = (await _workloadService.GetAllEmployeesWorkloadAsync()).ToList();
            var overloadedCount = workloads.Count(w => w.IsOverloaded);

            var summary = new DashboardSummaryDTO
            {
                TotalDivisions = allDivisions.Count,
                TotalProjects = totalProjects,
                TotalLevels = totalLevels,
                TotalTasks = totalTasks,
                TotalActiveTasks = activeTasks,
                TotalCompletedTasks = completedTasks,
                TotalOverdueTasks = overdueTasks,
                TotalUrgentTasks = urgentTasks,
                TotalEmployees = totalEmployees,
                OverloadedEmployeesCount = overloadedCount,
                CompletionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 2) : 0
            };

            // Map divisions dashboard
            var divisionDtos = new List<DivisionDashboardDTO>();
            foreach (var div in allDivisions)
            {
                var divTasks = allTasks.Where(t => t.Level?.Project?.DivisionId == div.Id).ToList();
                var divCompleted = divTasks.Count(t => t.Status == CurrentSituation.Completed);
                var divActive = divTasks.Count(t => ActiveStatuses.Contains(t.Status));
                var divOverdue = divTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now);

                var divProjects = new List<ProjectDashboardDTO>();
                foreach (var proj in div.Projects.Where(p => !p.IsDeleted))
                {
                    divProjects.Add(BuildProjectDashboardDTO(proj, allTasks, now, div.Name));
                }

                divisionDtos.Add(new DivisionDashboardDTO
                {
                    DivisionId = div.Id,
                    DivisionName = div.Name,
                    ManagerName = div.Manager?.FullName ?? div.Manager?.UserName,
                    ManagerEmail = div.Manager?.Email,
                    TotalProjects = div.Projects.Count(p => !p.IsDeleted),
                    TotalTasks = divTasks.Count,
                    ActiveTasks = divActive,
                    CompletedTasks = divCompleted,
                    OverdueTasks = divOverdue,
                    CompletionRate = divTasks.Count > 0 ? Math.Round((double)divCompleted / divTasks.Count * 100, 2) : 0,
                    Projects = divProjects
                });
            }

            // Standalone projects dashboard
            var standAloneProjectDtos = standAloneProjects
                .Select(p => BuildProjectDashboardDTO(p, allTasks, now, null))
                .ToList();

            // Recent Urgent & Overdue
            var recentUrgent = allTasks
                .Where(t => ActiveStatuses.Contains(t.Status) && t.Priority == Priority.Urgent)
                .OrderBy(t => t.Deadline)
                .Take(10)
                .ToList();

            var recentOverdue = allTasks
                .Where(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now)
                .OrderBy(t => t.Deadline)
                .Take(10)
                .ToList();

            return new CompanyDashboardDTO
            {
                Summary = summary,
                Divisions = divisionDtos,
                StandAloneProjects = standAloneProjectDtos,
                EmployeeWorkloads = workloads,
                RecentUrgentTasks = _mapper.Map<List<TaskDTO>>(recentUrgent),
                RecentOverdueTasks = _mapper.Map<List<TaskDTO>>(recentOverdue)
            };
        }

        public async Task<DivisionDashboardDTO?> GetDivisionDashboardAsync(Guid divisionId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var now = DateTime.UtcNow;

            var div = await _context.Divisions
                .Where(d => d.Id == divisionId && d.TenantId == tenantId && !d.IsDeleted)
                .Include(d => d.Manager)
                .Include(d => d.Projects.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Levels.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync();

            if (div == null)
                return null;

            var divTasks = await _context.Tasks
                .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Level != null && t.Level.Project.DivisionId == divisionId)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Level)
                    .ThenInclude(l => l!.Project)
                .ToListAsync();

            var divCompleted = divTasks.Count(t => t.Status == CurrentSituation.Completed);
            var divActive = divTasks.Count(t => ActiveStatuses.Contains(t.Status));
            var divOverdue = divTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now);

            var divProjects = div.Projects.Where(p => !p.IsDeleted)
                .Select(p => BuildProjectDashboardDTO(p, divTasks, now, div.Name))
                .ToList();

            return new DivisionDashboardDTO
            {
                DivisionId = div.Id,
                DivisionName = div.Name,
                ManagerName = div.Manager?.FullName ?? div.Manager?.UserName,
                ManagerEmail = div.Manager?.Email,
                TotalProjects = div.Projects.Count(p => !p.IsDeleted),
                TotalTasks = divTasks.Count,
                ActiveTasks = divActive,
                CompletedTasks = divCompleted,
                OverdueTasks = divOverdue,
                CompletionRate = divTasks.Count > 0 ? Math.Round((double)divCompleted / divTasks.Count * 100, 2) : 0,
                Projects = divProjects
            };
        }

        public async Task<ProjectDashboardDTO?> GetProjectDashboardAsync(Guid projectId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var now = DateTime.UtcNow;

            var proj = await _context.Projects
                .Where(p => p.Id == projectId && p.TenantId == tenantId && !p.IsDeleted)
                .Include(p => p.Division)
                .Include(p => p.Manager)
                .Include(p => p.Levels.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync();

            if (proj == null)
                return null;

            var projTasks = await _context.Tasks
                .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Level != null && t.Level.ProjectId == projectId)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Level)
                .ToListAsync();

            return BuildProjectDashboardDTO(proj, projTasks, now, proj.Division?.Name);
        }

        public async Task<IEnumerable<TaskDTO>> FilterDashboardTasksAsync(DashboardFilterDTO filter)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var query = _context.Tasks
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Level)
                    .ThenInclude(l => l!.Project)
                        .ThenInclude(p => p.Division)
                .AsQueryable();

            if (filter.DivisionId.HasValue)
            {
                query = query.Where(t => t.Level != null && t.Level.Project != null && t.Level.Project.DivisionId == filter.DivisionId.Value);
            }

            if (filter.ProjectId.HasValue)
            {
                query = query.Where(t => t.Level != null && t.Level.ProjectId == filter.ProjectId.Value);
            }

            if (filter.LevelId.HasValue)
            {
                query = query.Where(t => t.LevelId == filter.LevelId.Value);
            }

            if (filter.AssignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<CurrentSituation>(filter.Status, true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(filter.Priority) && Enum.TryParse<Priority>(filter.Priority, true, out var priorityEnum))
            {
                query = query.Where(t => t.Priority == priorityEnum);
            }

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(search) || (t.Description != null && t.Description.ToLower().Contains(search)));
            }

            var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<TaskDTO>>(tasks);
        }

        private static ProjectDashboardDTO BuildProjectDashboardDTO(Project proj, List<TaskItem> tasks, DateTime now, string? divisionName)
        {
            var projTasks = tasks.Where(t => t.Level != null && t.Level.ProjectId == proj.Id).ToList();
            var projCompleted = projTasks.Count(t => t.Status == CurrentSituation.Completed);
            var projActive = projTasks.Count(t => ActiveStatuses.Contains(t.Status));
            var projOverdue = projTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now);

            var levelDtos = new List<LevelDashboardDTO>();
            foreach (var level in proj.Levels.Where(l => !l.IsDeleted).OrderBy(l => l.Order))
            {
                var levelTasks = projTasks.Where(t => t.LevelId == level.Id).ToList();
                levelDtos.Add(new LevelDashboardDTO
                {
                    LevelId = level.Id,
                    LevelName = level.Name,
                    Order = level.Order,
                    TotalTasks = levelTasks.Count,
                    ActiveTasks = levelTasks.Count(t => ActiveStatuses.Contains(t.Status)),
                    CompletedTasks = levelTasks.Count(t => t.Status == CurrentSituation.Completed),
                    OverdueTasks = levelTasks.Count(t => ActiveStatuses.Contains(t.Status) && t.Deadline < now)
                });
            }

            return new ProjectDashboardDTO
            {
                ProjectId = proj.Id,
                ProjectName = proj.Name,
                DivisionId = proj.DivisionId,
                DivisionName = divisionName ?? proj.Division?.Name,
                ManagerName = proj.Manager?.FullName ?? proj.Manager?.UserName,
                ManagerEmail = proj.Manager?.Email,
                TotalLevels = proj.Levels.Count(l => !l.IsDeleted),
                TotalTasks = projTasks.Count,
                ActiveTasks = projActive,
                CompletedTasks = projCompleted,
                OverdueTasks = projOverdue,
                CompletionRate = projTasks.Count > 0 ? Math.Round((double)projCompleted / projTasks.Count * 100, 2) : 0,
                Levels = levelDtos
            };
        }
    }
}
