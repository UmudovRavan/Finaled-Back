using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly IGenericService<TaskDTO, TaskItem> _genericService;
        private readonly ITaksService _taskService;
        private readonly ITaskAttachmentService _taskAttachmentService;
        private readonly INotificationService _notificationService;

        public TaskController(
            IGenericService<TaskDTO, TaskItem> genericService,
            ITaksService taskService,
            ITaskAttachmentService taskAttachmentService,
            INotificationService notificationService)
        {
            _genericService = genericService;
            _taskService = taskService;
            _taskAttachmentService = taskAttachmentService;
            _notificationService = notificationService;
        }

        [Authorize(Policy = "CanCreateTasks")]
        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromForm] TaskDTO taskDto, [FromForm] List<IFormFile>? files)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(taskDto.CreatedByUserId))
            {
                taskDto.CreatedByUserId = userId ?? Guid.Empty.ToString();
            }

            // Frontend "null" və ya "undefined" string göndərə bilər
            if (taskDto.AssignedToUserId == "null" || taskDto.AssignedToUserId == "undefined" || string.IsNullOrWhiteSpace(taskDto.AssignedToUserId))
            {
                taskDto.AssignedToUserId = null;
            }

            var createdTask = await _genericService.AddAsync(taskDto);

            // Bütün gələn form fayllarını topla (həm files parametri, həm də Request.Form.Files)
            var allFormFiles = new List<IFormFile>();
            if (files != null && files.Count > 0)
            {
                allFormFiles.AddRange(files);
            }
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                foreach (var f in Request.Form.Files)
                {
                    if (!allFormFiles.Contains(f))
                    {
                        allFormFiles.Add(f);
                    }
                }
            }

            if (allFormFiles.Count > 0)
            {
                createdTask.Files ??= new List<FileDto>();
                foreach (var file in allFormFiles)
                {
                    if (file.Length == 0) continue;

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    var fileDto = new FileDto
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Content = ms.ToArray()
                    };

                    var attachment = await _taskAttachmentService.UploadAndSaveAsync(createdTask.Id, fileDto, taskDto.CreatedByUserId);
                    createdTask.Files.Add(new FileDto
                    {
                        Id = attachment.Id,
                        FileName = attachment.FileName,
                        ContentType = attachment.ContentType,
                        Size = attachment.Size,
                        Url = $"https://api-tms.altensor.com/api/TaskAttachment/{attachment.Id}/preview"
                    });
                }
            }

            if (!string.IsNullOrEmpty(taskDto.AssignedToUserId) && Guid.TryParse(taskDto.AssignedToUserId, out _))
            {
                await _notificationService.NotifyTaskAssignedAsync(taskDto.AssignedToUserId, createdTask.Title, createdTask.Id);
            }

            return Ok(createdTask);
        }

        [Authorize(Policy = "CanViewTasks")]
        [HttpGet("GetTask/{id}")]
        public async Task<IActionResult> GetTask(Guid id)
        {
            var task = await _genericService.GetByIdAsync(id, query => query
                .Include(t => t.Attachments)
                .Include(t => t.TaskComments!)
                    .ThenInclude(c => c.User)
                .Include(t => t.TaskComments!)
                    .ThenInclude(c => c.TaskCommentMentions));
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        [Authorize(Policy = "CanUpdateTasks")]
        [HttpPut("UpdateTask")]
        public async Task<IActionResult> UpdateTask([FromBody] TaskDTO taskDto)
        {
            if (taskDto == null || taskDto.Id == Guid.Empty)
            {
                return BadRequest("Yanlış və ya boş tapşırıq məlumatı.");
            }
            var updatedTask = await _genericService.UpdateAsync(taskDto);
            return Ok("Dəyişikliklər Qeyd Olundu");
        }

        [Authorize(Policy = "CanUploadAttachments")]
        [HttpPost("AddFilesToTask/{taskId}")]
        public async Task<IActionResult> AddFilesToTask(Guid taskId, [FromForm] List<IFormFile>? files)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (files == null || files.Count == 0)
                return BadRequest("Fayl göndərilməyib.");

            var uploadTasks = files.Select(async file =>
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var fileDto = new FileDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Content = ms.ToArray()
                };

                await _taskAttachmentService.UploadAndSaveAsync(taskId, fileDto, userId!);
            });

            await Task.WhenAll(uploadTasks);
            return Ok("Fayllar uğurla əlavə olundu.");
        }

        [Authorize(Policy = "CanViewTasks")]
        [HttpGet("GetAllTask")]
        public async Task<IActionResult> GetAllTask()
        {
            var tasks = await _genericService.GetAllAsync(query => query.Include(t => t.Attachments));
            return Ok(tasks);
        }

        [Authorize(Policy = "CanDeleteTasks")]
        [HttpDelete("DeleteTask/{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var result = await _genericService.DeleteAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [Authorize(Policy = "CanCommentTasks")]
        [HttpPost("AddComment")]
        public async Task<IActionResult> AddComment(Guid taskId, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.AddComment(taskId, userId!, comment);
            return Ok();
        }

        [Authorize(Policy = "CanAssignTasks")]
        [HttpPost("AssignTask")]
        public async Task<IActionResult> AssignTask(Guid taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.AssignTaskAsync(taskId, userId!);
            return Ok();
        }

        [Authorize(Policy = "CanAssignTasks")]
        [HttpPost("UnAssignTask")]
        public async Task<IActionResult> UnAssignTask(Guid taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.UnAssingTaskAsync(taskId, userId!);
            return Ok();
        }

        [Authorize(Policy = "CanManageTaskStatus")]
        [HttpPost("AcceptTask")]
        public async Task<IActionResult> AcceptTask(Guid taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.AcceptTask(taskId, userId!);
            return Ok();
        }

        [Authorize(Policy = "CanManageTaskStatus")]
        [HttpPost("reject")]
        public async Task<IActionResult> RejectTask(Guid taskId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.RejectTask(taskId, userId!, reason);
            return Ok();
        }

        [Authorize(Policy = "CanManageTaskStatus")]
        [HttpPost("FinishTask")]
        public async Task<IActionResult> FinishTask(Guid taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _taskService.FinishTask(taskId, userId!);
            return Ok();
        }

        [Authorize(Policy = "CanManageTaskStatus")]
        [HttpPost("ReopenTask")]
        public async Task<IActionResult> ReturnedForRevision(Guid taskId, string userId, string reason)
        {
            await _taskService.ReturnedForRevision(taskId, userId, reason);
            return Ok();
        }
    }
}
