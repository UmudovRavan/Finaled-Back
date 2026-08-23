using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorkGroupService : IWorkGroupService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly IGenericService<WorkGroupDTO, WorkGroup> _groupWorkservice;
        private readonly IGenericService<TaskDTO, TaskItem> _genericService;
        private readonly INotificationService _notification;
        private readonly IGenericRepository<TaskTransaction> _transaction;

        public WorkGroupService(
            AppDbContext context,
            ICurrentTenantService tenantService,
            IGenericService<WorkGroupDTO, WorkGroup> groupWorkservice,
            IGenericService<TaskDTO, TaskItem> genericService,
            INotificationService notification,
            IGenericRepository<TaskTransaction> transaction)
        {
            _context = context;
            _tenantService = tenantService;
            _groupWorkservice = groupWorkservice;
            _genericService = genericService;
            _notification = notification;
            _transaction = transaction;
        }

        private Guid GetRequiredTenantId()
        {
            return _tenantService.TenantId
                ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
        }

        public async Task<IEnumerable<WorkGroupDTO>> GetAllWorkGroupsAsync()
        {
            var data = await _groupWorkservice.GetAllAsync(q => q
                .Include(w => w.Leader)
                .Include(w => w.Users)
                .Include(w => w.Tasks)
            );
            return data;
        }

        public async Task<WorkGroupDTO> GetWorkGroupByIdAsync(Guid id)
        {
            return await _groupWorkservice.GetByIdAsync(id, q => q
                .Include(w => w.Leader)
                .Include(w => w.Users)
                .Include(w => w.Tasks));
        }

        public async Task<WorkGroupDTO> CreateWorkGroupAsync(WorkGroupDTO workGroup)
        {
            var tenantId = GetRequiredTenantId();

            if (string.IsNullOrWhiteSpace(workGroup.LeaderId) || !Guid.TryParse(workGroup.LeaderId, out var leaderGuid))
                throw new Exception("Qrup rəhbərinin (Leader) ID-si yanlışdır.");

            var leader = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == leaderGuid && u.TenantId == tenantId);
            if (leader == null)
                throw new Exception("Seçilmiş rəhbər bazada tapılmadı.");

            var entity = new WorkGroup
            {
                Name = workGroup.Name.Trim(),
                LeaderId = leaderGuid,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (workGroup.UserIds != null && workGroup.UserIds.Count > 0)
            {
                var memberGuids = workGroup.UserIds
                    .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty && g != leaderGuid)
                    .ToList();

                var members = await _context.AppUsers
                    .Where(u => memberGuids.Contains(u.Id) && u.TenantId == tenantId)
                    .ToListAsync();

                foreach (var member in members)
                {
                    member.WorkGroupId = entity.Id;
                    entity.Users.Add(member);
                }
            }

            await _context.WorkGroups.AddAsync(entity);
            await _context.SaveChangesAsync();

            return await GetWorkGroupByIdAsync(entity.Id);
        }

        public async Task UpdateWorkGroupAsync(WorkGroupDTO workGroup)
        {
            var tenantId = GetRequiredTenantId();
            var existingGroup = await _context.WorkGroups
                .Include(w => w.Users)
                .FirstOrDefaultAsync(w => w.Id == workGroup.Id && w.TenantId == tenantId && !w.IsDeleted);

            if (existingGroup == null)
                throw new Exception("İş qrupu tapılmadı.");

            if (!string.IsNullOrWhiteSpace(workGroup.Name))
                existingGroup.Name = workGroup.Name.Trim();

            if (!string.IsNullOrWhiteSpace(workGroup.LeaderId) && Guid.TryParse(workGroup.LeaderId, out var leaderGuid))
            {
                var leader = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == leaderGuid && u.TenantId == tenantId);
                if (leader == null)
                    throw new Exception("Seçilmiş rəhbər tapılmadı.");
                existingGroup.LeaderId = leaderGuid;
            }

            if (workGroup.UserIds != null)
            {
                var targetGuids = workGroup.UserIds
                    .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();

                foreach (var user in existingGroup.Users.ToList())
                {
                    if (!targetGuids.Contains(user.Id))
                    {
                        user.WorkGroupId = null;
                        existingGroup.Users.Remove(user);
                    }
                }

                var newMembers = await _context.AppUsers
                    .Where(u => targetGuids.Contains(u.Id) && u.TenantId == tenantId && u.WorkGroupId != existingGroup.Id)
                    .ToListAsync();

                foreach (var member in newMembers)
                {
                    member.WorkGroupId = existingGroup.Id;
                    existingGroup.Users.Add(member);
                }
            }

            existingGroup.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWorkGroupAsync(Guid id)
        {
            var tenantId = GetRequiredTenantId();
            var existingGroup = await _context.WorkGroups
                .Include(w => w.Users)
                .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId && !w.IsDeleted);

            if (existingGroup == null)
                throw new Exception("İş qrupu tapılmadı.");

            foreach (var user in existingGroup.Users)
            {
                user.WorkGroupId = null;
            }

            existingGroup.IsDeleted = true;
            existingGroup.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<WorkGroupDTO?> GetWorkGroupByLeaderIdAsync(string leaderId)
        {
            var leaderGuid = Guid.Parse(leaderId);
            var workGroup = await _groupWorkservice.GetAllAsync(q => q
                .Include(w => w.Leader)
                .Include(w => w.Users)
                .Include(w => w.Tasks)
                .Where(w => w.LeaderId == leaderGuid)
            );
            return workGroup.FirstOrDefault();
        }

        public async Task AssignTaskToGroupAsync(Guid taskId, string leaderId, Guid targetWorkGroupId)
        {
            var currentGroup = await GetWorkGroupByLeaderIdAsync(leaderId);
            if (currentGroup == null)
                throw new Exception("User is not a leader of any work group.");

            var targetWorkGroup = await _groupWorkservice.GetByIdAsync(targetWorkGroupId);
            var task = await _genericService.GetByIdAsync(taskId);
            if (task == null)
                throw new Exception("Task not found.");

            if (task.WorkGroupId != currentGroup.Id)
                throw new Exception("Task does not belong to the leader's work group.");

            task.WorkGroupId = targetWorkGroupId;
            task.Status = CurrentSituation.Pending;

            await _genericService.UpdateAsync(task);

            await _transaction.AddAsync(new TaskTransaction
            {
                TaskItemId = task.Id,
                FromUserId = currentGroup.Id,
                ToUserId = targetWorkGroupId,
                Comments = $"Task assigned from {currentGroup.Name} to {targetWorkGroup.Name}"
            });

            await _notification.NotifyTaskAssignedAsync(targetWorkGroup.LeaderId, task.Title, taskId);
        }

        public async Task AddUserToWorkGroupAsync(Guid workGroupId, string userId)
        {
            var tenantId = GetRequiredTenantId();
            var workGroup = await _context.WorkGroups
                .Include(w => w.Users)
                .FirstOrDefaultAsync(w => w.Id == workGroupId && w.TenantId == tenantId && !w.IsDeleted);

            if (workGroup == null)
                throw new Exception("İş qrupu tapılmadı.");

            if (!Guid.TryParse(userId, out var userGuid))
                throw new Exception("İstifadəçi ID-si yanlışdır.");

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userGuid && u.TenantId == tenantId);
            if (user == null)
                throw new Exception("İstifadəçi tapılmadı.");

            if (user.WorkGroupId == workGroupId || workGroup.Users.Any(u => u.Id == userGuid))
                throw new Exception("İstifadəçi artıq bu iş qrupunun üzvüdür.");

            user.WorkGroupId = workGroupId;
            if (!workGroup.Users.Any(u => u.Id == userGuid))
            {
                workGroup.Users.Add(user);
            }

            await _context.SaveChangesAsync();
            await _notification.NotifyUserAddedToWorkGroupAsync(userId, workGroup.Name);
        }

        public async Task RemoveUserFromWorkGroupAsync(Guid workGroupId, string userId)
        {
            var tenantId = GetRequiredTenantId();
            var workGroup = await _context.WorkGroups
                .Include(w => w.Users)
                .FirstOrDefaultAsync(w => w.Id == workGroupId && w.TenantId == tenantId && !w.IsDeleted);

            if (workGroup == null)
                throw new Exception("İş qrupu tapılmadı.");

            if (!Guid.TryParse(userId, out var userGuid))
                throw new Exception("İstifadəçi ID-si yanlışdır.");

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userGuid && u.TenantId == tenantId);
            if (user == null)
                throw new Exception("İstifadəçi tapılmadı.");

            if (user.WorkGroupId != workGroupId && !workGroup.Users.Any(u => u.Id == userGuid))
                throw new Exception("İstifadəçi bu iş qrupunun üzvü deyil.");

            user.WorkGroupId = null;
            workGroup.Users.Remove(user);

            await _context.SaveChangesAsync();
            await _notification.NotifyUserRemovedFromWorkGroupAsync(userId, workGroup.Name);
        }
    }
}
