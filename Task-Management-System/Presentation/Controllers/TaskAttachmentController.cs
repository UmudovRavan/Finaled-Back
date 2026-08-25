using Contract.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskAttachmentController : ControllerBase
    {
        private readonly ITaskAttachmentService _taskAttachmentService;
        public TaskAttachmentController(ITaskAttachmentService taskAttachmentService)
        {
            _taskAttachmentService = taskAttachmentService;
        }

        [Authorize(Policy = "CanViewAttachments")]
        [HttpGet("{attachmentId:guid}/download")]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var fileDto = await _taskAttachmentService.DownloadAsync(attachmentId, userId ?? string.Empty);
            if (fileDto == null || fileDto.Content == null)
            {
                return NotFound();
            }
            return File(fileDto.Content, fileDto.ContentType, fileDto.FileName, enableRangeProcessing: true);
        }

        [Authorize(Policy = "CanViewAttachments")]
        [HttpGet("{attachmentId:guid}/preview")]
        [HttpGet("{attachmentId:guid}/view")]
        public async Task<IActionResult> PreviewAttachment(Guid attachmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var fileDto = await _taskAttachmentService.DownloadAsync(attachmentId, userId ?? string.Empty);
            if (fileDto == null || fileDto.Content == null)
            {
                return NotFound();
            }

            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileDto.FileName}\"");
            return File(fileDto.Content, fileDto.ContentType, enableRangeProcessing: true);
        }

        [Authorize(Policy = "CanViewAttachments")]
        [HttpGet("{attachmentId:guid}/preview-url")]
        public Task<IActionResult> GetPresignedUrl(Guid attachmentId)
        {
            var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
            var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;
            var url = $"{scheme}://{host}/api/TaskAttachment/{attachmentId}/preview";

            return Task.FromResult<IActionResult>(Ok(new { Url = url }));
        }
    }
}
