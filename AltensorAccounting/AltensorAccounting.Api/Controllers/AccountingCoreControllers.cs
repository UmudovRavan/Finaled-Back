using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public AccountsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var result = await _accountingService.GetAccountsAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateAccountAsync(dto, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/fiscal-periods")]
[Authorize]
public class FiscalPeriodsController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public FiscalPeriodsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpPost("years")]
    public async Task<IActionResult> CreateFiscalYear([FromBody] CreateFiscalYearDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateFiscalYearAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("years")]
    public async Task<IActionResult> GetFiscalYears(CancellationToken ct)
    {
        var result = await _accountingService.GetFiscalYearsAsync(ct);
        return Ok(result);
    }

    [HttpPost("{periodId:guid}/close")]
    public async Task<IActionResult> ClosePeriod([FromRoute] Guid periodId, CancellationToken ct)
    {
        await _accountingService.ClosePeriodAsync(periodId, ct);
        return Ok(new { success = true, message = "Maliyyə dövrü uğurla bağlandı." });
    }
}

[ApiController]
[Route("api/manual-journals")]
[Authorize]
public class ManualJournalsController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public ManualJournalsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateManualJournal([FromBody] CreateManualJournalDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateManualJournalAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("{journalId:guid}/post")]
    public async Task<IActionResult> PostManualJournal([FromRoute] Guid journalId, CancellationToken ct)
    {
        var result = await _accountingService.PostManualJournalAsync(journalId, ct);
        return Ok(result);
    }

    public record ReversalRequest(string Reason, DateTime ReversalDate);

    [HttpPost("{journalId:guid}/reverse")]
    public async Task<IActionResult> ReverseManualJournal([FromRoute] Guid journalId, [FromBody] ReversalRequest req, CancellationToken ct)
    {
        var result = await _accountingService.ReverseManualJournalAsync(journalId, req.Reason, req.ReversalDate, ct);
        return Ok(result);
    }
}
