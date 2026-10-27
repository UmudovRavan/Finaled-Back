using ClosedXML.Excel;
using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TaskExcelExportService : ITaskExcelExportService
    {
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IGenericRepository<CompanyTitle> _titleRepo;
        private readonly IGenericRepository<Division> _divisionRepo;
        private readonly IGenericRepository<Project> _projectRepo;

        public TaskExcelExportService(
            IGenericRepository<TaskItem> taskRepo,
            IGenericRepository<CompanyTitle> titleRepo,
            IGenericRepository<Division> divisionRepo,
            IGenericRepository<Project> projectRepo)
        {
            _taskRepo = taskRepo;
            _titleRepo = titleRepo;
            _divisionRepo = divisionRepo;
            _projectRepo = projectRepo;
        }

        public async Task<byte[]> ExportTasksToExcelAsync(TaskFilterDTO filter)
        {
            var tasks = await _taskRepo.GetAllAsync(query =>
            {
                query = query
                    .Include(t => t.TitleCompany)
                    .Include(t => t.Division)
                    .Include(t => t.Project)
                    .Include(t => t.Level)
                    .Include(t => t.AssignedToUser);

                // Müstəqil filtrləmə (1. Title, 2. Division, 3. Project)
                if (filter.TitleId.HasValue && filter.TitleId.Value != Guid.Empty)
                {
                    query = query.Where(t => t.TitleId == filter.TitleId.Value ||
                                            (t.Project != null && t.Project.TitleId == filter.TitleId.Value));
                }

                if (filter.DivisionId.HasValue && filter.DivisionId.Value != Guid.Empty)
                {
                    query = query.Where(t => t.DivisionId == filter.DivisionId.Value ||
                                            (t.Project != null && t.Project.DivisionId == filter.DivisionId.Value));
                }

                if (filter.ProjectId.HasValue && filter.ProjectId.Value != Guid.Empty)
                {
                    query = query.Where(t => t.ProjectId == filter.ProjectId.Value ||
                                            (t.Level != null && t.Level.ProjectId == filter.ProjectId.Value));
                }

                if (filter.Status.HasValue)
                {
                    query = query.Where(t => t.Status == filter.Status.Value);
                }

                if (filter.Priority.HasValue)
                {
                    query = query.Where(t => t.Priority == filter.Priority.Value);
                }

                if (filter.AssignedToUserId.HasValue && filter.AssignedToUserId.Value != Guid.Empty)
                {
                    query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
                }

                if (filter.FromDeadline.HasValue)
                {
                    query = query.Where(t => t.Deadline >= filter.FromDeadline.Value);
                }

                if (filter.ToDeadline.HasValue)
                {
                    query = query.Where(t => t.Deadline <= filter.ToDeadline.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var s = filter.Search.Trim().ToLower();
                    query = query.Where(t => t.Title.ToLower().Contains(s) || (t.Description != null && t.Description.ToLower().Contains(s)));
                }

                return query.OrderByDescending(t => t.CreatedAt);
            });

            // Filtrlənmiş kontekst adlarını tapaq (başlıq üçün)
            string? titleName = null;
            if (filter.TitleId.HasValue && filter.TitleId.Value != Guid.Empty)
            {
                var title = await _titleRepo.GetByIdAsync(filter.TitleId.Value);
                titleName = title?.Name;
            }

            string? divisionName = null;
            if (filter.DivisionId.HasValue && filter.DivisionId.Value != Guid.Empty)
            {
                var div = await _divisionRepo.GetByIdAsync(filter.DivisionId.Value);
                divisionName = div?.Name;
            }

            string? projectName = null;
            if (filter.ProjectId.HasValue && filter.ProjectId.Value != Guid.Empty)
            {
                var prj = await _projectRepo.GetByIdAsync(filter.ProjectId.Value);
                projectName = prj?.Name;
            }

            return GenerateExcelFromEntities(tasks, titleName, divisionName, projectName);
        }

        public byte[] GenerateExcelBytes(IEnumerable<TaskDTO> tasks, string? titleContext = null, string? divisionContext = null, string? projectContext = null)
        {
            var taskList = tasks.ToList();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Əməliyyatlar");

            BuildExcelLayout(ws, taskList.Count, titleContext, divisionContext, projectContext);

            int row = 5;
            int index = 1;
            foreach (var t in taskList)
            {
                ws.Cell(row, 1).Value = index++;
                ws.Cell(row, 2).Value = t.TitleName ?? "-";
                ws.Cell(row, 3).Value = t.DivisionName ?? "-";
                ws.Cell(row, 4).Value = t.ProjectName ?? "-";
                ws.Cell(row, 5).Value = t.LevelName ?? "-";
                ws.Cell(row, 6).Value = t.Title;
                ws.Cell(row, 7).Value = t.Description ?? string.Empty;
                ws.Cell(row, 8).Value = t.AssignedToUserName ?? "Təyin edilməyib";
                ws.Cell(row, 9).Value = FormatPriority(t.Priority);
                ws.Cell(row, 10).Value = FormatDifficulty(t.Difficulty);
                ws.Cell(row, 11).Value = FormatStatus(t.Status);
                ws.Cell(row, 12).Value = t.Deadline.ToString("dd.MM.yyyy HH:mm");

                // Sətir xətləri
                var rowRange = ws.Range(row, 1, row, 12);
                rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
                if (index % 2 == 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                row++;
            }

            SafelyAdjustColumns(ws, 12);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private byte[] GenerateExcelFromEntities(List<TaskItem> tasks, string? titleName, string? divisionName, string? projectName)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Əməliyyatlar və Tapşırıqlar");

            BuildExcelLayout(ws, tasks.Count, titleName, divisionName, projectName);

            int row = 5;
            int index = 1;
            foreach (var t in tasks)
            {
                var titleText = t.TitleCompany?.Name ?? t.Project?.TitleCompany?.Name ?? "-";
                var divisionText = t.Division?.Name ?? t.Project?.Division?.Name ?? "-";
                var projectText = t.Project?.Name ?? t.Level?.Project?.Name ?? "-";
                var levelText = t.Level?.Name ?? "-";
                var assignedUser = t.AssignedToUser != null
                    ? (t.AssignedToUser.FullName ?? t.AssignedToUser.UserName ?? t.AssignedToUser.Email)
                    : "Təyin edilməyib";

                ws.Cell(row, 1).Value = index++;
                ws.Cell(row, 2).Value = titleText;
                ws.Cell(row, 3).Value = divisionText;
                ws.Cell(row, 4).Value = projectText;
                ws.Cell(row, 5).Value = levelText;
                ws.Cell(row, 6).Value = t.Title;
                ws.Cell(row, 7).Value = t.Description ?? string.Empty;
                ws.Cell(row, 8).Value = assignedUser;
                ws.Cell(row, 9).Value = FormatPriority(t.Priority);
                ws.Cell(row, 10).Value = FormatDifficulty(t.Difficulty);
                ws.Cell(row, 11).Value = FormatStatus(t.Status);
                ws.Cell(row, 12).Value = t.Deadline.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 13).Value = t.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                var rowRange = ws.Range(row, 1, row, 13);
                rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
                if (index % 2 == 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                row++;
            }

            SafelyAdjustColumns(ws, 13);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private void BuildExcelLayout(IXLWorksheet ws, int count, string? titleContext, string? divisionContext, string? projectContext)
        {
            // Şirkət və Hesabat Başlığı
            ws.Cell(1, 1).Value = "BMG INTERNATIONAL — TAPŞIRIQ VƏ ƏMƏLİYYATLAR HESABATI";
            ws.Range(1, 1, 1, 13).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 32;

            // Filtr Məlumatları Zolağı
            var filterInfo = $"Tarix: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC | " +
                             $"Title (Holdinq/Şirkət): {(string.IsNullOrEmpty(titleContext) ? "Bütün Holdinqlər" : titleContext)} | " +
                             $"Division (Bölmə): {(string.IsNullOrEmpty(divisionContext) ? "Bütün Bölmələr" : divisionContext)} | " +
                             $"Project (Layihə): {(string.IsNullOrEmpty(projectContext) ? "Bütün Layihələr" : projectContext)} | " +
                             $"Cəmi Əməliyyat Sayı: {count}";

            ws.Cell(2, 1).Value = filterInfo;
            ws.Range(2, 1, 2, 13).Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");
            ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Row(2).Height = 22;

            ws.Row(3).Height = 8; // Boş aralıq

            // Cədvəl Sütun Başlıqları
            var headers = new[]
            {
                "№",
                "Title (Holdinq / Şirkət)",
                "Division (Bölmə)",
                "Project (Layihə)",
                "Mərhələ (Level)",
                "Tapşırıq Adı",
                "Təsvir",
                "İcraçı Əməkdaş",
                "Prioritet",
                "Çətinlik",
                "Status",
                "Son Tarix (Deadline)",
                "Yaradılma Tarixi"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(4, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 11;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(4).Height = 26;
        }

        private static string FormatStatus(CurrentSituation status) => status switch
        {
            CurrentSituation.Pending => "Gözləmədə",
            CurrentSituation.Assigned => "Təyin olunub",
            CurrentSituation.InProgress => "İcrada",
            CurrentSituation.UnderReview => "Yoxlamada",
            CurrentSituation.Completed => "Tamamlandı",
            CurrentSituation.Expired => "Vaxtı bitib",
            _ => status.ToString()
        };

        private static string FormatPriority(Priority priority) => priority switch
        {
            Priority.Low => "Aşağı",
            Priority.Normal => "Normal",
            Priority.Urgent => "Təcili",
            _ => priority.ToString()
        };

        private static string FormatDifficulty(DifficultyLevel difficulty) => difficulty switch
        {
            DifficultyLevel.Easy => "Asan",
            DifficultyLevel.Medium => "Orta",
            DifficultyLevel.Hard => "Çətin",
            _ => difficulty.ToString()
        };

        private static void SafelyAdjustColumns(IXLWorksheet ws, int columnCount)
        {
            try
            {
                ws.Columns().AdjustToContents(10.0, 50.0);
            }
            catch
            {
                // Headless Linux serverlərdə font tapılmadıqda sistem çökməsin, standart en tətbiq edirik
                for (int c = 1; c <= columnCount; c++)
                {
                    ws.Column(c).Width = c switch
                    {
                        1 => 6,   // №
                        2 => 22,  // Title
                        3 => 20,  // Division
                        4 => 22,  // Layihə
                        5 => 18,  // Mərhələ
                        6 => 30,  // Tapşırıq
                        7 => 35,  // Təsvir
                        8 => 22,  // Məsul Şəxs
                        9 => 14,  // Prioritet
                        10 => 14, // Çətinlik
                        11 => 16, // Status
                        12 => 18, // Son Tarix
                        13 => 18, // Yaradılma Tarixi
                        _ => 18
                    };
                }
            }
        }
    }
}
