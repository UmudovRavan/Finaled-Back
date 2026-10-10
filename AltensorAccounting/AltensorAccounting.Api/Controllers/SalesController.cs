using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Contract.DTOs.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly IAccountingService _accountingService;

    public SalesController(ISalesService salesService, IAccountingService accountingService)
    {
        _salesService = salesService;
        _accountingService = accountingService;
    }

    // -------------------------------------------------------------
    // 1. Satış Sifarişləri (Sales Orders)
    // -------------------------------------------------------------
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<SalesOrderDto>), 200)]
    public async Task<ActionResult<List<SalesOrderDto>>> GetOrders(CancellationToken ct)
    {
        var result = await _salesService.GetSalesOrdersAsync(ct);
        return Ok(result);
    }

    [HttpGet("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(SalesOrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid orderId, CancellationToken ct)
    {
        var result = await _salesService.GetSalesOrderByIdAsync(orderId, ct);
        if (result == null) return NotFound(new { message = "Satış sifarişi tapılmadı." });
        return Ok(result);
    }

    [HttpPost("orders")]
    [ProducesResponseType(typeof(SalesOrderDto), 200)]
    public async Task<ActionResult<SalesOrderDto>> CreateOrder([FromBody] CreateSalesOrderDto dto, CancellationToken ct)
    {
        var result = await _salesService.CreateSalesOrderAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("orders/{orderId:guid}/confirm")]
    [ProducesResponseType(typeof(SalesOrderDto), 200)]
    public async Task<ActionResult<SalesOrderDto>> ConfirmOrder([FromRoute] Guid orderId, CancellationToken ct)
    {
        var result = await _salesService.ConfirmSalesOrderAsync(orderId, ct);
        return Ok(result);
    }

    [HttpPost("orders/{orderId:guid}/cancel")]
    [ProducesResponseType(typeof(SalesOrderDto), 200)]
    public async Task<ActionResult<SalesOrderDto>> CancelOrder([FromRoute] Guid orderId, CancellationToken ct)
    {
        var result = await _salesService.CancelSalesOrderAsync(orderId, ct);
        return Ok(result);
    }

    // -------------------------------------------------------------
    // 2. Mal Təhvili / Göndərişi (Delivery Note / Goods Issue)
    // -------------------------------------------------------------
    [HttpGet("deliveries")]
    [ProducesResponseType(typeof(List<DeliveryNoteDto>), 200)]
    public async Task<ActionResult<List<DeliveryNoteDto>>> GetDeliveries(CancellationToken ct)
    {
        var result = await _salesService.GetDeliveryNotesAsync(ct);
        return Ok(result);
    }

    [HttpGet("deliveries/{deliveryId:guid}")]
    [ProducesResponseType(typeof(DeliveryNoteDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDeliveryById([FromRoute] Guid deliveryId, CancellationToken ct)
    {
        var result = await _salesService.GetDeliveryNoteByIdAsync(deliveryId, ct);
        if (result == null) return NotFound(new { message = "Mal göndəriş sənədi tapılmadı." });
        return Ok(result);
    }

    [HttpPost("deliveries")]
    [ProducesResponseType(typeof(DeliveryNoteDto), 200)]
    public async Task<ActionResult<DeliveryNoteDto>> CreateDelivery([FromBody] CreateDeliveryNoteDto dto, CancellationToken ct)
    {
        var result = await _salesService.CreateDeliveryNoteAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("deliveries/{deliveryId:guid}/post")]
    [ProducesResponseType(typeof(DeliveryNoteDto), 200)]
    public async Task<ActionResult<DeliveryNoteDto>> PostDelivery([FromRoute] Guid deliveryId, CancellationToken ct)
    {
        var result = await _salesService.PostDeliveryNoteAsync(deliveryId, ct);
        return Ok(result);
    }

    // -------------------------------------------------------------
    // 3. Satış Qaimələri (Sales Invoices / Customer Invoices)
    // -------------------------------------------------------------
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(List<CustomerInvoiceDto>), 200)]
    public async Task<ActionResult<List<CustomerInvoiceDto>>> GetInvoices(CancellationToken ct)
    {
        var result = await _accountingService.GetCustomerInvoicesAsync(ct);
        return Ok(result);
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(CustomerInvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoiceById([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var result = await _accountingService.GetCustomerInvoiceByIdAsync(invoiceId, ct);
        if (result == null) return NotFound(new { message = "Satış qaiməsi tapılmadı." });
        return Ok(result);
    }

    [HttpPost("invoices")]
    [ProducesResponseType(typeof(CustomerInvoiceDto), 200)]
    public async Task<ActionResult<CustomerInvoiceDto>> CreateInvoice([FromBody] CreateCustomerInvoiceDto dto, CancellationToken ct)
    {
        var result = await _accountingService.CreateCustomerInvoiceAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("invoices/{invoiceId:guid}/post")]
    public async Task<IActionResult> PostInvoice([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var result = await _accountingService.PostCustomerInvoiceAsync(invoiceId, ct);
        return Ok(result);
    }
}
