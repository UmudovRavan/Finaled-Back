using AutoMapper;
using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentTenantService _tenantService;

        public TenantSettingsService(
            AppDbContext context,
            IMapper mapper,
            ICurrentTenantService tenantService)
        {
            _context = context;
            _mapper = mapper;
            _tenantService = tenantService;
        }

        public async Task<TenantSettingsDTO> GetSettingsAsync()
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var settings = await _context.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);

            if (settings == null)
            {
                // Initialize default settings for tenant
                settings = new TenantSettings
                {
                    TenantId = tenantId,
                    IsOverdueNotificationEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.TenantSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<TenantSettingsDTO>(settings);
        }

        public async Task<TenantSettingsDTO> UpdateSettingsAsync(UpdateTenantSettingsDTO dto)
        {
            var tenantId = _tenantService.TenantId ?? throw new UnauthorizedAccessException("Tenant konteksti tapılmadı.");
            var settings = await _context.TenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);

            if (settings == null)
            {
                settings = new TenantSettings
                {
                    TenantId = tenantId,
                    OverdueTaskNotificationEmail = dto.OverdueTaskNotificationEmail,
                    IsOverdueNotificationEnabled = dto.IsOverdueNotificationEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.TenantSettings.AddAsync(settings);
            }
            else
            {
                settings.OverdueTaskNotificationEmail = dto.OverdueTaskNotificationEmail;
                settings.IsOverdueNotificationEnabled = dto.IsOverdueNotificationEnabled;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<TenantSettingsDTO>(settings);
        }
    }
}
