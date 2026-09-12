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
    public class WorkloadController : ControllerBase
    {
        private readonly IWorkloadService _workloadService;

        public WorkloadController(IWorkloadService workloadService)
        {
            _workloadService = workloadService;
        }

        [HttpGet]
        [Authorize(Policy = "CanViewWorkload")]
        public async Task<IActionResult> GetAllWorkloads()
        {
            var workloads = await _workloadService.GetAllEmployeesWorkloadAsync();
            return Ok(workloads);
        }

        [HttpGet("{userId:guid}")]
        [Authorize(Policy = "CanViewWorkload")]
        public async Task<IActionResult> GetEmployeeWorkload(Guid userId)
        {
            var workload = await _workloadService.GetEmployeeWorkloadAsync(userId);
            if (workload == null)
                return NotFound(new { message = "İstifadəçi tapılmadı." });

            return Ok(workload);
        }

        [HttpGet("check-assign/{userId:guid}")]
        [Authorize(Policy = "CanViewWorkload")]
        public async Task<IActionResult> CheckWorkloadBeforeAssign(Guid userId)
        {
            var warning = await _workloadService.CheckWorkloadBeforeAssignAsync(userId);
            return Ok(warning);
        }
    }
}
