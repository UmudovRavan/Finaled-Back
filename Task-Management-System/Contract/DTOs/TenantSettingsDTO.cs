using System;

namespace Contract.DTOs
{
    public class TenantSettingsDTO
    {
        public Guid Id { get; set; }
        public string? OverdueTaskNotificationEmail { get; set; }
        public bool IsOverdueNotificationEnabled { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateTenantSettingsDTO
    {
        public string? OverdueTaskNotificationEmail { get; set; }
        public bool IsOverdueNotificationEnabled { get; set; } = true;
    }
}
