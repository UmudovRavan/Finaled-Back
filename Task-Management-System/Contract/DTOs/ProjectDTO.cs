using System;
using System.Collections.Generic;

namespace Contract.DTOs
{
    public class ProjectDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid? DivisionId { get; set; }
        public string? DivisionName { get; set; }
        public Guid ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public int LevelCount { get; set; }
        public int TaskCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProjectLevelDTO>? Levels { get; set; }
    }

    public class CreateProjectDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid ManagerId { get; set; }
    }

    public class UpdateProjectDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid? DivisionId { get; set; }
        public Guid ManagerId { get; set; }
    }
}
