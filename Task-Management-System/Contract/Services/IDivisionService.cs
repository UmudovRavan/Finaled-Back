using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IDivisionService
    {
        Task<IEnumerable<DivisionDTO>> GetAllDivisionsAsync();
        Task<DivisionDTO?> GetDivisionByIdAsync(Guid id);
        Task<DivisionDTO> CreateDivisionAsync(CreateDivisionDTO dto);
        Task<DivisionDTO> UpdateDivisionAsync(Guid id, UpdateDivisionDTO dto);
        Task<bool> DeleteDivisionAsync(Guid id);
        Task<bool> IsUserManagerOfDivisionAsync(Guid divisionId, Guid userId);
    }
}
