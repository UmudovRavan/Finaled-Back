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
            // .Include(u => u.PerformancePoints) istifadə edərkən EF Core "fix-up"
            // mexanizmi PerformancePoint.User → AppUser back-reference quraraq
            // JSON serialization-da sonsuz dövrə (cycle) yaradır.
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
                    WorkGroupId   = u.WorkGroupId,
                    PerformancePoints = u.PerformancePoints
                        .Select(p => new PerformancePoint
                        {
                            Id        = p.Id,
                            TenantId  = p.TenantId,
                            UserId    = p.UserId,
                            Points    = p.Points,
                            Reason    = p.Reason,
                            CreatedAt = p.CreatedAt,
                            UpdatedAt = p.UpdatedAt,
                            IsDeleted = p.IsDeleted
                            // User navigation property-ni DAXIL ETMİRİK — dövrü yaradır
                        }).ToList()
                })
                .ToListAsync();
        }

        public async Task<AppUser?> GetByIdAsync(Guid userId)
        {
            return await _db.AppUsers.FindAsync(userId);
        }
    }
}
