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
    public async Task<IActionResult> GetTrialBalance(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] DateTime? asOfDate,
        [FromQuery] string? search,
        [FromQuery] bool includeZeroBalance = false,
        [FromQuery] string? currency = "AZN",
        CancellationToken ct = default)
    {
        var to = toDate ?? asOfDate ?? DateTime.UtcNow;
        var from = fromDate;
        var result = await _reportService.GetTrialBalanceAsync(from, to, search, includeZeroBalance, currency, ct);
        return Ok(result);
    }

    // 1. Financial Position (Maliyyə Vəziyyəti Haqqında Hesabat / Balans)
    [HttpGet("financial-position")]
    public async Task<IActionResult> GetFinancialPosition([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var result = await _reportService.GetFinancialPositionAsync(date, ct);
        return Ok(result);
    }

    // 2. Profit or Loss (Mənfəət və ya Zərər Haqqında Hesabat)
    [HttpGet("profit-or-loss")]
    public async Task<IActionResult> GetProfitOrLoss([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetProfitOrLossAsync(from, to, ct);
        return Ok(result);
    }

    // 3. Changes in Equity (Kapitalda Dəyişikliklər Haqqında Hesabat)
    [HttpGet("changes-in-equity")]
    public async Task<IActionResult> GetChangesInEquity([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetChangesInEquityAsync(from, to, ct);
        return Ok(result);
    }

    // 4. Cash Flow (Pul Vəsaitlərinin Hərəkəti Haqqında Hesabat)
    [HttpGet("cash-flow")]
    public async Task<IActionResult> GetCashFlow([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        var from = fromDate ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetCashFlowAsync(from, to, ct);
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
