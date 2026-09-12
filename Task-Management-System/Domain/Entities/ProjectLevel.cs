using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class ProjectLevel : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Order { get; set; } = 0;

        public Guid ProjectId { get; set; }
        public Project? Project { get; set; }

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
