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
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        [Authorize(Policy = "CanViewProjects")]
        public async Task<IActionResult> GetAllProjects([FromQuery] Guid? divisionId)
        {
            var projects = await _projectService.GetAllProjectsAsync(divisionId);
            return Ok(projects);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewProjects")]
        public async Task<IActionResult> GetProject(Guid id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
                return NotFound(new { message = "Layihə tapılmadı." });

            return Ok(project);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateProjects")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _projectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetProject), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanUpdateProjects")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _projectService.UpdateProjectAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanDeleteProjects")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var result = await _projectService.DeleteProjectAsync(id);
            if (!result)
                return NotFound(new { message = "Layihə tapılmadı." });

            return NoContent();
        }
    }
}
