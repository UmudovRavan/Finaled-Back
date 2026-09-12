using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IProjectLevelService
    {
        Task<IEnumerable<ProjectLevelDTO>> GetLevelsByProjectIdAsync(Guid projectId);
        Task<ProjectLevelDTO?> GetLevelByIdAsync(Guid id);
        Task<ProjectLevelDTO> CreateLevelAsync(CreateProjectLevelDTO dto);
        Task<ProjectLevelDTO> UpdateLevelAsync(Guid id, UpdateProjectLevelDTO dto);
        Task<bool> DeleteLevelAsync(Guid id);
    }
}
