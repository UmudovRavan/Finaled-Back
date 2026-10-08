using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Webhooks;
using AltensorAccounting.Persistence.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Api.Controllers;

[ApiController]
[Route("internal/webhooks")]
public class InternalWebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserSyncService _userSyncService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InternalWebhooksController> _logger;

    public InternalWebhooksController(
        IConfiguration configuration,
        IUserSyncService userSyncService,
        IServiceProvider serviceProvider,
        ILogger<InternalWebhooksController> logger)
    {
        _configuration = configuration;
        _userSyncService = userSyncService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpPost("user-created")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleUserCreated(
        [FromBody] UserCreatedIntegrationEvent @event,
        [FromHeader(Name = "X-Webhook-Secret")] string? webhookSecret,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        CancellationToken cancellationToken)
    {
        var expectedSecret = _configuration["Webhook:SharedSecret"] 
                          ?? _configuration["InternalCommunication:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            _logger.LogError("[AltensorAccounting] Webhook secret konfiqurasiya edilməyib (Webhook:SharedSecret və ya InternalCommunication:ApiKey).");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Webhook secret not configured." });
        }

        var incomingSecret = !string.IsNullOrWhiteSpace(webhookSecret) ? webhookSecret : apiKey;

        if (string.IsNullOrWhiteSpace(incomingSecret) || 
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(incomingSecret), 
                System.Text.Encoding.UTF8.GetBytes(expectedSecret)))
        {
            _logger.LogWarning("[AltensorAccounting] İcazəsiz webhook sorğusu cəhdi. UserId: {UserId}", @event?.UserId);
            return Unauthorized(new { message = "Unauthorized webhook request." });
        }

        if (@event == null || @event.UserId == Guid.Empty || @event.TenantId == Guid.Empty)
        {
            return BadRequest(new { message = "Invalid event payload." });
        }

        _logger.LogInformation("[AltensorAccounting] Webhook qəbul olundu: UserId={UserId}, TenantId={TenantId}, Email={Email}", 
            @event.UserId, @event.TenantId, @event.Email);

        // 1. Sync User into local table
        try
        {
            await _userSyncService.SyncUserCreatedAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AltensorAccounting] User sync xətası (UserId={UserId}): {Message}", @event.UserId, ex.Message);
        }

        // 2. Ensure Tenant Defaults (Chart of Accounts, Company) are seeded
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbSeeder.SeedTenantAccountingDefaultsAsync(db, @event.TenantId, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AltensorAccounting] Tenant defaults seed edilərkən xəta: {Message}", ex.Message);
        }

        return Ok(new { success = true });
    }
}
