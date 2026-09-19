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
using System.Collections.Concurrent;
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

        // Hər tenant üçün təkrar bildirişin spam olaraq saatbasaat getməsinin qarşısını alan lüğət
        private static readonly ConcurrentDictionary<Guid, DateTime> _lastNotificationSentAt = new();

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

            // Sistem qalxanda ilk dövr üçün 30 saniyə gözləmə
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

                // Default yoxlama intervalı: 60 dəqiqə
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

            // Bildiriş aktiv olan və email təyin edilmiş tenant ayarları (AsNoTracking ilə)
            var tenantSettingsList = await context.TenantSettings
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.IsOverdueNotificationEnabled && !string.IsNullOrEmpty(s.OverdueTaskNotificationEmail))
                .ToListAsync(stoppingToken);

            var minHours = _configuration.GetValue<int>("OverdueEmail:MinHoursBetweenEmails", 20);
            if (minHours <= 0) minHours = 20;

            foreach (var settings in tenantSettingsList)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                // Spam qoruması: eyni tenant-a son minHours (məsələn 20 saat) ərzində artıq mail göndərilibsə, ötür
                if (_lastNotificationSentAt.TryGetValue(settings.TenantId, out var lastSentTime))
                {
                    var hoursPassed = (now - lastSentTime).TotalHours;
                    if (hoursPassed < minHours)
                    {
                        _logger.LogDebug("Tenant {TenantId} üçün son bildiriş {Hours:F1} saat əvvəl göndərilib. Minimum fasilə {MinHours} saatdır, ötürülür.",
                            settings.TenantId, hoursPassed, minHours);
                        continue;
                    }
                }

                try
                {
                    // AsNoTracking ilə gecikmiş aktiv tapşırıqların çəkilməsi
                    var overdueTasks = await context.Tasks
                        .AsNoTracking()
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

                    var overdueItems = overdueTasks.Select(t =>
                    {
                        var diff = now - t.Deadline;
                        var totalHours = (int)diff.TotalHours;
                        var totalDays = (int)diff.TotalDays;

                        string durationText;
                        if (totalDays >= 1)
                        {
                            var remHours = totalHours % 24;
                            durationText = remHours > 0 ? $"{totalDays} gün {remHours} saat" : $"{totalDays} gün";
                        }
                        else
                        {
                            durationText = $"{Math.Max(1, totalHours)} saat";
                        }

                        return new OverdueTaskEmailItem
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
                            DaysOverdue = Math.Max(0, totalDays),
                            OverdueDuration = durationText
                        };
                    }).ToList();

                    _logger.LogInformation("Sending overdue email for tenant {TenantId} to {Email} ({Count} tasks).",
                        settings.TenantId, settings.OverdueTaskNotificationEmail, overdueItems.Count);

                    await emailSender.SendOverdueTasksEmailAsync(settings.OverdueTaskNotificationEmail!, overdueItems);

                    // Bildiriş uğurla göndərildi, son göndəriş vaxtını yenilə
                    _lastNotificationSentAt[settings.TenantId] = now;
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
