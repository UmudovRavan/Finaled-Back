using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDTO>> GetAllProjectsAsync(Guid? divisionId = null);
        Task<ProjectDTO?> GetProjectByIdAsync(Guid id);
        Task<ProjectDTO> CreateProjectAsync(CreateProjectDTO dto);
        Task<ProjectDTO> UpdateProjectAsync(Guid id, UpdateProjectDTO dto);
        Task<bool> DeleteProjectAsync(Guid id);
        Task<bool> CanUserManageProjectAsync(Guid projectId, Guid userId);
    }
}
