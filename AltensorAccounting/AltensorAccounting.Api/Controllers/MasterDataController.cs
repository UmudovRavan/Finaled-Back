using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Accounting;
using AltensorAccounting.Contract.DTOs.Inventory;
using AltensorAccounting.Contract.DTOs.MasterData;
using AltensorAccounting.Contract.DTOs.Procurement;
using AltensorAccounting.Contract.DTOs.Treasury;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("api/master-data")]
[Authorize]
public class MasterDataController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;
    private readonly IAccountingService _accountingService;
    private readonly IProcurementService _procurementService;
    private readonly IInventoryService _inventoryService;
    private readonly ITreasuryService _treasuryService;

    public MasterDataController(
        IMasterDataService masterDataService,
        IAccountingService accountingService,
        IProcurementService procurementService,
        IInventoryService inventoryService,
        ITreasuryService treasuryService)
    {
        _masterDataService = masterDataService;
        _accountingService = accountingService;
        _procurementService = procurementService;
        _inventoryService = inventoryService;
        _treasuryService = treasuryService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(MasterDataSummaryDto), 200)]
    public async Task<ActionResult<MasterDataSummaryDto>> GetSummary(CancellationToken ct)
    {
        var result = await _masterDataService.GetSummaryAsync(ct);
        return Ok(result);
    }

    [HttpGet("tabs")]
    [ProducesResponseType(typeof(List<MasterDataTabItemDto>), 200)]
    public async Task<ActionResult<List<MasterDataTabItemDto>>> GetTabs(CancellationToken ct)
    {
        var result = await _masterDataService.GetTabsAsync(ct);
        return Ok(result);
    }

    // Reference Master Data shortcuts under Configuration/Master Data module
    [HttpGet("customers")]
    [ProducesResponseType(typeof(List<CustomerDto>), 200)]
    public async Task<ActionResult<List<CustomerDto>>> GetCustomers(CancellationToken ct)
    {
        var result = await _accountingService.GetCustomersAsync(ct);
        return Ok(result);
    }

    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(List<SupplierDto>), 200)]
    public async Task<ActionResult<List<SupplierDto>>> GetSuppliers(CancellationToken ct)
    {
        var result = await _procurementService.GetSuppliersAsync(ct);
        return Ok(result);
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(List<ItemDto>), 200)]
    public async Task<ActionResult<List<ItemDto>>> GetItems(CancellationToken ct)
    {
        var result = await _inventoryService.GetItemsAsync(ct);
        return Ok(result);
    }

    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(List<WarehouseDto>), 200)]
    public async Task<ActionResult<List<WarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        var result = await _inventoryService.GetWarehousesAsync(ct);
        return Ok(result);
    }

    [HttpGet("bank-accounts")]
    [ProducesResponseType(typeof(List<BankAccountDto>), 200)]
    public async Task<ActionResult<List<BankAccountDto>>> GetBankAccounts(CancellationToken ct)
    {
        var result = await _treasuryService.GetBankAccountsAsync(ct);
        return Ok(result);
    }

    [HttpGet("cash-desks")]
    [ProducesResponseType(typeof(List<CashDeskDto>), 200)]
    public async Task<ActionResult<List<CashDeskDto>>> GetCashDesks(CancellationToken ct)
    {
        var result = await _treasuryService.GetCashDesksAsync(ct);
        return Ok(result);
    }

    // Requirement #5: Initial Balance Setup relocated to Master Data / Configuration
    [HttpPost("initial-balances")]
    public async Task<IActionResult> SetInitialBalances([FromBody] SetInitialBalancesDto dto, CancellationToken ct)
    {
        await _accountingService.SetInitialBalancesAsync(dto, ct);
        return Ok(new { message = "İlkin qalıqlar uğurla daxil edildi və audit jurnalında qeydiyyata alındı." });
    }
}
