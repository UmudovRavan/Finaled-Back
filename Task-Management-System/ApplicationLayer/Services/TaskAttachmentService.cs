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
                // Documents & Office
                ".pdf"   => "application/pdf",
                ".doc"   => "application/msword",
                ".docx"  => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".dot"   => "application/msword",
                ".dotx"  => "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
                ".rtf"   => "application/rtf",
                ".odt"   => "application/vnd.oasis.opendocument.text",

                // Spreadsheets
                ".xls"   => "application/vnd.ms-excel",
                ".xlsx"  => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xlsm"  => "application/vnd.ms-excel.sheet.macroEnabled.12",
                ".xltx"  => "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
                ".csv"   => "text/csv",
                ".ods"   => "application/vnd.oasis.opendocument.spreadsheet",

                // Presentations
                ".ppt"   => "application/vnd.ms-powerpoint",
                ".pptx"  => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".pps"   => "application/vnd.ms-powerpoint",
                ".ppsx"  => "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
                ".potx"  => "application/vnd.openxmlformats-officedocument.presentationml.template",
                ".odp"   => "application/vnd.oasis.opendocument.presentation",

                // Text & Notes
                ".txt"   => "text/plain",
                ".md"    => "text/markdown",
                ".log"   => "text/plain",
                ".cfg"   => "text/plain",
                ".ini"   => "text/plain",

                // Images
                ".jpg"   => "image/jpeg",
                ".jpeg"  => "image/jpeg",
                ".png"   => "image/png",
                ".gif"   => "image/gif",
                ".webp"  => "image/webp",
                ".svg"   => "image/svg+xml",
                ".bmp"   => "image/bmp",
                ".ico"   => "image/x-icon",
                ".tiff"  => "image/tiff",
                ".tif"   => "image/tiff",
                ".heic"  => "image/heic",
                ".heif"  => "image/heif",

                // Archives
                ".zip"   => "application/zip",
                ".rar"   => "application/vnd.rar",
                ".7z"    => "application/x-7z-compressed",
                ".tar"   => "application/x-tar",
                ".gz"    => "application/gzip",

                // Audio
                ".mp3"   => "audio/mpeg",
                ".wav"   => "audio/wav",
                ".ogg"   => "audio/ogg",
                ".m4a"   => "audio/mp4",
                ".aac"   => "audio/aac",
                ".flac"  => "audio/flac",

                // Video
                ".mp4"   => "video/mp4",
                ".mov"   => "video/quicktime",
                ".avi"   => "video/x-msvideo",
                ".mkv"   => "video/x-matroska",
                ".webm"  => "video/webm",
                ".wmv"   => "video/x-ms-wmv",

                // Code & Data
                ".json"  => "application/json",
                ".xml"   => "application/xml",
                ".html"  => "text/html",
                ".htm"   => "text/html",
                ".css"   => "text/css",
                ".js"    => "application/javascript",
                ".sql"   => "application/sql",

                _        => "application/octet-stream"
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
