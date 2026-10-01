using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Domain.Entities.Procurement;

namespace AltensorAccounting.Application.Services.Procurement;

public interface IThreeWayMatchService
{
    Task<ThreeWayMatchResultDto> EvaluateMatchAsync(SupplierInvoice invoice, CancellationToken ct = default);
}
