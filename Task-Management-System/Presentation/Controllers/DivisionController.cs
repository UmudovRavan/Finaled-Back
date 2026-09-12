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
    public class DivisionController : ControllerBase
    {
        private readonly IDivisionService _divisionService;

        public DivisionController(IDivisionService divisionService)
        {
            _divisionService = divisionService;
        }

        [HttpGet]
        [Authorize(Policy = "CanViewDivisions")]
        public async Task<IActionResult> GetAllDivisions()
        {
            var divisions = await _divisionService.GetAllDivisionsAsync();
            return Ok(divisions);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewDivisions")]
        public async Task<IActionResult> GetDivision(Guid id)
        {
            var division = await _divisionService.GetDivisionByIdAsync(id);
            if (division == null)
                return NotFound(new { message = "Şöbə tapılmadı." });

            return Ok(division);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateDivisions")]
        public async Task<IActionResult> CreateDivision([FromBody] CreateDivisionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _divisionService.CreateDivisionAsync(dto);
            return CreatedAtAction(nameof(GetDivision), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanUpdateDivisions")]
        public async Task<IActionResult> UpdateDivision(Guid id, [FromBody] UpdateDivisionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _divisionService.UpdateDivisionAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanDeleteDivisions")]
        public async Task<IActionResult> DeleteDivision(Guid id)
        {
            var result = await _divisionService.DeleteDivisionAsync(id);
            if (!result)
                return NotFound(new { message = "Şöbə tapılmadı." });

            return NoContent();
        }
    }
}
