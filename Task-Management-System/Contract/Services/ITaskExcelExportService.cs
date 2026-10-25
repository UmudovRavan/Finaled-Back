using Contract.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface ITaskExcelExportService
    {
        /// <summary>
        /// Filtrlənmiş tapşırıqları korporativ stillərlə bəzədilmiş Excel (.xlsx) faylına ixrac edir.
        /// </summary>
        Task<byte[]> ExportTasksToExcelAsync(TaskFilterDTO filter);

        /// <summary>
        /// Verilmiş tapşırıqlar siyahısını birbaşa Excel (.xlsx) baytlarına çevirir.
        /// </summary>
        byte[] GenerateExcelBytes(IEnumerable<TaskDTO> tasks, string? titleContext = null, string? divisionContext = null, string? projectContext = null);
    }
}
