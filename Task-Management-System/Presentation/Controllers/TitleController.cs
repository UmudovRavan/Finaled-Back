using Contract.DTOs;
using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    /// <summary>
    /// Üç əsas analitik bölgüdən biri: Title (Holdinq / Şirkət / MMC — məsələn: A Holdinq, B Holdinq, X MMC).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TitleController : ControllerBase
    {
        private readonly ICompanyTitleService _titleService;

        public TitleController(ICompanyTitleService titleService)
        {
            _titleService = titleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTitles()
        {
            var titles = await _titleService.GetAllTitlesAsync();
            return Ok(titles);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTitle(Guid id)
        {
            var title = await _titleService.GetTitleByIdAsync(id);
            if (title == null)
            {
                return NotFound(new { message = "Title (Holdinq/Şirkət) tapılmadı." });
            }
            return Ok(title);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTitle([FromBody] CreateCompanyTitleDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var created = await _titleService.CreateTitleAsync(dto);
            return CreatedAtAction(nameof(GetTitle), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateCompanyTitleDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _titleService.UpdateTitleAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTitle(Guid id)
        {
            var deleted = await _titleService.DeleteTitleAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Title tapılmadı." });
            }
            return NoContent();
        }
    }
}
