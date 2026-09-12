using Contract.DTOs;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectLevelController : ControllerBase
    {
        private readonly IProjectLevelService _levelService;

        public ProjectLevelController(IProjectLevelService levelService)
        {
            _levelService = levelService;
        }

        [HttpGet("by-project/{projectId:guid}")]
        [Authorize(Policy = "CanViewLevels")]
        public async Task<IActionResult> GetLevelsByProject(Guid projectId)
        {
            var levels = await _levelService.GetLevelsByProjectIdAsync(projectId);
            return Ok(levels);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewLevels")]
        public async Task<IActionResult> GetLevel(Guid id)
        {
            var level = await _levelService.GetLevelByIdAsync(id);
            if (level == null)
                return NotFound(new { message = "Mərhələ tapılmadı." });

            return Ok(level);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateLevels")]
        public async Task<IActionResult> CreateLevel([FromBody] CreateProjectLevelDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _levelService.CreateLevelAsync(dto);
            return CreatedAtAction(nameof(GetLevel), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanUpdateLevels")]
        public async Task<IActionResult> UpdateLevel(Guid id, [FromBody] UpdateProjectLevelDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _levelService.UpdateLevelAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanDeleteLevels")]
        public async Task<IActionResult> DeleteLevel(Guid id)
        {
            var result = await _levelService.DeleteLevelAsync(id);
            if (!result)
                return NotFound(new { message = "Mərhələ tapılmadı." });

            return NoContent();
        }
    }
}
