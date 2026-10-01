using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProcurementController : ControllerBase
{
    private readonly IProcurementService _procurementService;

    public ProcurementController(IProcurementService procurementService)
    {
        _procurementService = procurementService;
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken ct)
    {
        var result = await _procurementService.GetSuppliersAsync(ct);
        return Ok(result);
    }

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        var result = await _procurementService.CreateSupplierAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("purchase-orders")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _procurementService.CreatePurchaseOrderAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("purchase-orders/{orderId:guid}/approve")]
    public async Task<IActionResult> ApprovePurchaseOrder([FromRoute] Guid orderId, CancellationToken ct)
    {
        var result = await _procurementService.ApprovePurchaseOrderAsync(orderId, ct);
        return Ok(result);
    }

    [HttpPost("goods-receipts")]
    public async Task<IActionResult> CreateGoodsReceipt([FromBody] CreateGoodsReceiptDto dto, CancellationToken ct)
    {
        var result = await _procurementService.CreateGoodsReceiptAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("goods-receipts/{receiptId:guid}/post")]
    public async Task<IActionResult> PostGoodsReceipt([FromRoute] Guid receiptId, CancellationToken ct)
    {
        var result = await _procurementService.PostGoodsReceiptAsync(receiptId, ct);
        return Ok(result);
    }

    [HttpPost("supplier-invoices")]
    public async Task<IActionResult> CreateSupplierInvoice([FromBody] CreateSupplierInvoiceDto dto, CancellationToken ct)
    {
        var result = await _procurementService.CreateSupplierInvoiceAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("supplier-invoices/{invoiceId:guid}/3-way-match")]
    public async Task<IActionResult> EvaluateThreeWayMatch([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var result = await _procurementService.EvaluateThreeWayMatchAsync(invoiceId, ct);
        return Ok(result);
    }

    [HttpPost("supplier-invoices/{invoiceId:guid}/post")]
    public async Task<IActionResult> PostSupplierInvoice([FromRoute] Guid invoiceId, CancellationToken ct)
    {
        var result = await _procurementService.PostSupplierInvoiceAsync(invoiceId, ct);
        return Ok(result);
    }
}
