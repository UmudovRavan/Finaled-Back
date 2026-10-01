using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(CancellationToken ct)
    {
        var result = await _inventoryService.GetItemsAsync(ct);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto, CancellationToken ct)
    {
        var result = await _inventoryService.CreateItemAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct)
    {
        var result = await _inventoryService.GetWarehousesAsync(ct);
        return Ok(result);
    }

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto dto, CancellationToken ct)
    {
        var result = await _inventoryService.CreateWarehouseAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("stock-transactions")]
    public async Task<IActionResult> CreateStockTransaction([FromBody] CreateStockTransactionDto dto, CancellationToken ct)
    {
        var result = await _inventoryService.CreateStockTransactionAsync(dto, ct);
        return Ok(result);
    }

    [HttpPost("stock-transactions/{id:guid}/post")]
    public async Task<IActionResult> PostStockTransaction([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _inventoryService.PostStockTransactionAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("stock-ledger")]
    public async Task<IActionResult> GetStockLedger([FromQuery] Guid? itemId, [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var result = await _inventoryService.GetStockLedgerAsync(itemId, warehouseId, ct);
        return Ok(result);
    }
}
