using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DifficultyLevel Difficulty { get; set; }
        public Priority Priority { get; set; } = Priority.Normal;
        public CurrentSituation Status { get; set; }
        public DateTime Deadline { get; set; }

        public Guid? AssignedToUserId { get; set; }
        public Guid CreatedByUserId { get; set; }

        public AppUser? AssignedToUser { get; set; }
        public AppUser CreatedByUser { get; set; } = default!;

        public Guid? LevelId { get; set; }
        public ProjectLevel? Level { get; set; }

        public Guid? AssignedWorkGroupId { get; set; }
        public WorkGroup? AssignedWorkGroup { get; set; }

        public Guid? ParentTaskId { get; set; }

        public List<TaskComment>? TaskComments { get; set; }
        public List<TaskAttachment>? Attachments { get; set; }
    }
}
