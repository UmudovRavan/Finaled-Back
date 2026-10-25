using Domain.Enums;
using System;

namespace Contract.DTOs
{
    /// <summary>
    /// Üç analitik bölgü (Division, Project, Title) və status üzrə müstəqil filtrləmə modeli.
    /// </summary>
    public class TaskFilterDTO
    {
        // 1. Title (Holdinq / Şirkət / MMC - Məsələn: A Holdinq, X MMC)
        public Guid? TitleId { get; set; }

        // 2. Division (Departament / Bölmə - Məsələn: Satış Departamenti)
        public Guid? DivisionId { get; set; }

        // 3. Project (Layihə - Məsələn: ERP Tətbiqi. Layihəsiz seçim aradan qaldırılıb)
        public Guid? ProjectId { get; set; }

        // Əlavə filtrlər
        public CurrentSituation? Status { get; set; }
        public Priority? Priority { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? FromDeadline { get; set; }
        public DateTime? ToDeadline { get; set; }
        public string? Search { get; set; }
    }
}
