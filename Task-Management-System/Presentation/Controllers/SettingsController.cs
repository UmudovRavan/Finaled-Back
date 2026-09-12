using Contract.DTOs;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ITenantSettingsService _settingsService;

        public SettingsController(ITenantSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        [Authorize(Policy = "CanViewSettings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return Ok(settings);
        }

        [HttpPut]
        [Authorize(Policy = "CanUpdateSettings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _settingsService.UpdateSettingsAsync(dto);
            return Ok(updated);
        }
    }
}
