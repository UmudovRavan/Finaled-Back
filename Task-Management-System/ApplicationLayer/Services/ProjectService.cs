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
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentTenantService _tenantService;

        public ProjectService(
            AppDbContext context,
            IMapper mapper,
            ICurrentTenantService tenantService)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<ProjectDTO>> GetAllProjectsAsync(Guid? divisionId = null)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var query = _context.Projects
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .Include(p => p.Division)
                .Include(p => p.Manager)
                .Include(p => p.Levels.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.Tasks.Where(t => !t.IsDeleted))
                .AsQueryable();

            if (divisionId.HasValue)
            {
                query = query.Where(p => p.DivisionId == divisionId.Value);
            }

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<ProjectDTO>>(projects);
        }

        public async Task<ProjectDTO?> GetProjectByIdAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var project = await _context.Projects
                .Where(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted)
                .Include(p => p.Division)
                .Include(p => p.Manager)
                .Include(p => p.Levels.Where(l => !l.IsDeleted).OrderBy(l => l.Order))
                    .ThenInclude(l => l.Tasks.Where(t => !t.IsDeleted))
                        .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefaultAsync();

            return project == null ? null : _mapper.Map<ProjectDTO>(project);
        }

        public async Task<ProjectDTO> CreateProjectAsync(CreateProjectDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");

            // Check manager exists
            var managerExists = await _context.AppUsers
                .AnyAsync(u => u.Id == dto.ManagerId && u.TenantId == tenantId);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Layihə meneceri (UserId: {dto.ManagerId}) tapılmadı.");
            }

            // Check division if provided
            if (dto.DivisionId.HasValue)
            {
                var divisionExists = await _context.Divisions
                    .AnyAsync(d => d.Id == dto.DivisionId.Value && d.TenantId == tenantId && !d.IsDeleted);
                if (!divisionExists)
                {
                    throw new KeyNotFoundException("Göstərilən şöbə tapılmadı.");
                }
            }

            var project = _mapper.Map<Project>(dto);
            project.TenantId = tenantId;
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            return (await GetProjectByIdAsync(project.Id))!;
        }

        public async Task<ProjectDTO> UpdateProjectAsync(Guid id, UpdateProjectDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted);

            if (project == null)
            {
                throw new KeyNotFoundException("Layihə tapılmadı.");
            }

            var managerExists = await _context.AppUsers
                .AnyAsync(u => u.Id == dto.ManagerId && u.TenantId == tenantId);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Layihə meneceri (UserId: {dto.ManagerId}) tapılmadı.");
            }

            if (dto.DivisionId.HasValue)
            {
                var divisionExists = await _context.Divisions
                    .AnyAsync(d => d.Id == dto.DivisionId.Value && d.TenantId == tenantId && !d.IsDeleted);
                if (!divisionExists)
                {
                    throw new KeyNotFoundException("Göstərilən şöbə tapılmadı.");
                }
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.DivisionId = dto.DivisionId;
            project.ManagerId = dto.ManagerId;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await GetProjectByIdAsync(project.Id))!;
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && !p.IsDeleted);

            if (project == null)
                return false;

            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserManageProjectAsync(Guid projectId, Guid userId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var project = await _context.Projects
                .Include(p => p.Division)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId && !p.IsDeleted);

            if (project == null)
                return false;

            if (project.ManagerId == userId)
                return true;

            if (project.Division != null && project.Division.ManagerId == userId)
                return true;

            return false;
        }
    }
}
