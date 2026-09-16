using Contract.DTOs.Kpi;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    /// <summary>
    /// BMG International "Dəyər-Zərər" KPI və Performans Qiymətləndirmə Sistemi Controller-i.
    /// Əməkdaş, Rəhbər/Menecer və Admin/HR kabinetləri üçün endpoint-lər təqdim edir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KpiController : ControllerBase
    {
        private readonly IKpiService _kpiService;

        public KpiController(IKpiService kpiService)
        {
            _kpiService = kpiService;
        }

        // ─────────────────────────────────────────────────────────────
        // 1. ƏMƏKDAŞ KABİNETİ (Bütün istifadəçilər öz ballarını görə bilər)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Əməkdaşın öz cari gün, həftəlik və aylıq KPI xülasəsi və mənfiyə düşmə səbəbləri.
        /// GET /api/kpi/my-summary?year=2026&month=9
        /// </summary>
        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMySummary([FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            var summary = await _kpiService.GetMyKpiSummaryAsync(year, month);
            return Ok(summary);
        }

        /// <summary>
        /// Əməkdaşın günbəgün ətraflı KPI tarixçəsi.
        /// GET /api/kpi/my-history?startDate=2026-09-01&endDate=2026-09-30
        /// </summary>
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory([FromQuery] DateOnly? startDate = null, [FromQuery] DateOnly? endDate = null)
        {
            var history = await _kpiService.GetMyDailyHistoryAsync(startDate, endDate);
            return Ok(history);
        }

        // ─────────────────────────────────────────────────────────────
        // 2. RƏHBƏR / MENECER KABİNETİ
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Menecerin tabeliyində olan əməkdaşların siyahısı və seçilmiş tarix üzrə qiymətləndirmə statusu.
        /// GET /api/kpi/subordinates-status?date=2026-09-16
        /// </summary>
        [HttpGet("subordinates-status")]
        [Authorize(Policy = "CanViewPerformance")]
        public async Task<IActionResult> GetSubordinatesStatus([FromQuery] DateOnly? date = null)
        {
            var statusList = await _kpiService.GetSubordinatesStatusAsync(date);
            return Ok(statusList);
        }

        /// <summary>
        /// Menecer tərəfindən tabelikdəki əməkdaşa cari gün üçün bal daxil edilməsi (+1/0, 0/-1, +1/0).
        /// POST /api/kpi/submit
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitDailyKpi([FromBody] CreateDailyKpiDTO dto)
        {
            var result = await _kpiService.SubmitDailyKpiAsync(dto);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. ADMIN / HR KABİNETİ
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Səhv daxil edilmiş balın Admin və ya HR tərəfindən redaktə olunması (səbəb qeyd edilməklə).
        /// PUT /api/kpi/{id}
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "TenantAdmin,PlatformSuperAdmin,Admin,SuperAdmin,TmsDirector,HR")]
        public async Task<IActionResult> UpdateDailyKpi(Guid id, [FromBody] UpdateDailyKpiDTO dto)
        {
            var result = await _kpiService.UpdateDailyKpiAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Bal qeydinin silinməsi (yalnız Admin / HR).
        /// DELETE /api/kpi/{id}
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "TenantAdmin,PlatformSuperAdmin,Admin,SuperAdmin,TmsDirector,HR")]
        public async Task<IActionResult> DeleteDailyKpi(Guid id)
        {
            var deleted = await _kpiService.DeleteDailyKpiAsync(id);
            if (!deleted)
                return NotFound(new { message = "KPI qeydi tapılmadı." });

            return Ok(new { message = "KPI qeydi uğurla silindi." });
        }

        /// <summary>
        /// Şirkət və şöbələr üzrə ümumi analitika və trend qrafikləri.
        /// GET /api/kpi/analytics?year=2026&month=9&divisionId=...
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Policy = "CanViewDashboard")]
        public async Task<IActionResult> GetCompanyAnalytics([FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] Guid? divisionId = null)
        {
            var analytics = await _kpiService.GetCompanyAnalyticsAsync(year, month, divisionId);
            return Ok(analytics);
        }

        /// <summary>
        /// Tarix, şöbə və işçi üzrə filtrlənmiş ətraflı hesabatların çıxarılması.
        /// GET /api/kpi/report
        /// </summary>
        [HttpGet("report")]
        [Authorize(Policy = "CanViewPerformance")]
        public async Task<IActionResult> GetReport([FromQuery] KpiReportFilterDTO filter)
        {
            var report = await _kpiService.GetKpiReportAsync(filter);
            return Ok(report);
        }

        // ─────────────────────────────────────────────────────────────
        // 4. AYLIK YEKUN BAL VƏ REYTİNQ (LEADERBOARD)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Aylıq Yekun Bal (Cumulative Score) üzrə reytinq cədvəli.
        /// GET /api/kpi/leaderboard?year=2026&month=9&divisionId=...
        /// </summary>
        [HttpGet("leaderboard")]
        [Authorize(Policy = "CanViewPerformance")]
        public async Task<IActionResult> GetLeaderboard([FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] Guid? divisionId = null)
        {
            var leaderboard = await _kpiService.GetMonthlyLeaderboardAsync(year, month, divisionId);
            return Ok(leaderboard);
        }
    }
}
