using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public CompanyController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet("default-accounts")]
    [ProducesResponseType(typeof(CompanyDefaultAccountsDto), 200)]
    public async Task<ActionResult<CompanyDefaultAccountsDto>> GetDefaultAccounts(CancellationToken ct)
    {
        var result = await _accountingService.GetDefaultAccountsAsync(ct);
        return Ok(result);
    }

    [HttpPut("default-accounts")]
    [ProducesResponseType(typeof(CompanyDefaultAccountsDto), 200)]
    public async Task<ActionResult<CompanyDefaultAccountsDto>> UpdateDefaultAccounts(
        [FromBody] CompanyDefaultAccountsDto dto,
        CancellationToken ct)
    {
        var result = await _accountingService.UpdateDefaultAccountsAsync(dto, ct);
        return Ok(result);
    }
}
