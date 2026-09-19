using System;

namespace Contract.DTOs
{
    public class OverdueTaskEmailItem
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = default!;
        public string? AssignedToUserName { get; set; }
        public string? AssignedToEmail { get; set; }
        public string? DivisionName { get; set; }
        public string? ProjectName { get; set; }
        public string? LevelName { get; set; }
        public string Priority { get; set; } = "Normal";
        public DateTime Deadline { get; set; }
        public int DaysOverdue { get; set; }
        public string OverdueDuration { get; set; } = string.Empty;
    }
}
