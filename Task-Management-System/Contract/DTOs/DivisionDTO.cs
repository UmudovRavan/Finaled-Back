using System;
using System.Collections.Generic;

namespace Contract.DTOs
{
    public class DivisionDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public int ProjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProjectDTO>? Projects { get; set; }
    }

    public class CreateDivisionDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid ManagerId { get; set; }
    }

    public class UpdateDivisionDTO
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid ManagerId { get; set; }
    }
}
