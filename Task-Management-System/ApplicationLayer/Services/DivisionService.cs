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
    public class DivisionService : IDivisionService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentTenantService _tenantService;

        public DivisionService(
            AppDbContext context,
            IMapper mapper,
            ICurrentTenantService tenantService)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
        }

        public async Task<IEnumerable<DivisionDTO>> GetAllDivisionsAsync()
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var divisions = await _context.Divisions
                .Where(d => d.TenantId == tenantId && !d.IsDeleted)
                .Include(d => d.Manager)
                .Include(d => d.Projects.Where(p => !p.IsDeleted))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DivisionDTO>>(divisions);
        }

        public async Task<DivisionDTO?> GetDivisionByIdAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var division = await _context.Divisions
                .Where(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted)
                .Include(d => d.Manager)
                .Include(d => d.Projects.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Levels.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync();

            return division == null ? null : _mapper.Map<DivisionDTO>(division);
        }

        public async Task<DivisionDTO> CreateDivisionAsync(CreateDivisionDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");

            // Manager must exist in the same tenant
            var managerExists = await _context.AppUsers
                .AnyAsync(u => u.Id == dto.ManagerId && u.TenantId == tenantId);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Menecer (UserId: {dto.ManagerId}) tapılmadı.");
            }

            var division = _mapper.Map<Division>(dto);
            division.TenantId = tenantId;
            division.CreatedAt = DateTime.UtcNow;
            division.UpdatedAt = DateTime.UtcNow;

            await _context.Divisions.AddAsync(division);
            await _context.SaveChangesAsync();

            return (await GetDivisionByIdAsync(division.Id))!;
        }

        public async Task<DivisionDTO> UpdateDivisionAsync(Guid id, UpdateDivisionDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var division = await _context.Divisions
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);

            if (division == null)
            {
                throw new KeyNotFoundException("Şöbə tapılmadı.");
            }

            var managerExists = await _context.AppUsers
                .AnyAsync(u => u.Id == dto.ManagerId && u.TenantId == tenantId);
            if (!managerExists)
            {
                throw new KeyNotFoundException($"Menecer (UserId: {dto.ManagerId}) tapılmadı.");
            }

            division.Name = dto.Name;
            division.Description = dto.Description;
            division.ManagerId = dto.ManagerId;
            division.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await GetDivisionByIdAsync(division.Id))!;
        }

        public async Task<bool> DeleteDivisionAsync(Guid id)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var division = await _context.Divisions
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);

            if (division == null)
                return false;

            division.IsDeleted = true;
            division.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserManagerOfDivisionAsync(Guid divisionId, Guid userId)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            return await _context.Divisions
                .AnyAsync(d => d.Id == divisionId && d.TenantId == tenantId && d.ManagerId == userId && !d.IsDeleted);
        }
    }
}
