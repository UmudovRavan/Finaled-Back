using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Persistence.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskTransaction> TaskTransactions { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskCommentMention> TaskCommentMentions { get; set; }
        public DbSet<DailyKpiRecord> DailyKpiRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<WorkGroup> WorkGroups { get; set; }
        public DbSet<PasswordResetOTP> PasswordResetOtps { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectLevel> ProjectLevels { get; set; }
        public DbSet<TenantSettings> TenantSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= AppUser =================
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedNever(); // Auth Service-dən gəlir
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.FullName).HasMaxLength(256);
                entity.Property(u => u.UserName).HasMaxLength(128);
                entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            });

            // ================= TaskItem =================
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasOne(t => t.AssignedToUser)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(t => t.AssignedToUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedByUser)
                      .WithMany(u => u.CreatedTasks)
                      .HasForeignKey(t => t.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<TaskItem>()
                      .WithMany()
                      .HasForeignKey(t => t.ParentTaskId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedWorkGroup)
                      .WithMany(w => w.Tasks)
                      .HasForeignKey(t => t.AssignedWorkGroupId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ================= TaskComment =================
            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasOne(c => c.TaskItem)
                      .WithMany(t => t.TaskComments)
                      .HasForeignKey(c => c.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                      .WithMany(u => u.TaskComments)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= TaskCommentMention =================
            modelBuilder.Entity<TaskCommentMention>(entity =>
            {
                entity.HasOne(m => m.TaskComment)
                      .WithMany(c => c.TaskCommentMentions)
                      .HasForeignKey(m => m.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.MentionedUser)
                      .WithMany(u => u.TaskCommentMentions)
                      .HasForeignKey(m => m.MentionedUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= TaskTransaction =================
            modelBuilder.Entity<TaskTransaction>(entity =>
            {
                entity.HasOne(t => t.TaskItem)
                      .WithMany()
                      .HasForeignKey(t => t.TaskItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.FromUser)
                      .WithMany()
                      .HasForeignKey(t => t.FromUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ToUser)
                      .WithMany()
                      .HasForeignKey(t => t.ToUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= DailyKpiRecord =================
            modelBuilder.Entity<DailyKpiRecord>(entity =>
            {
                entity.HasOne(k => k.Employee)
                      .WithMany(u => u.DailyKpiRecords)
                      .HasForeignKey(k => k.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(k => k.Evaluator)
                      .WithMany(u => u.EvaluatedKpiRecords)
                      .HasForeignKey(k => k.EvaluatorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(k => k.Division)
                      .WithMany(d => d.DailyKpiRecords)
                      .HasForeignKey(k => k.DivisionId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(k => new { k.TenantId, k.EmployeeId, k.EvaluationDate })
                      .IsUnique();

                entity.HasIndex(k => new { k.TenantId, k.EvaluationDate });
            });

            // AppUser → Division
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Division)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DivisionId)
                .OnDelete(DeleteBehavior.SetNull);

            // ================= Notification =================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ================= WorkGroup =================
            // AppUser → WorkGroup (member)
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.WorkGroup)
                .WithMany(w => w.Users)
                .HasForeignKey(u => u.WorkGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkGroup → Leader
            modelBuilder.Entity<WorkGroup>()
                .HasOne(w => w.Leader)
                .WithMany()
                .HasForeignKey(w => w.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================= Division =================
            modelBuilder.Entity<Division>(entity =>
            {
                entity.Property(d => d.Name).IsRequired().HasMaxLength(256);
                entity.HasOne(d => d.Manager)
                      .WithMany(u => u.ManagedDivisions)
                      .HasForeignKey(d => d.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Projects)
                      .WithOne(p => p.Division)
                      .HasForeignKey(p => p.DivisionId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ================= Project =================
            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(p => p.Name).IsRequired().HasMaxLength(256);
                entity.HasOne(p => p.Manager)
                      .WithMany(u => u.ManagedProjects)
                      .HasForeignKey(p => p.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.Levels)
                      .WithOne(l => l.Project)
                      .HasForeignKey(l => l.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ================= ProjectLevel =================
            modelBuilder.Entity<ProjectLevel>(entity =>
            {
                entity.Property(l => l.Name).IsRequired().HasMaxLength(256);
                entity.HasMany(l => l.Tasks)
                      .WithOne(t => t.Level)
                      .HasForeignKey(t => t.LevelId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ================= TenantSettings =================
            modelBuilder.Entity<TenantSettings>(entity =>
            {
                entity.HasIndex(s => s.TenantId);
            });
        }
    }
}
