using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/customer-invoices")]
[Authorize]
public class CustomerInvoicesController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public CustomerInvoicesController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateCustomerInvoiceDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateCustomerInvoiceAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("{invoiceId:guid}/post")]
    public async Task<IActionResult> PostInvoice([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var result = await _accountingService.PostCustomerInvoiceAsync(invoiceId, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public PaymentsController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreatePaymentAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("{paymentId:guid}/post")]
    public async Task<IActionResult> PostPayment([FromRoute] Guid paymentId, CancellationToken ct)
    {
        var result = await _accountingService.PostPaymentAsync(paymentId, ct);
        return Ok(result);
    }
}
