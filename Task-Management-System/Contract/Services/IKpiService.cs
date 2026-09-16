using Contract.DTOs.Kpi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IKpiService
    {
        // ─── Əməkdaş Kabineti ───────────────────────────────────────
        Task<EmployeeKpiSummaryDTO> GetMyKpiSummaryAsync(int? year = null, int? month = null);
        Task<IEnumerable<DailyKpiDTO>> GetMyDailyHistoryAsync(DateOnly? startDate = null, DateOnly? endDate = null);

        // ─── Rəhbər / Menecer Kabineti ─────────────────────────────
        Task<IEnumerable<SubordinateKpiStatusDTO>> GetSubordinatesStatusAsync(DateOnly? date = null);
        Task<DailyKpiDTO> SubmitDailyKpiAsync(CreateDailyKpiDTO dto);

        // ─── Admin / HR Kabineti ───────────────────────────────────
        Task<DailyKpiDTO> UpdateDailyKpiAsync(Guid id, UpdateDailyKpiDTO dto);
        Task<bool> DeleteDailyKpiAsync(Guid id);
        Task<CompanyKpiAnalyticsDTO> GetCompanyAnalyticsAsync(int? year = null, int? month = null, Guid? divisionId = null);
        Task<IEnumerable<DailyKpiDTO>> GetKpiReportAsync(KpiReportFilterDTO filter);

        // ─── Aylıq Yekun Bal və Reytinq (Leaderboard) ──────────────
        Task<IEnumerable<KpiLeaderboardItemDTO>> GetMonthlyLeaderboardAsync(int? year = null, int? month = null, Guid? divisionId = null);
    }
}
