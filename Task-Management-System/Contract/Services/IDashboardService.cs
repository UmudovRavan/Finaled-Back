using Contract.DTOs;
using Contract.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IDashboardService
    {
        Task<CompanyDashboardDTO> GetCompanyDashboardAsync();
        Task<DivisionDashboardDTO?> GetDivisionDashboardAsync(Guid divisionId);
        Task<ProjectDashboardDTO?> GetProjectDashboardAsync(Guid projectId);
        Task<IEnumerable<TaskDTO>> FilterDashboardTasksAsync(DashboardFilterDTO filter);
    }
}
