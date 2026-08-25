using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Minio;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TaskAttachmentService : ITaskAttachmentService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IGenericRepository<TaskAttachment> _attachmentRepository;
        private readonly IUnityOfWork _unitOfWork;

        public TaskAttachmentService(IFileStorageService fileStorageService, IGenericRepository<TaskAttachment> attachmentRepository, IUnityOfWork unit)
        {
            _fileStorageService = fileStorageService;
            _attachmentRepository = attachmentRepository;
            _unitOfWork = unit;
        }

        public async Task<string> GetPreviewUrlAsync(Guid attachmentId, string currentUserId)
        {
            var attachment = await GetAttachmentFromDbAsync(attachmentId, currentUserId);
            if (attachment == null || string.IsNullOrEmpty(attachment.ObjectName))
                throw new FileNotFoundException("Attachment not found");

            return await _fileStorageService.GetPresignedUrlAsync(attachment.ObjectName);
        }

        public async Task<FileDto> DownloadAsync(Guid attachmentId, string currentUserId)
        {
            var attachment = await GetAttachmentFromDbAsync(attachmentId, currentUserId);
            if (attachment == null || string.IsNullOrEmpty(attachment.ObjectName))
                throw new FileNotFoundException("Attachment not found");

            var stream = await _fileStorageService.DownloadAsync(attachment.ObjectName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            return new FileDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = !string.IsNullOrWhiteSpace(attachment.ContentType)
                    ? attachment.ContentType
                    : GetContentType(attachment.FileName),
                Size = attachment.Size,
                Content = ms.ToArray()
            };
        }

        private async Task<TaskAttachment?> GetAttachmentFromDbAsync(Guid attachmentId, string currentUserId)
        {
            var attachment = await _attachmentRepository.GetByIdAsync(
                attachmentId,
                include: q => q.Include(a => a.Task)
            );

            return attachment;
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

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                FileName = fileDto.FileName,
                ObjectName = objectName,
                ContentType = !string.IsNullOrWhiteSpace(fileDto.ContentType)
                    ? fileDto.ContentType
                    : GetContentType(fileDto.FileName),
                Size = fileDto.Content?.Length ?? 0
            };

            await _attachmentRepository.AddAsync(attachment);
            await _unitOfWork.SaveChangesAsync();

            return attachment;
        }
    }
}
