using Contract.DTOs.Kpi;
using Contract.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class KpiService : IKpiService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly ILogger<KpiService> _logger;

        public KpiService(
            AppDbContext context,
            ICurrentTenantService tenantService,
            ILogger<KpiService> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        private Guid GetRequiredTenantId()
        {
            return _tenantService.TenantId
                ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
        }

        private Guid GetRequiredUserId()
        {
            return _tenantService.UserId
                ?? throw new UnauthorizedAccessException("İstifadəçi identifikasiya olunmayıb.");
        }

        private bool IsAdminOrHr()
        {
            if (_tenantService.IsPlatformSuperAdmin || _tenantService.IsTenantAdmin)
                return true;

            var roles = _tenantService.Roles.ToList();
            if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                               r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                               r.Equals("TmsDirector", StringComparison.OrdinalIgnoreCase) ||
                               r.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
                               r.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               r.IndexOf("HR", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }

            if (_tenantService.HasPermission("hr.read") ||
                _tenantService.HasPermission("hr.write") ||
                _tenantService.HasPermission("tms.kpi.admin") ||
                _tenantService.HasPermission("tms.dashboard.view"))
            {
                return true;
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // 1. ƏMƏKDAŞ KABİNETİ
        // ─────────────────────────────────────────────────────────────

        public async Task<EmployeeKpiSummaryDTO> GetMyKpiSummaryAsync(int? year = null, int? month = null)
        {
            var tenantId = GetRequiredTenantId();
            var userId = GetRequiredUserId();

            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var employee = await _context.AppUsers
                .Include(u => u.Division)
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (employee == null)
                throw new KeyNotFoundException("Əməkdaş profili tapılmadı.");

            // Bütün qeydlər (cari ay və ümumi tarixçə üçün)
            var allMyRecords = await _context.DailyKpiRecords
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && r.EmployeeId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.EvaluationDate)
                .ToListAsync();

            // Bu günün qeydi
            var todayRecord = allMyRecords.FirstOrDefault(r => r.EvaluationDate == today);

            // Cari həftənin sərhədləri (Bazar ertəsindən Bazar gününə)
            var currentDayOfWeek = (int)today.DayOfWeek;
            // Əgər Bazar günüdürsə (0), C#-da 7 kimi qəbul edək
            int daysSinceMonday = currentDayOfWeek == 0 ? 6 : currentDayOfWeek - 1;
            var monday = today.AddDays(-daysSinceMonday);
            var sunday = monday.AddDays(6);

            var weekRecords = allMyRecords
                .Where(r => r.EvaluationDate >= monday && r.EvaluationDate <= sunday)
                .ToList();

            var monthRecords = allMyRecords
                .Where(r => r.EvaluationDate.Year == targetYear && r.EvaluationDate.Month == targetMonth)
                .ToList();

            var negativeRecords = allMyRecords
                .Where(r => r.DisciplineScore < 0 || r.TotalScore < 0)
                .ToList();

            return new EmployeeKpiSummaryDTO
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName ?? employee.UserName ?? employee.Email,
                EmployeeEmail = employee.Email,
                DivisionId = employee.DivisionId,
                DivisionName = employee.Division?.Name,
                TodayScore = todayRecord != null ? MapToDTO(todayRecord) : null,
                WeeklyTotalScore = weekRecords.Sum(r => r.TotalScore),
                WeeklyAverageScore = weekRecords.Count > 0 ? Math.Round(weekRecords.Average(r => r.TotalScore), 2) : 0,
                MonthlyTotalScore = monthRecords.Sum(r => r.TotalScore),
                MonthlyAverageScore = monthRecords.Count > 0 ? Math.Round(monthRecords.Average(r => r.TotalScore), 2) : 0,
                DisciplineViolationsCount = allMyRecords.Count(r => r.DisciplineScore == -1),
                NegativeScoresHistory = negativeRecords.Select(MapToDTO).ToList(),
                RecentHistory = allMyRecords.Take(10).Select(MapToDTO).ToList()
            };
        }

        public async Task<IEnumerable<DailyKpiDTO>> GetMyDailyHistoryAsync(DateOnly? startDate = null, DateOnly? endDate = null)
        {
            var tenantId = GetRequiredTenantId();
            var userId = GetRequiredUserId();

            var query = _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && r.EmployeeId == userId && !r.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(r => r.EvaluationDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.EvaluationDate <= endDate.Value);

            var records = await query
                .OrderByDescending(r => r.EvaluationDate)
                .ToListAsync();

            return records.Select(MapToDTO);
        }

        // ─────────────────────────────────────────────────────────────
        // 2. RƏHBƏR / MENECER KABİNETİ
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<SubordinateKpiStatusDTO>> GetSubordinatesStatusAsync(DateOnly? date = null)
        {
            var tenantId = GetRequiredTenantId();
            var userId = GetRequiredUserId();
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var subordinates = await GetSubordinatesListAsync(tenantId, userId);

            if (subordinates.Count == 0)
                return Enumerable.Empty<SubordinateKpiStatusDTO>();

            var subIds = subordinates.Select(s => s.Id).ToList();

            var dateRecords = await _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && subIds.Contains(r.EmployeeId) && r.EvaluationDate == targetDate && !r.IsDeleted)
                .ToDictionaryAsync(r => r.EmployeeId);

            var result = new List<SubordinateKpiStatusDTO>();
            foreach (var sub in subordinates)
            {
                dateRecords.TryGetValue(sub.Id, out var rec);
                result.Add(new SubordinateKpiStatusDTO
                {
                    EmployeeId = sub.Id,
                    EmployeeName = sub.FullName ?? sub.UserName ?? sub.Email,
                    EmployeeEmail = sub.Email,
                    DivisionId = sub.DivisionId,
                    DivisionName = sub.Division?.Name,
                    IsEvaluatedToday = rec != null,
                    TodayKpi = rec != null ? MapToDTO(rec) : null
                });
            }

            return result;
        }

        public async Task<DailyKpiDTO> SubmitDailyKpiAsync(CreateDailyKpiDTO dto)
        {
            var tenantId = GetRequiredTenantId();
            var userId = GetRequiredUserId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (dto.EmployeeId == userId && !IsAdminOrHr())
                throw new InvalidOperationException("Menecer öz fəaliyyətinə KPI balı daxil edə bilməz.");

            // 1. Bal Hüdudlarının Validasiyası
            if (dto.JobDutiesScore != 0 && dto.JobDutiesScore != 1)
                throw new ArgumentException("Vəzifə öhdəlikləri balı yalnız 0 və ya 1 ola bilər.");

            if (dto.DisciplineScore != 0 && dto.DisciplineScore != -1)
                throw new ArgumentException("İntizam balı yalnız 0 və ya -1 ola bilər.");

            if (dto.BonusScore != 0 && dto.BonusScore != 1)
                throw new ArgumentException("Bonus balı yalnız 0 və ya 1 ola bilər.");

            if (dto.DisciplineScore == -1 && string.IsNullOrWhiteSpace(dto.DisciplinePenaltyReason))
                throw new ArgumentException("İntizam qaydası pozulduqda (-1) cərimə səbəbi mütləq qeyd edilməlidir.");

            // 2. Tabelik hüququnun yoxlanılması
            var employee = await _context.AppUsers
                .Include(u => u.Division)
                .FirstOrDefaultAsync(u => u.Id == dto.EmployeeId && u.TenantId == tenantId);

            if (employee == null)
                throw new KeyNotFoundException("Əməkdaş tapılmadı.");

            if (!IsAdminOrHr())
            {
                // Menecer yalnız öz Division-ındakı əməkdaşları qiymətləndirə bilər
                var isManagerOfEmployee = await _context.Divisions
                    .AnyAsync(d => d.TenantId == tenantId && d.ManagerId == userId && !d.IsDeleted &&
                                   (employee.DivisionId == d.Id ||
                                    _context.Tasks.Any(t => t.AssignedToUserId == dto.EmployeeId && t.Level != null && t.Level.Project != null && t.Level.Project.DivisionId == d.Id)));

                if (!isManagerOfEmployee)
                {
                    throw new UnauthorizedAccessException("Bu əməkdaş sizin rəhbərlik etdiyiniz şöbəyə aid deyil.");
                }
            }

            // 3. Eyni gün üçün mövcud qeydi axtar
            var existingRecord = await _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.EmployeeId == dto.EmployeeId && r.EvaluationDate == today && !r.IsDeleted);

            var totalScore = dto.JobDutiesScore + dto.DisciplineScore + dto.BonusScore;

            if (existingRecord != null)
            {
                // Cari gün bitənədək menecer öz qiymətini yeniləyə bilər
                existingRecord.JobDutiesScore = dto.JobDutiesScore;
                existingRecord.DisciplineScore = dto.DisciplineScore;
                existingRecord.BonusScore = dto.BonusScore;
                existingRecord.TotalScore = totalScore;
                existingRecord.DisciplinePenaltyReason = dto.DisciplineScore == -1 ? dto.DisciplinePenaltyReason : null;
                existingRecord.BonusReason = dto.BonusScore == 1 ? dto.BonusReason : null;
                existingRecord.Comments = dto.Comments;
                existingRecord.EvaluatorId = userId;
                existingRecord.DivisionId = employee.DivisionId ?? existingRecord.DivisionId;
                existingRecord.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return MapToDTO(existingRecord);
            }
            else
            {
                // Yeni qeyd
                var record = new DailyKpiRecord
                {
                    TenantId = tenantId,
                    EmployeeId = dto.EmployeeId,
                    EvaluatorId = userId,
                    DivisionId = employee.DivisionId,
                    EvaluationDate = today,
                    JobDutiesScore = dto.JobDutiesScore,
                    DisciplineScore = dto.DisciplineScore,
                    BonusScore = dto.BonusScore,
                    TotalScore = totalScore,
                    DisciplinePenaltyReason = dto.DisciplineScore == -1 ? dto.DisciplinePenaltyReason : null,
                    BonusReason = dto.BonusScore == 1 ? dto.BonusReason : null,
                    Comments = dto.Comments,
                    IsAdminEdited = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.DailyKpiRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                // İlişkili obyektləri yüklə
                await _context.Entry(record).Reference(r => r.Employee).LoadAsync();
                await _context.Entry(record).Reference(r => r.Evaluator).LoadAsync();
                if (record.DivisionId.HasValue)
                    await _context.Entry(record).Reference(r => r.Division).LoadAsync();

                return MapToDTO(record);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 3. ADMIN / HR KABİNETİ
        // ─────────────────────────────────────────────────────────────

        public async Task<DailyKpiDTO> UpdateDailyKpiAsync(Guid id, UpdateDailyKpiDTO dto)
        {
            var tenantId = GetRequiredTenantId();
            var userId = GetRequiredUserId();

            if (!IsAdminOrHr())
                throw new UnauthorizedAccessException("Yalnız Admin və ya HR əməkdaşları balları redaktə edə bilər.");

            if (string.IsNullOrWhiteSpace(dto.AdminEditReason))
                throw new ArgumentException("Admin tərəfindən bal dəyişdirildikdə redaktə səbəbi (AdminEditReason) mütləq qeyd edilməlidir.");

            if (dto.JobDutiesScore != 0 && dto.JobDutiesScore != 1)
                throw new ArgumentException("Vəzifə öhdəlikləri balı yalnız 0 və ya 1 ola bilər.");

            if (dto.DisciplineScore != 0 && dto.DisciplineScore != -1)
                throw new ArgumentException("İntizam balı yalnız 0 və ya -1 ola bilər.");

            if (dto.BonusScore != 0 && dto.BonusScore != 1)
                throw new ArgumentException("Bonus balı yalnız 0 və ya 1 ola bilər.");

            if (dto.DisciplineScore == -1 && string.IsNullOrWhiteSpace(dto.DisciplinePenaltyReason))
                throw new ArgumentException("İntizam qaydası pozulduqda (-1) cərimə səbəbi mütləq qeyd edilməlidir.");

            var record = await _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

            if (record == null)
                throw new KeyNotFoundException("KPI qeydi tapılmadı.");

            record.JobDutiesScore = dto.JobDutiesScore;
            record.DisciplineScore = dto.DisciplineScore;
            record.BonusScore = dto.BonusScore;
            record.TotalScore = dto.JobDutiesScore + dto.DisciplineScore + dto.BonusScore;
            record.DisciplinePenaltyReason = dto.DisciplineScore == -1 ? dto.DisciplinePenaltyReason : null;
            record.BonusReason = dto.BonusScore == 1 ? dto.BonusReason : null;
            record.Comments = dto.Comments;
            record.IsAdminEdited = true;
            record.AdminEditReason = dto.AdminEditReason;
            record.EvaluatorId = userId;
            record.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDTO(record);
        }

        public async Task<bool> DeleteDailyKpiAsync(Guid id)
        {
            var tenantId = GetRequiredTenantId();

            if (!IsAdminOrHr())
                throw new UnauthorizedAccessException("Yalnız Admin və ya HR əməkdaşları bal qeydini silə bilər.");

            var record = await _context.DailyKpiRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted);

            if (record == null)
                return false;

            record.IsDeleted = true;
            record.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CompanyKpiAnalyticsDTO> GetCompanyAnalyticsAsync(int? year = null, int? month = null, Guid? divisionId = null)
        {
            var tenantId = GetRequiredTenantId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            var query = _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && !r.IsDeleted &&
                            r.EvaluationDate.Year == targetYear && r.EvaluationDate.Month == targetMonth);

            if (divisionId.HasValue)
            {
                query = query.Where(r => r.DivisionId == divisionId.Value);
            }

            var records = await query.ToListAsync();

            var totalEvaluations = records.Count;
            var evaluatedEmployeesCount = records.Select(r => r.EmployeeId).Distinct().Count();
            var cumulativeScore = records.Sum(r => r.TotalScore);
            var averageScore = totalEvaluations > 0 ? Math.Round(records.Average(r => r.TotalScore), 2) : 0;

            // Top performers (Aylıq reytinq)
            var topPerformers = records
                .GroupBy(r => new { r.EmployeeId, r.Employee.FullName, r.Employee.UserName, r.Employee.Email, r.DivisionId, DivisionName = r.Division != null ? r.Division.Name : null })
                .Select(g => new KpiLeaderboardItemDTO
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.FullName ?? g.Key.UserName ?? g.Key.Email,
                    EmployeeEmail = g.Key.Email,
                    DivisionId = g.Key.DivisionId,
                    DivisionName = g.Key.DivisionName,
                    MonthlyCumulativeScore = g.Sum(x => x.TotalScore),
                    AverageDailyScore = Math.Round(g.Average(x => x.TotalScore), 2),
                    DaysEvaluated = g.Count(),
                    DutiesCompletedDays = g.Count(x => x.JobDutiesScore == 1),
                    DisciplineViolationsDays = g.Count(x => x.DisciplineScore == -1),
                    BonusDays = g.Count(x => x.BonusScore == 1)
                })
                .OrderByDescending(x => x.MonthlyCumulativeScore)
                .Take(5)
                .ToList();

            for (int i = 0; i < topPerformers.Count; i++)
            {
                topPerformers[i].Rank = i + 1;
            }

            // Şöbələr üzrə bölgü
            var allDivisions = await _context.Divisions
                .Include(d => d.Manager)
                .Where(d => d.TenantId == tenantId && !d.IsDeleted)
                .ToListAsync();

            var divisionBreakdown = new List<DivisionKpiAnalyticsDTO>();
            foreach (var div in allDivisions)
            {
                var divRecords = records.Where(r => r.DivisionId == div.Id).ToList();
                divisionBreakdown.Add(new DivisionKpiAnalyticsDTO
                {
                    DivisionId = div.Id,
                    DivisionName = div.Name,
                    ManagerName = div.Manager?.FullName ?? div.Manager?.UserName,
                    EmployeeCount = divRecords.Select(r => r.EmployeeId).Distinct().Count(),
                    CumulativeScore = divRecords.Sum(r => r.TotalScore),
                    AverageScore = divRecords.Count > 0 ? Math.Round(divRecords.Average(r => r.TotalScore), 2) : 0,
                    DisciplineViolationsCount = divRecords.Count(r => r.DisciplineScore == -1),
                    BonusCount = divRecords.Count(r => r.BonusScore == 1)
                });
            }

            // Günlük trend
            var dailyTrends = records
                .GroupBy(r => r.EvaluationDate)
                .OrderBy(g => g.Key)
                .Select(g => new DailyTrendDTO
                {
                    Date = g.Key,
                    AverageScore = Math.Round(g.Average(x => x.TotalScore), 2),
                    TotalEvaluations = g.Count(),
                    ViolationsCount = g.Count(x => x.DisciplineScore == -1),
                    BonusesCount = g.Count(x => x.BonusScore == 1)
                })
                .ToList();

            var violations = records
                .Where(r => r.DisciplineScore == -1)
                .OrderByDescending(r => r.EvaluationDate)
                .Take(10)
                .Select(MapToDTO)
                .ToList();

            return new CompanyKpiAnalyticsDTO
            {
                Year = targetYear,
                Month = targetMonth,
                TotalEvaluationsCount = totalEvaluations,
                EvaluatedEmployeesCount = evaluatedEmployeesCount,
                CompanyCumulativeScore = cumulativeScore,
                CompanyAverageDailyScore = averageScore,
                TotalDutiesCompletedCount = records.Count(r => r.JobDutiesScore == 1),
                TotalDisciplineViolationsCount = records.Count(r => r.DisciplineScore == -1),
                TotalBonusCount = records.Count(r => r.BonusScore == 1),
                TopPerformers = topPerformers,
                RecentDisciplineViolations = violations,
                DivisionBreakdown = divisionBreakdown,
                DailyTrends = dailyTrends
            };
        }

        public async Task<IEnumerable<DailyKpiDTO>> GetKpiReportAsync(KpiReportFilterDTO filter)
        {
            var tenantId = GetRequiredTenantId();

            var query = _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Evaluator)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && !r.IsDeleted);

            if (filter.StartDate.HasValue)
                query = query.Where(r => r.EvaluationDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(r => r.EvaluationDate <= filter.EndDate.Value);

            if (filter.DivisionId.HasValue)
                query = query.Where(r => r.DivisionId == filter.DivisionId.Value);

            if (filter.EmployeeId.HasValue)
                query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);

            if (filter.EvaluatorId.HasValue)
                query = query.Where(r => r.EvaluatorId == filter.EvaluatorId.Value);

            if (filter.HasDisciplineViolationOnly == true)
                query = query.Where(r => r.DisciplineScore == -1);

            if (filter.HasBonusOnly == true)
                query = query.Where(r => r.BonusScore == 1);

            if (filter.HasDutiesCompletedOnly == true)
                query = query.Where(r => r.JobDutiesScore == 1);

            var list = await query
                .OrderByDescending(r => r.EvaluationDate)
                .ToListAsync();

            return list.Select(MapToDTO);
        }

        // ─────────────────────────────────────────────────────────────
        // 4. AYLIK REYTİNQ (LEADERBOARD)
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<KpiLeaderboardItemDTO>> GetMonthlyLeaderboardAsync(int? year = null, int? month = null, Guid? divisionId = null)
        {
            var tenantId = GetRequiredTenantId();
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            var query = _context.DailyKpiRecords
                .Include(r => r.Employee)
                .Include(r => r.Division)
                .Where(r => r.TenantId == tenantId && !r.IsDeleted &&
                            r.EvaluationDate.Year == targetYear && r.EvaluationDate.Month == targetMonth);

            if (divisionId.HasValue)
            {
                query = query.Where(r => r.DivisionId == divisionId.Value);
            }

            var records = await query.ToListAsync();

            var leaderboard = records
                .GroupBy(r => new
                {
                    r.EmployeeId,
                    r.Employee.FullName,
                    r.Employee.UserName,
                    r.Employee.Email,
                    r.DivisionId,
                    DivisionName = r.Division != null ? r.Division.Name : null
                })
                .Select(g => new KpiLeaderboardItemDTO
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.FullName ?? g.Key.UserName ?? g.Key.Email,
                    EmployeeEmail = g.Key.Email,
                    DivisionId = g.Key.DivisionId,
                    DivisionName = g.Key.DivisionName,
                    MonthlyCumulativeScore = g.Sum(x => x.TotalScore),
                    AverageDailyScore = Math.Round(g.Average(x => x.TotalScore), 2),
                    DaysEvaluated = g.Count(),
                    DutiesCompletedDays = g.Count(x => x.JobDutiesScore == 1),
                    DisciplineViolationsDays = g.Count(x => x.DisciplineScore == -1),
                    BonusDays = g.Count(x => x.BonusScore == 1)
                })
                .OrderByDescending(x => x.MonthlyCumulativeScore)
                .ThenByDescending(x => x.AverageDailyScore)
                .ToList();

            for (int i = 0; i < leaderboard.Count; i++)
            {
                leaderboard[i].Rank = i + 1;
            }

            return leaderboard;
        }

        // ─────────────────────────────────────────────────────────────
        // KÖMƏKÇİ METODLAR
        // ─────────────────────────────────────────────────────────────

        private async Task<List<AppUser>> GetSubordinatesListAsync(Guid tenantId, Guid managerId)
        {
            if (IsAdminOrHr())
            {
                return await _context.AppUsers
                    .Include(u => u.Division)
                    .Where(u => u.TenantId == tenantId)
                    .OrderBy(u => u.FullName ?? u.UserName)
                    .ToListAsync();
            }

            // Menecerin idarə etdiyi şöbələri tap
            var managedDivisions = await _context.Divisions
                .Where(d => d.TenantId == tenantId && d.ManagerId == managerId && !d.IsDeleted)
                .Select(d => d.Id)
                .ToListAsync();

            if (managedDivisions.Count == 0)
                return new List<AppUser>();

            // Həmin şöbədə olan və ya həmin şöbənin layihələrində tapşırığı olan istifadəçilər
            return await _context.AppUsers
                .Include(u => u.Division)
                .Where(u => u.TenantId == tenantId && u.Id != managerId &&
                            (u.DivisionId.HasValue && managedDivisions.Contains(u.DivisionId.Value) ||
                             _context.Tasks.Any(t => t.AssignedToUserId == u.Id && t.Level != null && t.Level.Project != null && managedDivisions.Contains(t.Level.Project.DivisionId ?? Guid.Empty))))
                .Distinct()
                .OrderBy(u => u.FullName ?? u.UserName)
                .ToListAsync();
        }

        private static DailyKpiDTO MapToDTO(DailyKpiRecord r)
        {
            return new DailyKpiDTO
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee?.FullName ?? r.Employee?.UserName ?? r.Employee?.Email,
                EmployeeEmail = r.Employee?.Email,
                DivisionId = r.DivisionId,
                DivisionName = r.Division?.Name,
                EvaluatorId = r.EvaluatorId,
                EvaluatorName = r.Evaluator?.FullName ?? r.Evaluator?.UserName ?? r.Evaluator?.Email,
                EvaluationDate = r.EvaluationDate,
                JobDutiesScore = r.JobDutiesScore,
                DisciplineScore = r.DisciplineScore,
                BonusScore = r.BonusScore,
                TotalScore = r.TotalScore,
                DisciplinePenaltyReason = r.DisciplinePenaltyReason,
                BonusReason = r.BonusReason,
                Comments = r.Comments,
                IsAdminEdited = r.IsAdminEdited,
                AdminEditReason = r.AdminEditReason,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            };
        }
    }
}
