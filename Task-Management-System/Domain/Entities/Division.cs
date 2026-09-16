using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Division : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid ManagerId { get; set; }
        public AppUser? Manager { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<DailyKpiRecord> DailyKpiRecords { get; set; } = new List<DailyKpiRecord>();
    }
}
