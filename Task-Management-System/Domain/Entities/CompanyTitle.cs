using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    /// <summary>
    /// Tapşırıq və layihələrin aid olduğu hüquqi şəxs, holdinq və ya sifarişçi müəssisə (Title - məs. A Holdinq, B Holdinq, X MMC).
    /// </summary>
    public class CompanyTitle : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? Voen { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
