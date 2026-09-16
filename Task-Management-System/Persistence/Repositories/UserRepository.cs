using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;

namespace Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AppUser>> GetAllUsersAsync(Guid tenantId)
        {
            // Select projection ilə yalnız lazım olan sahələri götürürük.
            return await _db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId)
                .Select(u => new AppUser
                {
                    Id            = u.Id,
                    TenantId      = u.TenantId,
                    Email         = u.Email,
                    FullName      = u.FullName,
                    UserName      = u.UserName,
                    CreatedAt     = u.CreatedAt,
                    DivisionId    = u.DivisionId,
                    WorkGroupId   = u.WorkGroupId
                })
                .ToListAsync();
        }

        public async Task<AppUser?> GetByIdAsync(Guid userId)
        {
            return await _db.AppUsers.FindAsync(userId);
        }
    }
}
