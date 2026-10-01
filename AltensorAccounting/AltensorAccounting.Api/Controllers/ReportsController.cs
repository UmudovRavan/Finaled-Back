using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var result = await _reportService.GetTrialBalanceAsync(date, ct);
        return Ok(result);
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var result = await _reportService.GetBalanceSheetAsync(date, ct);
        return Ok(result);
    }

    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetIncomeStatementAsync(from, to, ct);
        return Ok(result);
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetAgingReport([FromQuery] string partyType = "Customer", [FromQuery] DateTime? asOfDate = null, CancellationToken ct = default)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var result = await _reportService.GetAgingReportAsync(partyType, date, ct);
        return Ok(result);
    }

    [HttpGet("subledger-reconciliation")]
    public async Task<IActionResult> GetSubledgerReconciliation([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var result = await _reportService.GetSubledgerReconciliationAsync(date, ct);
        return Ok(result);
    }
}
