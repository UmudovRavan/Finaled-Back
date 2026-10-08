using System;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Persistence.Data;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Persistence.Services;

public class TenantSeeder : ITenantSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(AppDbContext context, ILogger<TenantSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task SeedTenantDefaultsAsync(Guid tenantId, CancellationToken ct = default)
    {
        return DbSeeder.SeedTenantAccountingDefaultsAsync(_context, tenantId, _logger);
    }
}
