using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface ICompanyTitleService
    {
        Task<IEnumerable<CompanyTitleDTO>> GetAllTitlesAsync();
        Task<CompanyTitleDTO?> GetTitleByIdAsync(Guid id);
        Task<CompanyTitleDTO> CreateTitleAsync(CreateCompanyTitleDTO dto);
        Task<CompanyTitleDTO> UpdateTitleAsync(Guid id, UpdateCompanyTitleDTO dto);
        Task<bool> DeleteTitleAsync(Guid id);
    }
}
