using System;
using System.Threading;
using System.Threading.Tasks;

namespace AltensorAccounting.Application.Interfaces;

public interface ITenantSeeder
{
    Task SeedTenantDefaultsAsync(Guid tenantId, CancellationToken ct = default);
}
