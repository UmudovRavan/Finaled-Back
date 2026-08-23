using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly AppDbContext _db;

        public AppUserRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Auth Service webhook-dan gələn user məlumatını upsert edir.
        /// Əgər user mövcuddursa — məlumatları yeniləyir.
        /// Mövcud deyilsə — yeni AppUser yaradır.
        /// </summary>
        public async Task EnsureExistsAsync(Guid userId, Guid tenantId, string email, string? fullName, string? userName)
        {
            var existing = await _db.AppUsers.FindAsync(userId);
            if (existing == null)
            {
                _db.AppUsers.Add(new AppUser
                {
                    Id = userId,
                    TenantId = tenantId,
                    Email = email,
                    FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : (!string.IsNullOrWhiteSpace(email) ? email.Split('@')[0] : null),
                    UserName = !string.IsNullOrWhiteSpace(userName) ? userName : (!string.IsNullOrWhiteSpace(email) ? email.Split('@')[0] : null),
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
                if (!string.IsNullOrWhiteSpace(fullName)) existing.FullName = fullName;
                if (!string.IsNullOrWhiteSpace(userName)) existing.UserName = userName;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<AppUser?> GetByIdAsync(Guid userId)
        {
            return await _db.AppUsers.FindAsync(userId);
        }

        public async Task<List<AppUser>> GetByTenantAsync(Guid tenantId)
        {
            return await _db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();
        }
    }
}
