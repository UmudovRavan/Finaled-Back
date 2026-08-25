using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TaskAttachmentService : ITaskAttachmentService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _tenantService;
        private readonly ILogger<TaskAttachmentService> _logger;

        public TaskAttachmentService(
            IFileStorageService fileStorageService,
            AppDbContext context,
            ICurrentTenantService tenantService,
            ILogger<TaskAttachmentService> logger)
        {
            _fileStorageService = fileStorageService;
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public async Task<string> GetPreviewUrlAsync(Guid attachmentId, string currentUserId)
        {
            var attachment = await GetAttachmentFromDbAsync(attachmentId, currentUserId);
            if (attachment == null || string.IsNullOrEmpty(attachment.ObjectName))
                return string.Empty;

            return await _fileStorageService.GetPresignedUrlAsync(attachment.ObjectName);
        }

        public async Task<FileDto?> DownloadAsync(Guid attachmentId, string currentUserId)
        {
            try
            {
                var attachment = await GetAttachmentFromDbAsync(attachmentId, currentUserId);
                if (attachment == null || string.IsNullOrEmpty(attachment.ObjectName))
                {
                    _logger.LogWarning("Task attachment bazada tapılmadı: {AttachmentId}", attachmentId);
                    return null;
                }

                var stream = await _fileStorageService.DownloadAsync(attachment.ObjectName);
                if (stream == null)
                {
                    _logger.LogWarning("Fayl Minio yaddaşında tapılmadı: {ObjectName}", attachment.ObjectName);
                    return null;
                }

                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                return new FileDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    ContentType = !string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? attachment.ContentType
                        : GetContentType(attachment.FileName),
                    Size = attachment.Size > 0 ? attachment.Size : ms.Length,
                    Content = ms.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fayl endirilməsi zamanı xəta: AttachmentId={AttachmentId}", attachmentId);
                return null;
            }
        }

        private async Task<TaskAttachment?> GetAttachmentFromDbAsync(Guid attachmentId, string currentUserId)
        {
            return await _context.Set<TaskAttachment>()
                .AsNoTracking()
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf"  => "application/pdf",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".gif"  => "image/gif",
                ".webp" => "image/webp",
                ".svg"  => "image/svg+xml",
                ".txt"  => "text/plain",
                ".csv"  => "text/csv",
                ".doc"  => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls"  => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip"  => "application/zip",
                _       => "application/octet-stream"
            };
        }

        public async Task<TaskAttachment> UploadAndSaveAsync(Guid taskId, FileDto fileDto, string currentUserId)
        {
            var objectName = await _fileStorageService.UploadAsync(fileDto);

            var tenantId = _tenantService.TenantId;
            if (!tenantId.HasValue)
            {
                var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                tenantId = task?.TenantId ?? Guid.Empty;
            }

            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                TaskId = taskId,
                FileName = fileDto.FileName,
                ObjectName = objectName,
                ContentType = !string.IsNullOrWhiteSpace(fileDto.ContentType)
                    ? fileDto.ContentType
                    : GetContentType(fileDto.FileName),
                Size = fileDto.Content?.Length ?? 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Set<TaskAttachment>().AddAsync(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }
    }
}
