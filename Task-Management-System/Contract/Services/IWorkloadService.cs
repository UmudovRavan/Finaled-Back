using Contract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contract.Services
{
    public interface IWorkloadService
    {
        Task<IEnumerable<EmployeeWorkloadDTO>> GetAllEmployeesWorkloadAsync();
        Task<EmployeeWorkloadDTO?> GetEmployeeWorkloadAsync(Guid userId);
        Task<WorkloadWarningDTO> CheckWorkloadBeforeAssignAsync(Guid userId);
    }
}
