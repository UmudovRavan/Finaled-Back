using Contract.DTOs;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface ITenantSettingsService
    {
        Task<TenantSettingsDTO> GetSettingsAsync();
        Task<TenantSettingsDTO> UpdateSettingsAsync(UpdateTenantSettingsDTO dto);
    }
}
