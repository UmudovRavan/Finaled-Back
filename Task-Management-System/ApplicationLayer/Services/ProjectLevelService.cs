using AutoMapper;
using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ProjectLevelService : IProjectLevelService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentTenantService _tenantService;

        public ProjectLevelService(
            AppDbContext context,
            IMapper mapper,
            ICurrentTenantService tenantService)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<ProjectLevelDTO>> GetLevelsByProjectIdAsync(Guid projectId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var levels = await _context.ProjectLevels
                .Where(l => l.ProjectId == projectId && l.TenantId == tenantId && !l.IsDeleted)
                .Include(l => l.Project)
                .Include(l => l.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.AssignedToUser)
                .OrderBy(l => l.Order)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectLevelDTO>>(levels);
        }

        public async Task<ProjectLevelDTO?> GetLevelByIdAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var level = await _context.ProjectLevels
                .Where(l => l.Id == id && l.TenantId == tenantId && !l.IsDeleted)
                .Include(l => l.Project)
                .Include(l => l.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefaultAsync();

            return level == null ? null : _mapper.Map<ProjectLevelDTO>(level);
        }

        public async Task<ProjectLevelDTO> CreateLevelAsync(CreateProjectLevelDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == dto.ProjectId && p.TenantId == tenantId && !p.IsDeleted);

            if (!projectExists)
            {
                throw new KeyNotFoundException("Layihə tapılmadı.");
            }

            var level = _mapper.Map<ProjectLevel>(dto);
            level.TenantId = tenantId;
            level.CreatedAt = DateTime.UtcNow;
            level.UpdatedAt = DateTime.UtcNow;

            // Auto-assign order if 0
            if (level.Order == 0)
            {
                var maxOrder = await _context.ProjectLevels
                    .Where(l => l.ProjectId == dto.ProjectId && l.TenantId == tenantId && !l.IsDeleted)
                    .Select(l => (int?)l.Order)
                    .MaxAsync() ?? 0;
                level.Order = maxOrder + 1;
            }

            await _context.ProjectLevels.AddAsync(level);
            await _context.SaveChangesAsync();

            return (await GetLevelByIdAsync(level.Id))!;
        }

        public async Task<ProjectLevelDTO> UpdateLevelAsync(Guid id, UpdateProjectLevelDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var level = await _context.ProjectLevels
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId && !l.IsDeleted);

            if (level == null)
            {
                throw new KeyNotFoundException("Mərhələ tapılmadı.");
            }

            level.Name = dto.Name;
            level.Description = dto.Description;
            level.Order = dto.Order;
            level.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await GetLevelByIdAsync(level.Id))!;
        }

        public async Task<bool> DeleteLevelAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var level = await _context.ProjectLevels
                .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId && !l.IsDeleted);

            if (level == null)
                return false;

            level.IsDeleted = true;
            level.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
