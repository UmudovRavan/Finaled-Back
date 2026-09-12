using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.BackgroundJobs
{
    public class OverdueTaskEmailJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueTaskEmailJob> _logger;
        private readonly IConfiguration _configuration;

        private static readonly CurrentSituation[] ActiveStatuses = new[]
        {
            CurrentSituation.Pending,
            CurrentSituation.Assigned,
            CurrentSituation.InProgress,
            CurrentSituation.UnderReview
        };

        public OverdueTaskEmailJob(
            IServiceScopeFactory scopeFactory,
            ILogger<OverdueTaskEmailJob> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverdueTaskEmailJob started.");

            // Initial short delay on startup before first run
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendOverdueTaskEmailsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing OverdueTaskEmailJob.");
                }

                // Default interval: 60 minutes
                var intervalMinutes = _configuration.GetValue<int>("OverdueEmail:IntervalMinutes", 60);
                if (intervalMinutes <= 0) intervalMinutes = 60;

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }

            _logger.LogInformation("OverdueTaskEmailJob is stopping.");
        }

        private async Task CheckAndSendOverdueTaskEmailsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var now = DateTime.UtcNow;

            // Get all tenant settings that have overdue notification enabled and email set
            var tenantSettingsList = await context.TenantSettings
                .Where(s => !s.IsDeleted && s.IsOverdueNotificationEnabled && !string.IsNullOrEmpty(s.OverdueTaskNotificationEmail))
                .ToListAsync(stoppingToken);

            foreach (var settings in tenantSettingsList)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var overdueTasks = await context.Tasks
                        .Where(t => t.TenantId == settings.TenantId &&
                                    !t.IsDeleted &&
                                    ActiveStatuses.Contains(t.Status) &&
                                    t.Deadline < now)
                        .Include(t => t.AssignedToUser)
                        .Include(t => t.Level)
                            .ThenInclude(l => l!.Project)
                                .ThenInclude(p => p.Division)
                        .OrderBy(t => t.Deadline)
                        .ToListAsync(stoppingToken);

                    if (overdueTasks.Count == 0)
                        continue;

                    var overdueItems = overdueTasks.Select(t => new OverdueTaskEmailItem
                    {
                        TaskId = t.Id,
                        Title = t.Title,
                        AssignedToUserName = t.AssignedToUser != null ? (t.AssignedToUser.FullName ?? t.AssignedToUser.UserName) : null,
                        AssignedToEmail = t.AssignedToUser?.Email,
                        DivisionName = t.Level?.Project?.Division?.Name,
                        ProjectName = t.Level?.Project?.Name,
                        LevelName = t.Level?.Name,
                        Priority = t.Priority.ToString(),
                        Deadline = t.Deadline,
                        DaysOverdue = Math.Max(1, (int)(now - t.Deadline).TotalDays)
                    }).ToList();

                    _logger.LogInformation("Sending overdue email for tenant {TenantId} to {Email} ({Count} tasks).",
                        settings.TenantId, settings.OverdueTaskNotificationEmail, overdueItems.Count);

                    await emailSender.SendOverdueTasksEmailAsync(settings.OverdueTaskNotificationEmail!, overdueItems);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send overdue email for tenant {TenantId} to {Email}.",
                        settings.TenantId, settings.OverdueTaskNotificationEmail);
                }
            }
        }
    }
}
