using System;

namespace Contract.DTOs.Kpi
{
    /// <summary>
    /// Admin / HR üçün hesabatların çıxarılması və cədvəl filtrləmə modeli.
    /// </summary>
    public class KpiReportFilterDTO
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? EvaluatorId { get; set; }

        // Yalnız intizam pozuntusu (-1) olanları göstər
        public bool? HasDisciplineViolationOnly { get; set; }

        // Yalnız bonus (+1) alanları göstər
        public bool? HasBonusOnly { get; set; }

        // Yalnız vəzifə öhdəliyini yerinə yetirənləri (+1) göstər
        public bool? HasDutiesCompletedOnly { get; set; }
    }
}
