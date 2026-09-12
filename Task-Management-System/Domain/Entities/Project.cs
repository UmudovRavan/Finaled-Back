using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public Guid? DivisionId { get; set; }
        public Division? Division { get; set; }

        public Guid ManagerId { get; set; }
        public AppUser? Manager { get; set; }

        public ICollection<ProjectLevel> Levels { get; set; } = new List<ProjectLevel>();
    }
}
