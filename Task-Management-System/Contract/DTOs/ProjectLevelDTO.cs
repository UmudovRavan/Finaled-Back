using System;
using System.Collections.Generic;

namespace Contract.DTOs
{
    public class ProjectLevelDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Order { get; set; }
        public Guid ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int TaskCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TaskDTO>? Tasks { get; set; }
    }

    public class CreateProjectLevelDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Order { get; set; } = 0;
        public Guid ProjectId { get; set; }
    }

    public class UpdateProjectLevelDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int Order { get; set; }
    }
}
