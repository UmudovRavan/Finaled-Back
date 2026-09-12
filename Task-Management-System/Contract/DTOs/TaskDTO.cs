using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Contract.DTOs
{
    public class TaskDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DifficultyLevel Difficulty { get; set; }
        public Priority Priority { get; set; } = Priority.Normal;
        public CurrentSituation Status { get; set; }
        public DateTime Deadline { get; set; }
        public Guid? WorkGroupId { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string CreatedByUserId { get; set; } = default!;

        public Guid? LevelId { get; set; }
        public string? LevelName { get; set; }
        public string? ProjectName { get; set; }
        public string? DivisionName { get; set; }

        public Guid? ParentTaskId { get; set; }
        public List<Guid>? TaskCommentId { get; set; }
        public List<TaskCommentDTO>? TaskComments { get; set; }
        public List<FileDto>? Files { get; set; }
    }
}
