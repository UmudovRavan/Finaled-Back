using Contract.DTOs;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public PerformanceController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        [Authorize(Policy = "CanViewPerformance")]
        [HttpGet("GetPerformanceReport")]
        public async Task<IActionResult> GetPerformanceReport()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var report = await _performanceService.GetTotalPointsAsync(userId);
            return Ok(report);
        }

        [Authorize(Roles = "TenantAdmin,PlatformSuperAdmin,Admin,SuperAdmin")]
        [HttpPost("Add Performance Point")]
        public async Task<IActionResult> AddPerformancePoint([FromBody] PerformancePointDTo point)
        {
            await _performanceService.AddPerformancePointAsync(point);
            return Ok(new { Message = "Performance points added successfully" });
        }

        [Authorize(Policy = "CanViewPerformance")]
        [HttpGet("GetLeaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var leaderboard = await _performanceService.leaderboard();
            return Ok(leaderboard);
        }
    }
}
