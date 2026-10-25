using AutoMapper;
using Contract.DTOs;
using Contract.Services;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CompanyTitleService : ICompanyTitleService
    {
        private readonly IGenericRepository<CompanyTitle> _titleRepo;
        private readonly IGenericRepository<TaskItem> _taskRepo;
        private readonly IUnityOfWork _unityOfWork;
        private readonly IMapper _mapper;

        public CompanyTitleService(
            IGenericRepository<CompanyTitle> titleRepo,
            IGenericRepository<TaskItem> taskRepo,
            IUnityOfWork unityOfWork,
            IMapper mapper)
        {
            _titleRepo = titleRepo;
            _taskRepo = taskRepo;
            _unityOfWork = unityOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CompanyTitleDTO>> GetAllTitlesAsync()
        {
            var titles = await _titleRepo.GetAllAsync(q => q
                .Include(t => t.Tasks.Where(task => !task.IsDeleted))
                .Include(t => t.Projects.Where(p => !p.IsDeleted))
                .OrderByDescending(t => t.CreatedAt));

            return titles.Select(t => new CompanyTitleDTO
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Voen = t.Voen,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                TotalTasksCount = t.Tasks.Count,
                TotalProjectsCount = t.Projects.Count
            }).ToList();
        }

        public async Task<CompanyTitleDTO?> GetTitleByIdAsync(Guid id)
        {
            var title = await _titleRepo.GetByIdAsync(id, q => q
                .Include(t => t.Tasks.Where(task => !task.IsDeleted))
                .Include(t => t.Projects.Where(p => !p.IsDeleted)));

            if (title == null) return null;

            return new CompanyTitleDTO
            {
                Id = title.Id,
                Name = title.Name,
                Description = title.Description,
                Voen = title.Voen,
                IsActive = title.IsActive,
                CreatedAt = title.CreatedAt,
                TotalTasksCount = title.Tasks.Count,
                TotalProjectsCount = title.Projects.Count
            };
        }

        public async Task<CompanyTitleDTO> CreateTitleAsync(CreateCompanyTitleDTO dto)
        {
            var normName = dto.Name.Trim().ToLower();
            var existing = await _titleRepo.AnyAsync(q => q.Where(t => t.Name.ToLower() == normName));

            if (existing)
            {
                throw new InvalidOperationException($"'{dto.Name}' adlı Title (Holdinq/Şirkət) artıq mövcuddur.");
            }

            var title = new CompanyTitle
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Voen = dto.Voen,
                IsActive = true
            };

            await _titleRepo.AddAsync(title);
            await _unityOfWork.SaveChangesAsync();

            return (await GetTitleByIdAsync(title.Id))!;
        }

        public async Task<CompanyTitleDTO> UpdateTitleAsync(Guid id, UpdateCompanyTitleDTO dto)
        {
            var title = await _titleRepo.GetByIdAsync(id);
            if (title == null)
            {
                throw new KeyNotFoundException("Title tapılmadı.");
            }

            var normName = dto.Name.Trim().ToLower();
            var duplicate = await _titleRepo.AnyAsync(q => q.Where(t => t.Id != id && t.Name.ToLower() == normName));

            if (duplicate)
            {
                throw new InvalidOperationException($"'{dto.Name}' adlı Title artıq başqa qeyddə mövcuddur.");
            }

            title.Name = dto.Name.Trim();
            title.Description = dto.Description;
            title.Voen = dto.Voen;
            title.IsActive = dto.IsActive;

            await _titleRepo.UpdateAsync(title);
            await _unityOfWork.SaveChangesAsync();

            return (await GetTitleByIdAsync(title.Id))!;
        }

        public async Task<bool> DeleteTitleAsync(Guid id)
        {
            var title = await _titleRepo.GetByIdAsync(id);
            if (title == null) return false;

            // Əgər aktiv tapşırıqlara bağlıdırsa silinməyə icazə vermirik
            var hasActiveTasks = await _taskRepo.AnyAsync(q => q.Where(t => t.TitleId == id));
            if (hasActiveTasks)
            {
                throw new InvalidOperationException("Bu Title-a (Holdinq/Şirkət) bağlı tapşırıqlar mövcud olduğu üçün onu birbaşa silmək olmaz.");
            }

            await _titleRepo.DeleteAsync(id);
            await _unityOfWork.SaveChangesAsync();

            return true;
        }
    }
}
