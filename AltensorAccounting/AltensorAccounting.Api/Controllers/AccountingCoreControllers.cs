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
    [ProducesResponseType(typeof(List<AccountDto>), 200)]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts(CancellationToken ct)
    {
        var result = await _accountingService.GetAccountsAsync(ct);
        return Ok(result);
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(List<AccountTreeNodeDto>), 200)]
    public async Task<ActionResult<List<AccountTreeNodeDto>>> GetAccountTree(CancellationToken ct)
    {
        var result = await _accountingService.GetAccountTreeAsync(ct);
        return Ok(result);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(List<AccountBalanceRowDto>), 200)]
    public async Task<ActionResult<List<AccountBalanceRowDto>>> GetAccountBalances(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] bool includeZeroBalance = false,
        [FromQuery] string? currency = "AZN",
        CancellationToken ct = default)
    {
        var result = await _accountingService.GetAccountBalancesAsync(fromDate, toDate, search, includeZeroBalance, currency, ct);
        return Ok(result);
    }

    [HttpGet("types")]
    [ProducesResponseType(typeof(List<AccountTypeOptionDto>), 200)]
    public async Task<ActionResult<List<AccountTypeOptionDto>>> GetAccountTypes(CancellationToken ct)
    {
        var result = await _accountingService.GetAccountTypesAsync(ct);
        return Ok(result);
    }

    [HttpGet("subcategories")]
    [ProducesResponseType(typeof(List<CategorySubcategoryMappingDto>), 200)]
    public async Task<ActionResult<List<CategorySubcategoryMappingDto>>> GetSubcategories(CancellationToken ct)
    {
        var result = await _accountingService.GetSubcategoriesAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), 200)]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateAccountAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("initial-balances")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SetInitialBalances([FromBody] SetInitialBalancesDto dto, CancellationToken ct)
    {
        await _accountingService.SetInitialBalancesAsync(dto, ct);
        return Ok(new { success = true, message = "İlkin qalıqlar uğurla daxil edildi və audit qeydi yaradıldı." });
    }

    [HttpPost("seed-template")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SeedTemplate(CancellationToken ct)
    {
        await _accountingService.SeedTemplateAsync(ct);
        return Ok(new { success = true, message = "Standart hesab planı və şirkət default hesabları uğurla quruldu." });
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
