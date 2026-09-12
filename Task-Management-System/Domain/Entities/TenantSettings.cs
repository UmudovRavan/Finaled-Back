using System;

namespace Domain.Entities
{
    public class TenantSettings : BaseEntity
    {
        public string? OverdueTaskNotificationEmail { get; set; }
        public bool IsOverdueNotificationEnabled { get; set; } = true;
    }
}
