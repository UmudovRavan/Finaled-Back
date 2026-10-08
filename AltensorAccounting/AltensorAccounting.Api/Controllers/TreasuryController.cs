using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Treasury;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TreasuryController : ControllerBase
{
    private readonly ITreasuryService _treasuryService;

    public TreasuryController(ITreasuryService treasuryService)
    {
        _treasuryService = treasuryService;
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts(CancellationToken ct)
    {
        var result = await _treasuryService.GetBankAccountsAsync(ct);
        return Ok(result);
    }

    [HttpPost("bank-accounts")]
    public async Task<IActionResult> CreateBankAccount([FromBody] CreateBankAccountDto dto, CancellationToken ct)
    {
        var result = await _treasuryService.CreateBankAccountAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("cash-desks")]
    public async Task<IActionResult> GetCashDesks(CancellationToken ct)
    {
        var result = await _treasuryService.GetCashDesksAsync(ct);
        return Ok(result);
    }

    [HttpPost("cash-desks")]
    public async Task<IActionResult> CreateCashDesk([FromBody] CreateCashDeskDto dto, CancellationToken ct)
    {
        var result = await _treasuryService.CreateCashDeskAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("bank-statements/import")]
    public async Task<IActionResult> ImportBankStatement([FromBody] ImportBankStatementDto dto, CancellationToken ct)
    {
        var result = await _treasuryService.ImportBankStatementAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("payment-runs")]
    public async Task<IActionResult> CreatePaymentRun([FromBody] CreatePaymentRunDto dto, CancellationToken ct)
    {
        var result = await _treasuryService.CreatePaymentRunAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("payment-runs/{id:guid}/post")]
    public async Task<IActionResult> PostPaymentRun([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _treasuryService.PostPaymentRunAsync(id, ct);
        return Ok(result);
    }
}
