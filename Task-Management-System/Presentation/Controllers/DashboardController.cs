using Contract.DTOs.Dashboard;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("company")]
        [Authorize(Policy = "CanViewDashboard")]
        public async Task<IActionResult> GetCompanyDashboard()
        {
            var dashboard = await _dashboardService.GetCompanyDashboardAsync();
            return Ok(dashboard);
        }

        [HttpGet("division/{divisionId:guid}")]
        [Authorize(Policy = "CanViewDashboard")]
        public async Task<IActionResult> GetDivisionDashboard(Guid divisionId)
        {
            var divisionDashboard = await _dashboardService.GetDivisionDashboardAsync(divisionId);
            if (divisionDashboard == null)
                return NotFound(new { message = "Şöbə tapılmadı." });

            return Ok(divisionDashboard);
        }

        [HttpGet("project/{projectId:guid}")]
        [Authorize(Policy = "CanViewDashboard")]
        public async Task<IActionResult> GetProjectDashboard(Guid projectId)
        {
            var projectDashboard = await _dashboardService.GetProjectDashboardAsync(projectId);
            if (projectDashboard == null)
                return NotFound(new { message = "Layihə tapılmadı." });

            return Ok(projectDashboard);
        }

        [HttpPost("filter-tasks")]
        [Authorize(Policy = "CanViewDashboard")]
        public async Task<IActionResult> FilterDashboardTasks([FromBody] DashboardFilterDTO filter)
        {
            var tasks = await _dashboardService.FilterDashboardTasksAsync(filter);
            return Ok(tasks);
        }
    }
}
